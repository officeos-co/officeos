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
- Request/response bodies are buffered, not streamed. Fine for static
  assets + JSON API calls; rethink if the agent dashboard ever serves
  multi-MB downloads.
"""

from __future__ import annotations

from typing import Any
from uuid import UUID

import asyncio

import httpx
import websockets
from fastapi import APIRouter, HTTPException, Request, Response, WebSocket, WebSocketDisconnect
from fastapi import status as http_status
from websockets.exceptions import ConnectionClosed
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

    # (ws route defined below)

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


# ─── websocket proxy ────────────────────────────────────────────────
#
# The zeroclaw SPA opens `{base}{path_prefix}/ws/chat?token=...&session_id=...`
# for live chat, and `{base}{path_prefix}/ws/canvas/{id}` for the
# canvas collaboration feature. We bridge both with a single catch-all
# route that forwards frames in both directions.
#
# Auth: browsers cannot set headers on `new WebSocket(...)`, so we
# read the `gateway_ui_session` cookie (set by the HTTP proxy on the
# SPA's first load) — or, as a fallback, accept `?t=<jwt>` on the WS
# URL. The upstream pod has `require_pairing=false` so we strip any
# `token=...` query param the SPA adds from its own localStorage; the
# pod would reject it anyway.


def _authorize_ws(websocket: WebSocket, gateway_id: UUID) -> str | None:
    """Return the validated token (query or cookie) or None if unauth."""
    token = websocket.query_params.get("t", "").strip() or None
    if token:
        try:
            verify_gateway_ui_token(token, expected_gateway_id=str(gateway_id))
            return token
        except SessionTokenError:
            return None

    cookie = websocket.cookies.get(_UI_COOKIE_NAME)
    if cookie:
        try:
            verify_gateway_ui_token(cookie, expected_gateway_id=str(gateway_id))
            return cookie
        except SessionTokenError:
            return None
    return None


@router.websocket("/gateways/{gateway_id}/ui/ws/{sub_path:path}")
async def proxy_gateway_ui_ws(
    websocket: WebSocket,
    gateway_id: UUID,
    sub_path: str,
    session: AsyncSession = SESSION_DEP,
) -> None:
    """Bridge a WebSocket between the browser and the agent pod."""
    if not _authorize_ws(websocket, gateway_id):
        await websocket.close(code=4401)  # application-level "unauthorized"
        return

    gateway = await Gateway.objects.by_id(gateway_id).first(session)
    if gateway is None:
        await websocket.close(code=4404)
        return

    host, port = _zeroclaw_host_port(gateway)

    # Rebuild the upstream URL using the same path prefix the agent
    # expects, stripping our own `t=` auth param.
    upstream_path = f"/api/gateways/{gateway_id}/ui/ws/{sub_path}"
    forward_query_items = [
        (k, v) for k, v in websocket.query_params.multi_items() if k != "t"
    ]
    if forward_query_items:
        from urllib.parse import urlencode

        upstream_url = f"ws://{host}:{port}{upstream_path}?{urlencode(forward_query_items)}"
    else:
        upstream_url = f"ws://{host}:{port}{upstream_path}"

    # Forward the Sec-WebSocket-Protocol subprotocols the browser
    # requested (the zeroclaw SPA always sends `zeroclaw.v1`).
    requested_subprotocols = websocket.headers.get("sec-websocket-protocol", "")
    subprotocols: list[str] = [
        s.strip() for s in requested_subprotocols.split(",") if s.strip()
    ]

    try:
        upstream_ws = await websockets.connect(
            upstream_url,
            subprotocols=subprotocols or None,
            open_timeout=10,
            max_size=None,
        )
    except Exception as exc:
        # Accept first so the client sees a clean close frame.
        await websocket.accept()
        await websocket.close(code=1011, reason=f"agent unreachable: {exc}"[:120])
        return

    # Accept with the same subprotocol the upstream negotiated so the
    # browser's `ws.protocol` matches what the SPA expects.
    negotiated = upstream_ws.subprotocol
    await websocket.accept(subprotocol=negotiated)

    async def client_to_upstream() -> None:
        try:
            while True:
                msg = await websocket.receive()
                if msg["type"] == "websocket.disconnect":
                    return
                if (data := msg.get("text")) is not None:
                    await upstream_ws.send(data)
                elif (data := msg.get("bytes")) is not None:
                    await upstream_ws.send(data)
        except (WebSocketDisconnect, ConnectionClosed):
            return

    async def upstream_to_client() -> None:
        try:
            async for frame in upstream_ws:
                if isinstance(frame, str):
                    await websocket.send_text(frame)
                else:
                    await websocket.send_bytes(frame)
        except ConnectionClosed:
            return

    task_c = asyncio.create_task(client_to_upstream())
    task_u = asyncio.create_task(upstream_to_client())
    try:
        done, pending = await asyncio.wait(
            {task_c, task_u}, return_when=asyncio.FIRST_COMPLETED
        )
        for t in pending:
            t.cancel()
    finally:
        await upstream_ws.close()
        try:
            await websocket.close()
        except RuntimeError:
            pass
