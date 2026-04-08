"""Reverse-proxy for the zeroclaw agent's embedded web dashboard.

Every zeroclaw pod serves its own SPA at `:42617/<prefix>/` where the
prefix is supplied via the `ZEROCLAW_PATH_PREFIX` env var we set at
pod creation (see `k8s_manager._build_env_vars`). Each pod gets a
unique prefix — `/api/gateways/{id}/ui` — so this proxy forwards
requests transparently with zero URL rewriting.

Auth model (new-tab navigation survives localStorage's limits):

1. Dashboard user clicks "Open agent dashboard".
2. Dashboard calls `POST /api/gateways/{id}/ui-token` with its
   normal session Bearer. Backend validates, mints a 30-minute JWT
   scoped to this one gateway_id, returns it.
3. Dashboard opens `.../ui/?t=<jwt>` in a new tab.
4. Proxy sees `?t=...`, verifies the JWT, then Set-Cookies it as a
   Path-scoped HttpOnly cookie (`gateway_ui_session`) so every
   subsequent SPA fetch under `/api/gateways/{id}/ui/*` inherits the
   same auth without the dashboard ever touching it again.
5. When the cookie expires (30 min) or is cleared, the user has to
   click "Open agent dashboard" again to mint a fresh token.

The UI token is **gateway-scoped**: a leaked token for one gateway
can't reach any other, because `verify_gateway_ui_token` rejects
mismatched `gw` claims.

Caveats:
- WebSocket upgrade (`/ws/chat`) is not yet proxied. This only covers
  HTTP request/response. Chat streaming needs a separate WS route.
- Request/response bodies are buffered, not streamed. Fine for static
  assets + JSON API calls; rethink if the agent dashboard ever serves
  multi-MB downloads.
"""

from __future__ import annotations

from typing import Any
from uuid import UUID

import httpx
from fastapi import APIRouter, HTTPException, Request, Response
from fastapi import status as http_status
from pydantic import BaseModel
from sqlmodel.ext.asyncio.session import AsyncSession

from app.api.deps import ORG_MEMBER_DEP, SESSION_DEP
from app.core.jwt import (
    SessionTokenError,
    UI_TOKEN_TTL_MINUTES,
    create_gateway_ui_token,
    verify_gateway_ui_token,
)
from app.models.gateways import Gateway
from app.services.gateway_dispatch_router import _zeroclaw_host_port
from app.services.organizations import OrganizationContext

router = APIRouter(tags=["gateway_ui"])

# Hop-by-hop headers a reverse proxy must NOT forward.
# https://www.rfc-editor.org/rfc/rfc7230#section-6.1
_HOP_BY_HOP = {
    "connection",
    "keep-alive",
    "proxy-authenticate",
    "proxy-authorization",
    "te",
    "trailers",
    "transfer-encoding",
    "upgrade",
    # Content-Length / Content-Encoding get recomputed after httpx
    # has buffered the response body.
    "content-length",
    "content-encoding",
}

# Cookie name for the cross-request UI session. Scoped per-gateway
# via the `Path` attribute so two open tabs can hold two tokens.
_UI_COOKIE_NAME = "gateway_ui_session"


# ─── token mint endpoint ────────────────────────────────────────────


class GatewayUiTokenResponse(BaseModel):
    token: str
    expires_in: int  # seconds


@router.post(
    "/gateways/{gateway_id}/ui-token",
    response_model=GatewayUiTokenResponse,
)
async def mint_gateway_ui_token(
    gateway_id: UUID,
    ctx: OrganizationContext = ORG_MEMBER_DEP,
    session: AsyncSession = SESSION_DEP,
) -> GatewayUiTokenResponse:
    """Mint a short-lived UI token for one gateway.

    The dashboard calls this right before opening the agent's embedded
    UI in a new tab. We check org-scope so users can't mint tokens
    for gateways they don't own.
    """
    gateway = (
        await Gateway.objects.by_id(gateway_id)
        .filter_by(organization_id=ctx.organization.id)
        .first(session)
    )
    if gateway is None:
        raise HTTPException(status_code=http_status.HTTP_404_NOT_FOUND)

    user_id = "unknown"
    if ctx.member and ctx.member.user_id is not None:
        user_id = str(ctx.member.user_id)

    token = create_gateway_ui_token(
        gateway_id=str(gateway_id),
        user_id=user_id,
    )
    return GatewayUiTokenResponse(
        token=token,
        expires_in=UI_TOKEN_TTL_MINUTES * 60,
    )


# ─── reverse proxy ──────────────────────────────────────────────────


