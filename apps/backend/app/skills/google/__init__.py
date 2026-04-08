"""Google skill — Drive search and Calendar upcoming-events via a
service-account JSON credential.

Auth: paste a service-account key JSON (the entire file contents) in
the dashboard. Stored under `credentials->>'service_account_json'`.
"""

from __future__ import annotations

import base64
import json
import time
from typing import Any

import httpx
from fastapi import APIRouter, HTTPException, status
from pydantic import BaseModel

from app.skills._manifest import CredentialField, LlmTool, SkillManifest

DRIVE_API = "https://www.googleapis.com/drive/v3"
CALENDAR_API = "https://www.googleapis.com/calendar/v3"
TOKEN_URL = "https://oauth2.googleapis.com/token"

MANIFEST = SkillManifest(
    name="google",
    title="Google Workspace",
    description="Search Google Drive and list upcoming Calendar events via a service-account key.",
    emoji="🔎",
    credential_fields=[
        CredentialField(
            key="service_account_json",
            label="Service Account JSON",
            kind="textarea",
            placeholder='{ "type": "service_account", "project_id": "...", "private_key": "-----BEGIN PRIVATE KEY-----..." }',
            help=(
                "Paste the entire contents of a service-account key file. "
                "Enable the Drive and Calendar APIs in your GCP project and "
                "share the relevant Drive folders / Calendar with the "
                "service-account email."
            ),
        ),
        CredentialField(
            key="calendar_id",
            label="Calendar ID (optional)",
            kind="text",
            required=False,
            placeholder="primary",
            help="Defaults to 'primary'. Use the calendar's email address for shared calendars.",
        ),
    ],
    llm_tools=[
        LlmTool(
            name="google.drive_search",
            description="Search Google Drive for files whose name contains the query.",
            parameters={
                "type": "object",
                "properties": {
                    "query": {"type": "string"},
                    "page_size": {"type": "integer", "minimum": 1, "maximum": 100, "default": 10},
                },
                "required": ["query"],
            },
        ),
        LlmTool(
            name="google.calendar_upcoming",
            description="List the next N events on the configured calendar.",
            parameters={
                "type": "object",
                "properties": {
                    "max_results": {"type": "integer", "minimum": 1, "maximum": 50, "default": 10},
                },
            },
        ),
    ],
)


class DriveSearchBody(BaseModel):
    query: str
    page_size: int = 10


class CalendarUpcomingBody(BaseModel):
    max_results: int = 10


def _b64url(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).rstrip(b"=").decode("ascii")


async def _access_token(sa_json: dict[str, Any], scopes: list[str]) -> str:
    from cryptography.hazmat.primitives import hashes, serialization
    from cryptography.hazmat.primitives.asymmetric import padding

    now = int(time.time())
    claims = {
        "iss": sa_json["client_email"],
        "scope": " ".join(scopes),
        "aud": TOKEN_URL,
        "iat": now,
        "exp": now + 3600,
    }
    header = {"alg": "RS256", "typ": "JWT"}
    segments = [
        _b64url(json.dumps(header, separators=(",", ":")).encode()),
        _b64url(json.dumps(claims, separators=(",", ":")).encode()),
    ]
    signing_input = ".".join(segments).encode("ascii")

    private_key = serialization.load_pem_private_key(
        sa_json["private_key"].encode("utf-8"),
        password=None,
    )
    signature = private_key.sign(  # type: ignore[call-arg]
        signing_input, padding.PKCS1v15(), hashes.SHA256()
    )
    assertion = ".".join(segments) + "." + _b64url(signature)

    async with httpx.AsyncClient(timeout=20.0) as client:
        resp = await client.post(
            TOKEN_URL,
            data={
                "grant_type": "urn:ietf:params:oauth:grant-type:jwt-bearer",
                "assertion": assertion,
            },
        )
    if resp.status_code >= 400:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"Google token exchange {resp.status_code}: {resp.text[:500]}",
        )
    return str(resp.json()["access_token"])


def _parse_sa(creds: dict[str, Any]) -> dict[str, Any]:
    raw = creds.get("service_account_json")
    if not raw:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail="Google skill: service_account_json not configured.",
        )
    if isinstance(raw, dict):
        return raw
    try:
        return json.loads(str(raw))
    except json.JSONDecodeError as exc:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail=f"Google skill: service_account_json is not valid JSON: {exc}",
        ) from exc


async def _call_google(
    method: str, url: str, token: str, params: dict[str, Any] | None = None
) -> Any:
    async with httpx.AsyncClient(timeout=20.0) as client:
        resp = await client.request(
            method,
            url,
            headers={"Authorization": f"Bearer {token}"},
            params=params,
        )
    if resp.status_code >= 400:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"Google API {resp.status_code}: {resp.text[:500]}",
        )
    return resp.json()


async def drive_search(body: DriveSearchBody, creds: dict[str, Any]) -> dict[str, Any]:
    sa = _parse_sa(creds)
    token = await _access_token(sa, scopes=["https://www.googleapis.com/auth/drive.readonly"])
    q_escaped = body.query.replace("'", "\\'")
    data = await _call_google(
        "GET",
        f"{DRIVE_API}/files",
        token=token,
        params={
            "q": f"name contains '{q_escaped}' and trashed = false",
            "pageSize": body.page_size,
            "fields": "files(id,name,mimeType,webViewLink,modifiedTime)",
        },
    )
    return {"files": data.get("files", [])}


async def calendar_upcoming(
    body: CalendarUpcomingBody, creds: dict[str, Any]
) -> dict[str, Any]:
    sa = _parse_sa(creds)
    token = await _access_token(
        sa, scopes=["https://www.googleapis.com/auth/calendar.readonly"]
    )
    calendar_id = str(creds.get("calendar_id") or "primary")
    import datetime as _dt

    now_iso = _dt.datetime.now(_dt.timezone.utc).isoformat().replace("+00:00", "Z")
    data = await _call_google(
        "GET",
        f"{CALENDAR_API}/calendars/{calendar_id}/events",
        token=token,
        params={
            "maxResults": body.max_results,
            "orderBy": "startTime",
            "singleEvents": "true",
            "timeMin": now_iso,
        },
    )
    return {
        "events": [
            {
                "id": e.get("id"),
                "summary": e.get("summary"),
                "start": (e.get("start") or {}).get("dateTime")
                or (e.get("start") or {}).get("date"),
                "end": (e.get("end") or {}).get("dateTime")
                or (e.get("end") or {}).get("date"),
                "html_link": e.get("htmlLink"),
            }
            for e in data.get("items", [])
        ]
    }


def register(router: APIRouter, creds_dep) -> None:
    @router.post("/drive_search")
    async def _drive_search(
        body: DriveSearchBody, creds: dict[str, Any] = creds_dep
    ) -> dict[str, Any]:
        return await drive_search(body, creds)

    @router.post("/calendar_upcoming")
    async def _calendar_upcoming(
        body: CalendarUpcomingBody, creds: dict[str, Any] = creds_dep
    ) -> dict[str, Any]:
        return await calendar_upcoming(body, creds)
