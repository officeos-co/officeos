"""LLM provider credential management API.

The dashboard's Settings → Providers page drives this. One API key
per (organization, provider). The gateway-create flow reads the key
by provider name when spawning an agent pod.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from fastapi import APIRouter, HTTPException, status
from pydantic import BaseModel
from sqlmodel import select
from sqlmodel.ext.asyncio.session import AsyncSession

from app.api.deps import ORG_MEMBER_DEP, SESSION_DEP
from app.core.time import utcnow
from app.models.provider_credentials import ProviderCredential
from app.services.organizations import OrganizationContext

router = APIRouter(prefix="/providers", tags=["providers"])


@dataclass(frozen=True)
class ProviderDef:
    name: str
    title: str
    description: str
    placeholder: str
    requires_key: bool = True


# Hardcoded catalog of supported LLM providers. Add an entry here
# (and update the gateway form's dropdown) to expose a new one in
# the Settings page.
PROVIDERS: list[ProviderDef] = [
    ProviderDef(
        name="openrouter",
        title="OpenRouter",
        description="Routes requests across many providers under one key.",
        placeholder="sk-or-v1-…",
    ),
    ProviderDef(
        name="openai",
        title="OpenAI",
        description="GPT-4 / o3 / o4-mini models.",
        placeholder="sk-…",
    ),
    ProviderDef(
        name="anthropic",
        title="Anthropic",
        description="Claude Opus / Sonnet / Haiku.",
        placeholder="sk-ant-…",
    ),
    ProviderDef(
        name="ollama",
        title="Ollama (local)",
        description="Local LLM runtime. No API key required — leave empty.",
        placeholder="",
        requires_key=False,
    ),
]

_BY_NAME = {p.name: p for p in PROVIDERS}


# ---------- response shapes ----------


class ProviderRead(BaseModel):
    name: str
    title: str
    description: str
    placeholder: str
    requires_key: bool
    configured: bool


class ProviderListResponse(BaseModel):
    providers: list[ProviderRead]


class ProviderKeyPut(BaseModel):
    api_key: str


# ---------- helpers ----------


async def _load_credential(
    name: str, ctx: OrganizationContext, session: AsyncSession
) -> ProviderCredential | None:
    return (
        await session.exec(
            select(ProviderCredential)
            .where(ProviderCredential.organization_id == ctx.organization.id)
            .where(ProviderCredential.provider == name),
        )
    ).first()


def _require_provider(name: str) -> ProviderDef:
    defn = _BY_NAME.get(name)
    if defn is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Unknown provider: {name!r}",
        )
    return defn


async def resolve_provider_api_key(
    *,
    provider: str,
    organization_id,
    session: AsyncSession,
) -> str | None:
    """Return the stored API key for one provider, or None.

    Called by the gateway-create handler. Returns None both when the
    provider isn't configured and when it's a key-less provider
    (e.g. Ollama). Callers distinguish via `PROVIDERS[provider].requires_key`.
    """
    row = (
        await session.exec(
            select(ProviderCredential)
            .where(ProviderCredential.organization_id == organization_id)
            .where(ProviderCredential.provider == provider),
        )
    ).first()
    return row.api_key if row else None


# ---------- routes ----------


@router.get("", response_model=ProviderListResponse)
async def list_providers(
    ctx: OrganizationContext = ORG_MEMBER_DEP,
    session: AsyncSession = SESSION_DEP,
) -> ProviderListResponse:
    items: list[ProviderRead] = []
    for defn in PROVIDERS:
        row = await _load_credential(defn.name, ctx, session)
        items.append(
            ProviderRead(
                name=defn.name,
                title=defn.title,
                description=defn.description,
                placeholder=defn.placeholder,
                requires_key=defn.requires_key,
                configured=bool(row and row.api_key),
            )
        )
    return ProviderListResponse(providers=items)


@router.put("/{name}", response_model=ProviderRead)
async def put_provider_key(
    name: str,
    body: ProviderKeyPut,
    ctx: OrganizationContext = ORG_MEMBER_DEP,
    session: AsyncSession = SESSION_DEP,
) -> ProviderRead:
    defn = _require_provider(name)
    key = (body.api_key or "").strip()
    if defn.requires_key and not key:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail=f"{defn.title} requires a non-empty API key.",
        )

    row = await _load_credential(name, ctx, session)
    if row is None:
        row = ProviderCredential(
            organization_id=ctx.organization.id,
            provider=name,
            api_key=key,
        )
        session.add(row)
    else:
        row.api_key = key
        row.updated_at = utcnow()
    await session.commit()
    await session.refresh(row)

    return ProviderRead(
        name=defn.name,
        title=defn.title,
        description=defn.description,
        placeholder=defn.placeholder,
        requires_key=defn.requires_key,
        configured=bool(row.api_key),
    )


@router.delete("/{name}", response_model=ProviderRead)
async def delete_provider_key(
    name: str,
    ctx: OrganizationContext = ORG_MEMBER_DEP,
    session: AsyncSession = SESSION_DEP,
) -> ProviderRead:
    defn = _require_provider(name)
    row = await _load_credential(name, ctx, session)
    if row is not None:
        await session.delete(row)
        await session.commit()
    return ProviderRead(
        name=defn.name,
        title=defn.title,
        description=defn.description,
        placeholder=defn.placeholder,
        requires_key=defn.requires_key,
        configured=False,
    )