def _authorize(request: Request, gateway_id: UUID) -> tuple[bool, str | None]:
    """Return (ok, token_from_query).

    Verifies a gateway-UI token from either:
      1. The `?t=...` query param (first request from new tab).
      2. The `gateway_ui_session` cookie (subsequent SPA fetches).

    When the query param is valid we return it so the caller can
    Set-Cookie it on the response, promoting one-shot URL auth into
    a persistent session for the rest of the proxy interaction.
    """
    token_from_query = request.query_params.get("t", "").strip() or None
    if token_from_query:
        try:
            verify_gateway_ui_token(
                token_from_query, expected_gateway_id=str(gateway_id)
            )
            return True, token_from_query
        except SessionTokenError:
            return False, None

    cookie = request.cookies.get(_UI_COOKIE_NAME)
    if cookie:
        try:
            verify_gateway_ui_token(cookie, expected_gateway_id=str(gateway_id))
            return True, None
        except SessionTokenError:
            return False, None

    return False, None


@router.api_route(
    "/gateways/{gateway_id}/ui",
    methods=["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"],
)
@router.api_route(
    "/gateways/{gateway_id}/ui/{sub_path:path}",
    methods=["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"],
)
async def proxy_gateway_ui(
    gateway_id: UUID,
    request: Request,
    sub_path: str = "",
    session: AsyncSession = SESSION_DEP,
) -> Response:
    """Forward a request to the zeroclaw pod's embedded web UI."""

    ok, token_from_query = _authorize(request, gateway_id)
    if not ok:
        # 401 with a minimal HTML body so a user who hits the URL
        # without a token sees a helpful message instead of JSON.
        return Response(
            content=(
                "<html><body style='font-family:system-ui;padding:2rem'>"
                "<h1>Unauthorized</h1>"
                "<p>Open this agent from the dashboard's "
                "<strong>Open agent dashboard</strong> button, which mints a "
                "short-lived access token. Direct URL access is not allowed."
                "</p></body></html>"
            ),
            status_code=http_status.HTTP_401_UNAUTHORIZED,
            media_type="text/html",
        )

    gateway = await Gateway.objects.by_id(gateway_id).first(session)
    if gateway is None:
        return Response(status_code=http_status.HTTP_404_NOT_FOUND)

    host, port = _zeroclaw_host_port(gateway)

    # The pod's path_prefix matches exactly, so forward the path
    # verbatim. Preserve query string.
    upstream_path = f"/api/gateways/{gateway_id}/ui"
    if sub_path:
        upstream_path = f"{upstream_path}/{sub_path}"
    upstream_url = f"http://{host}:{port}{upstream_path}"

    # Strip our own `t=` query param before forwarding — the agent
    # pod shouldn't see the auth token at all.
    forward_query_items = [
        (k, v) for k, v in request.query_params.multi_items() if k != "t"
    ]
    if forward_query_items:
        from urllib.parse import urlencode

        upstream_url = f"{upstream_url}?{urlencode(forward_query_items)}"

    # Strip hop-by-hop request headers and the Host header (httpx
    # sets its own based on the target URL). Also strip the Cookie
    # header so the agent never sees the UI session cookie.
    forward_headers = {
        k: v
        for k, v in request.headers.items()
        if k.lower() not in _HOP_BY_HOP
        and k.lower() not in ("host", "cookie")
    }

    body = await request.body()

    try:
        async with httpx.AsyncClient(timeout=20.0, follow_redirects=False) as client:
            upstream = await client.request(
                request.method,
                upstream_url,
                headers=forward_headers,
                content=body if body else None,
            )
    except httpx.RequestError as exc:
        return Response(
            content=f"Agent unreachable: {exc}".encode(),
            status_code=http_status.HTTP_502_BAD_GATEWAY,
            media_type="text/plain",
        )

    response_headers: dict[str, str] = {
        k: v for k, v in upstream.headers.items() if k.lower() not in _HOP_BY_HOP
    }

    response = Response(
        content=upstream.content,
        status_code=upstream.status_code,
        headers=response_headers,
        media_type=upstream.headers.get("content-type"),
    )

    # Promote a valid ?t= into a Path-scoped cookie so the SPA's
    # subsequent fetches inherit auth automatically.
    if token_from_query:
        response.set_cookie(
            key=_UI_COOKIE_NAME,
            value=token_from_query,
            max_age=UI_TOKEN_TTL_MINUTES * 60,
            path=f"/api/gateways/{gateway_id}/ui",
            secure=True,
            httponly=True,
            samesite="lax",
        )

    return response
