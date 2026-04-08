"""Agent-facing self endpoints.

Mirrors a handful of capabilities-management routes so that running
agent pods (authenticated via `X-Agent-Token`) can pull their live
tool list without needing a user session.

Mount:
    /api/agents/me/capabilities    — resolved LLM tool list
    /api/agents/me/skills/<skill>/<action>  — via build_agent_skills_router()
"""

from __future__ import annotations

from dataclasses import asdict
from typing import Any

from fastapi import APIRouter, Depends
from pydantic import BaseModel
from sqlmodel import select
from sqlmodel.ext.asyncio.session import AsyncSession

from app.core.agent_auth import AgentAuthContext, get_agent_auth_context
from app.db.session import get_session
from app.models.skill_credentials import SkillCredential
from app.skills import SKILLS
from app.skills._deps import _resolve_agent_org

router = APIRouter(prefix="/agents/me", tags=["agents_me"])


class CapabilitiesResponse(BaseModel):
    tools: list[dict[str, Any]]


@router.get("/capabilities", response_model=CapabilitiesResponse)
async def agent_capabilities(
    agent_ctx: AgentAuthContext = Depends(get_agent_auth_context),
    session: AsyncSession = Depends(get_session),
) -> CapabilitiesResponse:
    """Return the live LLM tool list for this agent's organization.

    Only skills that are both installed (`enabled = true`) and have
    non-empty credentials are included.
    """

    organization_id = await _resolve_agent_org(agent_ctx, session)
    tools: list[dict[str, Any]] = []
    for name, (manifest, _module) in SKILLS.items():
        row = (
            await session.exec(
                select(SkillCredential)
                .where(SkillCredential.organization_id == organization_id)
                .where(SkillCredential.skill_name == name),
            )
        ).first()
        if not row or not row.enabled or not row.credentials:
            continue
        for tool in manifest.llm_tools:
            # LLM tool name is `{skill}.{action}`; route is
            # `/api/agents/me/skills/{skill}/{action}`.
            action = tool.name.split(".", 1)[1]
            tools.append(
                {
                    "skill": name,
                    "name": tool.name,
                    "description": tool.description,
                    "parameters": tool.parameters,
                    "route": f"/api/agents/me/skills/{name}/{action}",
                },
            )
    return CapabilitiesResponse(tools=tools)
