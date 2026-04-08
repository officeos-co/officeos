"""Vault browser — read-only view over the CouchDB agent vaults.

Drives the dashboard's "Vault" tab. Lets operators inspect the raw
markdown content of each per-agent database without connecting
Obsidian to CouchDB directly.

Endpoints:

    GET /api/vault/databases              list all agent vault DBs
    GET /api/vault/{database}/files       list .md note paths
    GET /api/vault/{database}/files?path=…  raw markdown for one note

All routes require dashboard user auth (`ORG_MEMBER_DEP`) — this is
an operator tool, not an agent-facing API.
"""

from __future__ import annotations

import os
from typing import Any

import httpx
from fastapi import APIRouter, HTTPException, Query, status

from app.api.deps import ORG_MEMBER_DEP
from app.services.organizations import OrganizationContext

router = APIRouter(prefix="/vault", tags=["vault"])


def _vault_base_url() -> str:
    host = os.environ.get("ZEROCLAW_VAULT_HOST", "localhost")
    port = int(os.environ.get("ZEROCLAW_VAULT_PORT", "5984"))
    protocol = os.environ.get("ZEROCLAW_VAULT_PROTOCOL", "http")
    return f"{protocol}://{host}:{port}"


def _vault_auth() -> tuple[str, str] | None:
    user = os.environ.get("ZEROCLAW_VAULT_USERNAME", "")
    pw = os.environ.get("ZEROCLAW_VAULT_PASSWORD", "")
    return (user, pw) if user else None


async def _couch_get(path: str, params: dict[str, Any] | None = None) -> Any:
    url = f"{_vault_base_url()}{path}"
    auth = _vault_auth()
    try:
        async with httpx.AsyncClient(timeout=15.0) as client:
            resp = await client.get(url, params=params, auth=auth)
    except httpx.RequestError as exc:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"CouchDB unreachable: {exc}",
        ) from exc
    if resp.status_code == 404:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND)
    if resp.status_code >= 400:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"CouchDB {resp.status_code}: {resp.text[:300]}",
        )
    return resp.json()


# ─── routes ───────────────────────────────────────────────────────────


@router.get("/databases")
async def list_databases(
    _ctx: OrganizationContext = ORG_MEMBER_DEP,
) -> dict[str, list[str]]:
    """Return all vault databases visible in CouchDB.

    Filters out CouchDB system databases (`_users`, `_replicator`,
    etc.) and anything that doesn't look like an agent or user vault.
    """
    all_dbs: list[str] = await _couch_get("/_all_dbs")
    visible = [
        name
        for name in all_dbs
        if not name.startswith("_")
    ]
    return {"databases": sorted(visible)}


@router.get("/{database}/files")
async def list_files(
    database: str,
    path: str | None = Query(default=None, description="Optional single path"),
    _ctx: OrganizationContext = ORG_MEMBER_DEP,
) -> dict[str, Any]:
    """List every note path in `database` — or when `path` is given,
    return the raw markdown for that single note.

    Combining list + read on one route keeps the dashboard client
    tiny (one fetch function, one URL).
    """
    if path is not None:
        return await _read_note(database, path)

    data = await _couch_get(
        f"/{database}/_all_docs",
        params={"include_docs": "true"},
    )
    files: list[dict[str, Any]] = []
    for row in data.get("rows", []):
        row_id: str = row.get("id") or ""
        doc: dict[str, Any] = row.get("doc") or {}
        # Same filters as vault_cli.core.client.list_notes:
        if row_id.startswith("h:"):
            continue  # chunk doc
        if row_id.startswith("_"):
            continue  # system doc
        if row_id == "obsydian_livesync_version":
            continue
        if doc.get("deleted"):
            continue
        note_path = str(doc.get("path") or "")
        if not note_path:
            continue
        files.append(
            {
                "path": note_path,
                "id": row_id,
                "mtime": doc.get("mtime"),
                "size": doc.get("size"),
            },
        )
    files.sort(key=lambda f: f["path"])
    return {"database": database, "files": files}


async def _read_note(database: str, path: str) -> dict[str, Any]:
    """Fetch a single note by path and join its chunks into full markdown."""
    # Match vault_cli's _path_to_id: lowercase; leading `_` becomes `/_`.
    doc_id = path.lower()
    if doc_id.startswith("_"):
        doc_id = "/" + doc_id
    # CouchDB path-encoding: spaces, slashes, colons all need escaping.
    # httpx will URL-encode the full URL, but `/` inside the id is
    # ambiguous — we encode it ourselves via the `params` path.
    from urllib.parse import quote

    encoded = quote(doc_id, safe="")
    metadata = await _couch_get(f"/{database}/{encoded}")
    children: list[str] = metadata.get("children") or []

    chunks: list[str] = []
    for chunk_id in children:
        encoded_chunk = quote(chunk_id, safe="")
        chunk = await _couch_get(f"/{database}/{encoded_chunk}")
        chunks.append(str(chunk.get("data") or ""))

    return {
        "database": database,
        "path": metadata.get("path", path),
        "content": "".join(chunks),
        "mtime": metadata.get("mtime"),
        "ctime": metadata.get("ctime"),
    }
