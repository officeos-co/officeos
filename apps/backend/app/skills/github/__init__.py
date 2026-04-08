"""GitHub skill — list repos / issues / PRs via the GitHub REST API.

Auth: a single Personal Access Token (fine-grained or classic),
pasted in the dashboard. Stored under `credentials->>'token'`.
"""

from __future__ import annotations

from typing import Any

import httpx
from fastapi import APIRouter, HTTPException, status
from pydantic import BaseModel

from app.skills._manifest import CredentialField, LlmTool, SkillManifest

GITHUB_API = "https://api.github.com"

MANIFEST = SkillManifest(
    name="github",
    title="GitHub",
    description="List repositories, issues, and pull requests via the GitHub REST API.",
    emoji="🐙",
    credential_fields=[
        CredentialField(
            key="token",
            label="Personal Access Token",
            kind="password",
            placeholder="github_pat_… or ghp_…",
            help=(
                "Fine-grained PAT recommended. Needs read access to the "
                "repos/issues/PRs you want the agent to see. Create one at "
                "https://github.com/settings/tokens."
            ),
        ),
    ],
    llm_tools=[
        LlmTool(
            name="github.list_repos",
            description="List repositories accessible to the authenticated user.",
            parameters={
                "type": "object",
                "properties": {
                    "visibility": {
                        "type": "string",
                        "enum": ["all", "public", "private"],
                        "default": "all",
                    },
                    "per_page": {"type": "integer", "minimum": 1, "maximum": 100, "default": 30},
                },
            },
        ),
        LlmTool(
            name="github.list_issues",
            description="List open issues in a single repository.",
            parameters={
                "type": "object",
                "properties": {
                    "owner": {"type": "string"},
                    "repo": {"type": "string"},
                    "state": {
                        "type": "string",
                        "enum": ["open", "closed", "all"],
                        "default": "open",
                    },
                },
                "required": ["owner", "repo"],
            },
        ),
        LlmTool(
            name="github.list_prs",
            description="List pull requests in a single repository.",
            parameters={
                "type": "object",
                "properties": {
                    "owner": {"type": "string"},
                    "repo": {"type": "string"},
                    "state": {
                        "type": "string",
                        "enum": ["open", "closed", "all"],
                        "default": "open",
                    },
                },
                "required": ["owner", "repo"],
            },
        ),
    ],
)


class ListReposBody(BaseModel):
    visibility: str = "all"
    per_page: int = 30


class RepoBody(BaseModel):
    owner: str
    repo: str
    state: str = "open"


async def _call_gh(
    method: str, path: str, token: str, params: dict[str, Any] | None = None
) -> Any:
    headers = {
        "Authorization": f"Bearer {token}",
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
    }
    async with httpx.AsyncClient(timeout=20.0) as client:
        resp = await client.request(
            method, f"{GITHUB_API}{path}", headers=headers, params=params
        )
    if resp.status_code >= 400:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"GitHub API {resp.status_code}: {resp.text[:500]}",
        )
    return resp.json()


async def list_repos(body: ListReposBody, creds: dict[str, Any]) -> dict[str, Any]:
    data = await _call_gh(
        "GET",
        "/user/repos",
        token=str(creds["token"]),
        params={"visibility": body.visibility, "per_page": body.per_page},
    )
    return {
        "repos": [
            {
                "full_name": r.get("full_name"),
                "private": r.get("private"),
                "description": r.get("description"),
                "html_url": r.get("html_url"),
                "default_branch": r.get("default_branch"),
            }
            for r in data
        ]
    }


async def list_issues(body: RepoBody, creds: dict[str, Any]) -> dict[str, Any]:
    data = await _call_gh(
        "GET",
        f"/repos/{body.owner}/{body.repo}/issues",
        token=str(creds["token"]),
        params={"state": body.state},
    )
    issues = [i for i in data if "pull_request" not in i]
    return {
        "issues": [
            {
                "number": i.get("number"),
                "title": i.get("title"),
                "state": i.get("state"),
                "author": (i.get("user") or {}).get("login"),
                "html_url": i.get("html_url"),
            }
            for i in issues
        ]
    }


async def list_prs(body: RepoBody, creds: dict[str, Any]) -> dict[str, Any]:
    data = await _call_gh(
        "GET",
        f"/repos/{body.owner}/{body.repo}/pulls",
        token=str(creds["token"]),
        params={"state": body.state},
    )
    return {
        "prs": [
            {
                "number": pr.get("number"),
                "title": pr.get("title"),
                "state": pr.get("state"),
                "author": (pr.get("user") or {}).get("login"),
                "html_url": pr.get("html_url"),
                "draft": pr.get("draft"),
            }
            for pr in data
        ]
    }


def register(router: APIRouter, creds_dep) -> None:
    @router.post("/list_repos")
    async def _list_repos(
        body: ListReposBody, creds: dict[str, Any] = creds_dep
    ) -> dict[str, Any]:
        return await list_repos(body, creds)

    @router.post("/list_issues")
    async def _list_issues(
        body: RepoBody, creds: dict[str, Any] = creds_dep
    ) -> dict[str, Any]:
        return await list_issues(body, creds)

    @router.post("/list_prs")
    async def _list_prs(
        body: RepoBody, creds: dict[str, Any] = creds_dep
    ) -> dict[str, Any]:
        return await list_prs(body, creds)
