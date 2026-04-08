"""First-party skills.

Each skill is a small Python package under `app.skills.<name>` exporting:

    MANIFEST: SkillManifest   # metadata + credential schema + LLM tool descriptions
    register(router, creds_dep)  # mounts the action routes onto a router

The package-level `SKILLS` dict is the registry consumed by:
  * `/api/skills` management API (dashboard / user auth)
  * `/api/skills/{name}/{action}` user-auth execution routes
  * `/api/agents/me/skills/{name}/{action}` agent-auth execution routes
  * `/api/capabilities` and `/api/agents/me/capabilities`

Skills are **global per-org**: one `SkillCredential` row per
(organization, skill_name). No per-agent, no per-role assignment — the
demo deliberately keeps this flat.
"""

from __future__ import annotations

from fastapi import APIRouter

from app.skills import github as _github
from app.skills import google as _google
from app.skills import notion as _notion
from app.skills._deps import require_agent_credentials, require_credentials
from app.skills._manifest import CredentialField, LlmTool, SkillManifest

__all__ = [
    "SKILLS",
    "SkillManifest",
    "CredentialField",
    "LlmTool",
    "build_skills_router",
    "build_agent_skills_router",
]

# Each entry: {name: (manifest, module)}. The module must expose
# `register(router, creds_dep)`.
SKILLS = {
    _notion.MANIFEST.name: (_notion.MANIFEST, _notion),
    _github.MANIFEST.name: (_github.MANIFEST, _github),
    _google.MANIFEST.name: (_google.MANIFEST, _google),
}


def _build_router(creds_factory, prefix: str) -> APIRouter:
    router = APIRouter(prefix=prefix, tags=["skills"])
    for name, (_manifest, module) in SKILLS.items():
        sub = APIRouter()
        module.register(sub, creds_factory(name))
        router.include_router(sub, prefix=f"/{name}")
    return router


def build_skills_router() -> APIRouter:
    """Mount every skill under `/skills/<skill>` with **user** auth."""
    return _build_router(require_credentials, "/skills")


def build_agent_skills_router() -> APIRouter:
    """Mount every skill under `/agents/me/skills/<skill>` with **agent** auth."""
    return _build_router(require_agent_credentials, "/agents/me/skills")
