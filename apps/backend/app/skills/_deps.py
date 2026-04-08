"""Shared helpers for first-party skill routes.

Two credential-resolution paths are exposed because the same set of
skill action routes is mounted twice:

- `require_credentials(skill_name)` — resolves the calling **user** via
  `ORG_MEMBER_DEP`, used by the dashboard and manual curl testing.
- `require_agent_credentials(skill_name)` — resolves the calling
  **agent** via `X-Agent-Token`, walks to `agent.gateway.organization_id`,
  and loads the same `skill_credentials` row. This is what running
  agent pods hit via the Rust runtime's `BackendSkillTool`.

In both cases the response is an opaque credentials dict (API key /
PAT / service-account JSON / whatever the skill's manifest declared).
The caller never sees it directly — the skill handler does.
"""

from __future__ import annotations

from typing import Any

from fastapi import Depends, HTTPException, status
from sqlmodel import select
from sqlmodel.ext.asyncio.session import AsyncSession

from app.api.deps import ORG_MEMBER_DEP, SESSION_DEP
from app.core.agent_auth import AgentAuthContext, get_agent_auth_context
from app.db.session import get_session
from app.models.gateways import Gateway
from app.models.skill_credentials import SkillCredential
from app.services.organizations import OrganizationContext


async def _load_credential_row(
    skill_name: str,
    organization_id,
    session: AsyncSession,
) -> dict[str, Any]:
    row = (
        await session.exec(
            select(SkillCredential)
            .where(SkillCredential.organization_id == organization_id)
            .where(SkillCredential.skill_name == skill_name),
        )
    ).first()
    if row is None or not row.enabled:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail=f"Skill '{skill_name}' is not installed for this organization.",
        )
    if not row.credentials:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail=(
                f"Skill '{skill_name}' is installed but has no credentials "
                "configured. Configure it in the dashboard → Skills."
            ),
        )
    return dict(row.credentials)


def require_credentials(skill_name: str):  # -> FastAPI Depends
    """Dashboard/user-auth path: loads credentials for the current user's org."""

    async def _dep(
        ctx: OrganizationContext = ORG_MEMBER_DEP,
        session: AsyncSession = SESSION_DEP,
    ) -> dict[str, Any]:
        return await _load_credential_row(skill_name, ctx.organization.id, session)

    return Depends(_dep)


async def _resolve_agent_org(
    agent_ctx: AgentAuthContext,
    session: AsyncSession,
):
    """Walk agent → gateway → organization_id."""
    agent = agent_ctx.agent
    gateway = await Gateway.objects.by_id(agent.gateway_id).first(session)
    if gateway is None:
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Agent gateway not found.",
        )
    return gateway.organization_id


def require_agent_credentials(skill_name: str):  # -> FastAPI Depends
    """Agent-auth path (X-Agent-Token): loads credentials for the agent's org."""

    async def _dep(
        agent_ctx: AgentAuthContext = Depends(get_agent_auth_context),
        session: AsyncSession = Depends(get_session),
    ) -> dict[str, Any]:
        organization_id = await _resolve_agent_org(agent_ctx, session)
        return await _load_credential_row(skill_name, organization_id, session)

    return Depends(_dep)
