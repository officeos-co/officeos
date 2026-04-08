"""Notion skill — search and read pages via the Notion REST API.

Auth: single API key (Internal Integration Token), pasted in the
dashboard. Stored in `skill_credentials.credentials->>'api_key'`.

Handlers are pure async functions taking `(body, creds)`. Routes are
registered via `register(router, creds_dep)` so the same handlers can
be wired once for user auth (`/api/skills/notion/...`) and once for
agent auth (`/api/agents/me/skills/notion/...`).
"""

from __future__ import annotations

from typing import Any

import httpx
from fastapi import APIRouter, HTTPException, status
from pydantic import BaseModel

from app.skills._manifest import CredentialField, LlmTool, SkillManifest

NOTION_API = "https://api.notion.com/v1"
NOTION_VERSION = "2022-06-28"

MANIFEST = SkillManifest(
    name="notion",
    title="Notion",
    description="Search and read Notion pages and databases via the Notion REST API.",
    emoji="📝",
    credential_fields=[
        CredentialField(
            key="api_key",
            label="Internal Integration Token",
            kind="password",
            placeholder="secret_…",
            help=(
                "Create an Internal Integration at "
                "https://www.notion.so/my-integrations and share the pages "
                "you want the agent to access with it."
            ),
        ),
    ],
    llm_tools=[
        LlmTool(
            name="notion.search",
            description="Search the connected Notion workspace for pages matching a query.",
            parameters={
                "type": "object",
                "properties": {
                    "query": {"type": "string", "description": "Free-text query."},
                    "page_size": {
                        "type": "integer",
                        "minimum": 1,
                        "maximum": 100,
                        "default": 10,
                    },
                },
                "required": ["query"],
            },
        ),
        LlmTool(
            name="notion.read_page",
            description="Fetch a Notion page's top-level block children as plain text.",
            parameters={
                "type": "object",
                "properties": {
                    "page_id": {"type": "string"},
                },
                "required": ["page_id"],
            },
        ),
    ],
)


class SearchBody(BaseModel):
    query: str
    page_size: int = 10


class ReadPageBody(BaseModel):
    page_id: str


async def _call_notion(
    method: str,
    path: str,
    api_key: str,
    json: dict[str, Any] | None = None,
) -> dict[str, Any]:
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Notion-Version": NOTION_VERSION,
        "Content-Type": "application/json",
    }
    async with httpx.AsyncClient(timeout=20.0) as client:
        resp = await client.request(
            method, f"{NOTION_API}{path}", headers=headers, json=json
        )
    if resp.status_code >= 400:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"Notion API {resp.status_code}: {resp.text[:500]}",
        )
    return resp.json()


async def search(body: SearchBody, creds: dict[str, Any]) -> dict[str, Any]:
    data = await _call_notion(
        "POST",
        "/search",
        api_key=str(creds["api_key"]),
        json={"query": body.query, "page_size": body.page_size},
    )
    results = []
    for item in data.get("results", []):
        props = item.get("properties") or {}
        title = ""
        for prop in props.values():
            if prop.get("type") == "title":
                fragments = prop.get("title") or []
                title = "".join(f.get("plain_text", "") for f in fragments)
                break
        results.append(
            {
                "id": item.get("id"),
                "title": title or "(untitled)",
                "url": item.get("url"),
                "object": item.get("object"),
            },
        )
    return {"results": results}


async def read_page(body: ReadPageBody, creds: dict[str, Any]) -> dict[str, Any]:
    data = await _call_notion(
        "GET",
        f"/blocks/{body.page_id}/children?page_size=100",
        api_key=str(creds["api_key"]),
    )
    lines: list[str] = []
    for block in data.get("results", []):
        btype = block.get("type")
        payload = block.get(btype) or {}
        rich = payload.get("rich_text") or []
        text = "".join(rt.get("plain_text", "") for rt in rich)
        if text:
            lines.append(text)
    return {"page_id": body.page_id, "text": "\n".join(lines)}


def register(router: APIRouter, creds_dep) -> None:
    """Wire the pure handlers onto a FastAPI router with a credentials dep."""

    @router.post("/search")
    async def _search(body: SearchBody, creds: dict[str, Any] = creds_dep) -> dict[str, Any]:
        return await search(body, creds)

    @router.post("/read_page")
    async def _read_page(
        body: ReadPageBody, creds: dict[str, Any] = creds_dep
    ) -> dict[str, Any]:
        return await read_page(body, creds)
