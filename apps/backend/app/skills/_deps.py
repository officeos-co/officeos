"""Shared helpers for first-party skill routes.

Each skill route receives the current org via `ORG_MEMBER_DEP` and
loads its `SkillCredential` row via `load_credentials()`. If the skill
isn't installed/configured the route returns 409 Conflict with a
pointer at the dashboard so the user can fix it.
"""

from __future__ import annotations

from typing import Any

from fastapi import Depends, HTTPException, status
from sqlmodel import select
from sqlmodel.ext.asyncio.session import AsyncSession

from app.api.deps import ORG_MEMBER_DEP, SESSION_DEP
from app.models.skill_credentials import SkillCredential
from app.services.organizations import OrganizationContext


async def load_credentials(
    skill_name: str,
    ctx: OrganizationContext,
    session: AsyncSession,
) -> dict[str, Any]:
    """Return the stored credential dict for `skill_name` in this org.

    Raises 409 if the skill is uninstalled or has no credentials yet —
    the dashboard is expected to present the auth form in response.
    """

    row = (
        await session.exec(
            select(SkillCredential)
            .where(SkillCredential.organization_id == ctx.organization.id)
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


def require_credentials(skill_name: str):  # -> FastAPI dependency
    """Build a FastAPI dependency that loads credentials for one skill."""

    async def _dep(
        ctx: OrganizationContext = ORG_MEMBER_DEP,
        session: AsyncSession = SESSION_DEP,
    ) -> dict[str, Any]:
        return await load_credentials(skill_name, ctx, session)

    return Depends(_dep)
