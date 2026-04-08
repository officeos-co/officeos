"""First-party skills.

Each skill is a small Python package under `app.skills.<name>` exporting:

    MANIFEST: SkillManifest   # metadata + credential schema + LLM tool descriptions
    router:   APIRouter       # the FastAPI routes that do the actual work

The package-level `SKILLS` dict is the registry consumed by the
`/api/v1/skills` management API and the `/api/v1/capabilities` endpoint
that agents poll each turn.

Skills are **global per-org**: one `SkillCredential` row per
(organization, skill_name). No per-agent, no per-role assignment — the
v1 demo deliberately keeps this flat.
"""

from __future__ import annotations

from fastapi import APIRouter

from app.skills._manifest import CredentialField, LlmTool, SkillManifest
from app.skills.github import MANIFEST as GITHUB_MANIFEST
from app.skills.github import router as github_router
from app.skills.google import MANIFEST as GOOGLE_MANIFEST
from app.skills.google import router as google_router
from app.skills.notion import MANIFEST as NOTION_MANIFEST
from app.skills.notion import router as notion_router

__all__ = [
    "SKILLS",
    "SkillManifest",
    "CredentialField",
    "LlmTool",
    "build_skills_router",
]

SKILLS: dict[str, tuple[SkillManifest, APIRouter]] = {
    NOTION_MANIFEST.name: (NOTION_MANIFEST, notion_router),
    GITHUB_MANIFEST.name: (GITHUB_MANIFEST, github_router),
    GOOGLE_MANIFEST.name: (GOOGLE_MANIFEST, google_router),
}


def build_skills_router() -> APIRouter:
    """Mount every skill's router under /api/v1/skills/<skill>."""

    router = APIRouter(prefix="/skills", tags=["skills"])
    for name, (_manifest, skill_router) in SKILLS.items():
        router.include_router(skill_router, prefix=f"/{name}")
    return router
