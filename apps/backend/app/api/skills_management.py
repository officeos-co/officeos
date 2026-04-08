"""Skill management API — global-per-org skill install + credential config.

The single source of truth for the dashboard's Skills page and for the
`/capabilities` endpoint that running agents poll each turn.

Endpoints:

    GET  /skills                 list catalog + per-org install state
    GET  /skills/{name}          single skill detail (manifest + state)
    POST /skills/{name}/install
    POST /skills/{name}/uninstall
    PUT  /skills/{name}/credentials   { credentials: {...} }
    GET  /capabilities           resolved LLM tools for the org (agent-facing)

The per-skill *execution* routes (e.g. `POST /skills/notion/search`) are
mounted by `app.skills.build_skills_router()` on the same `/skills`
prefix. That's intentional: management verbs live at reserved suffixes
(`install`, `uninstall`, `credentials`) that cannot collide with skill
action names.
"""

from __future__ import annotations

from dataclasses import asdict
from typing import Any

from fastapi import APIRouter, HTTPException, status
from pydantic import BaseModel
from sqlmodel import select
from sqlmodel.ext.asyncio.session import AsyncSession

from app.api.deps import ORG_MEMBER_DEP, SESSION_DEP
from app.core.time import utcnow
from app.models.skill_credentials import SkillCredential
from app.services.organizations import OrganizationContext
from app.skills import SKILLS

router = APIRouter(tags=["skills"])


# ---------- response shapes ----------


class SkillStateRead(BaseModel):
    name: str
    title: str
    description: str
    emoji: str
    installed: bool
    configured: bool
    credential_fields: list[dict[str, Any]]
    llm_tools: list[dict[str, Any]]


class SkillListResponse(BaseModel):
    skills: list[SkillStateRead]


class CredentialsPut(BaseModel):
    credentials: dict[str, Any]


class CapabilitiesResponse(BaseModel):
    """Returned by /capabilities — the live tool list an agent should see."""

    tools: list[dict[str, Any]]  # {skill, name, description, parameters, route}


# ---------- helpers ----------


def _require_skill(name: str) -> tuple:
    if name not in SKILLS:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=f"Unknown skill: {name}")
    return SKILLS[name]


async def _load_state(
    name: str, ctx: OrganizationContext, session: AsyncSession
) -> SkillCredential | None:
    return (
        await session.exec(
            select(SkillCredential)
            .where(SkillCredential.organization_id == ctx.organization.id)
            .where(SkillCredential.skill_name == name),
        )
    ).first()


def _to_read(manifest, state: SkillCredential | None) -> SkillStateRead:
    return SkillStateRead(
        name=manifest.name,
        title=manifest.title,
        description=manifest.description,
        emoji=manifest.emoji,
        installed=bool(state and state.enabled),
        configured=bool(state and state.credentials),
        credential_fields=[asdict(f) for f in manifest.credential_fields],
        llm_tools=[asdict(t) for t in manifest.llm_tools],
    )


async def _upsert(
    name: str,
    ctx: OrganizationContext,
    session: AsyncSession,
    *,
    enabled: bool | None = None,
    credentials: dict[str, Any] | None = None,
) -> SkillCredential:
    row = await _load_state(name, ctx, session)
    if row is None:
        row = SkillCredential(
            organization_id=ctx.organization.id,
            skill_name=name,
            enabled=enabled or False,
            credentials=credentials or {},
        )
        session.add(row)
    else:
        if enabled is not None:
            row.enabled = enabled
        if credentials is not None:
            row.credentials = credentials
        row.updated_at = utcnow()
    await session.commit()
    await session.refresh(row)
    return row


# ---------- routes ----------


@router.get("/skills", response_model=SkillListResponse)
async def list_skills(
    ctx: OrganizationContext = ORG_MEMBER_DEP,
    session: AsyncSession = SESSION_DEP,
) -> SkillListResponse:
    items: list[SkillStateRead] = []
    for name, (manifest, _r) in SKILLS.items():
        state = await _load_state(name, ctx, session)
        items.append(_to_read(manifest, state))
    return SkillListResponse(skills=items)


@router.get("/skills/{name}", response_model=SkillStateRead)
async def get_skill(
    name: str,
    ctx: OrganizationContext = ORG_MEMBER_DEP,
    session: AsyncSession = SESSION_DEP,
) -> SkillStateRead:
    manifest, _r = _require_skill(name)
    state = await _load_state(name, ctx, session)
    return _to_read(manifest, state)


@router.post("/skills/{name}/install", response_model=SkillStateRead)
async def install_skill(
    name: str,
    ctx: OrganizationContext = ORG_MEMBER_DEP,
    session: AsyncSession = SESSION_DEP,
) -> SkillStateRead:
    manifest, _r = _require_skill(name)
    row = await _upsert(name, ctx, session, enabled=True)
    return _to_read(manifest, row)


@router.post("/skills/{name}/uninstall", response_model=SkillStateRead)
async def uninstall_skill(
    name: str,
    ctx: OrganizationContext = ORG_MEMBER_DEP,
    session: AsyncSession = SESSION_DEP,
) -> SkillStateRead:
    manifest, _r = _require_skill(name)
    row = await _upsert(name, ctx, session, enabled=False)
    return _to_read(manifest, row)


@router.put("/skills/{name}/credentials", response_model=SkillStateRead)
async def put_credentials(
    name: str,
    body: CredentialsPut,
    ctx: OrganizationContext = ORG_MEMBER_DEP,
    session: AsyncSession = SESSION_DEP,
) -> SkillStateRead:
    manifest, _r = _require_skill(name)
    # Validate required fields.
    missing = [
        f.key
        for f in manifest.credential_fields
        if f.required and not str(body.credentials.get(f.key, "")).strip()
    ]
    if missing:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail=f"Missing required credential fields: {missing}",
        )
    # Accept only known keys — avoid storing arbitrary junk the UI sent.
    known = {f.key for f in manifest.credential_fields}
    filtered = {k: v for k, v in body.credentials.items() if k in known}
    row = await _upsert(name, ctx, session, credentials=filtered)
    return _to_read(manifest, row)


@router.get("/capabilities", response_model=CapabilitiesResponse)
async def get_capabilities(
    ctx: OrganizationContext = ORG_MEMBER_DEP,
    session: AsyncSession = SESSION_DEP,
) -> CapabilitiesResponse:
    """Live tool list for agents — only includes installed + configured skills."""

    tools: list[dict[str, Any]] = []
    for name, (manifest, _r) in SKILLS.items():
        state = await _load_state(name, ctx, session)
        if not state or not state.enabled or not state.credentials:
            continue
        for t in manifest.llm_tools:
            tools.append(
                {
                    "skill": name,
                    "name": t.name,
                    "description": t.description,
                    "parameters": t.parameters,
                    # The route the agent (or eaos CLI) should POST to.
                    # Format: /api/skills/{skill}/{action}
                    # Convention: LLM tool name = "{skill}.{action}"
                    "route": f"/api/skills/{name}/{t.name.split('.', 1)[1]}",
                },
            )
    return CapabilitiesResponse(tools=tools)
