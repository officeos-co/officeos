"""Reverse-proxy for the zeroclaw agent's embedded web dashboard.

Every zeroclaw pod serves its own SPA at `:42617/<prefix>/` where the
prefix is supplied via the `ZEROCLAW_PATH_PREFIX` env var we set at
pod creation (see `k8s_manager._build_env_vars`). We pass each pod
its own unique prefix — `/api/gateways/{id}/ui` — so this proxy
route can forward requests transparently without URL rewriting:
whatever path the browser requests on the EAOS backend is exactly
what gets forwarded to the pod.

Public surface:

    GET/POST/etc  /api/gateways/{gateway_id}/ui/{path:path}

Auth: dashboard user session (ORG_MEMBER_DEP). The agent itself has
no direct exposure to the internet — this is the only way to reach
its UI from outside the cluster.

Caveats:
- WebSocket upgrade (ws/chat) is NOT yet proxied. This only covers
  HTTP request/response. Chat streaming needs a separate ws route.
- Request/response bodies are buffered, not streamed. Fine for
  static assets + JSON API calls; rethink if the agent dashboard
  ever serves multi-MB downloads.
- Cookies/auth on the upstream side are ignored — the agent pod
  has `ZEROCLAW_REQUIRE_PAIRING=false` set so no bearer is needed.
"""

from __future__ import annotations

from typing import Any
from urllib.parse import urlparse
from uuid import UUID

import httpx
from fastapi import APIRouter, Request, Response
from fastapi import status as http_status
from sqlmodel.ext.asyncio.session import AsyncSession

from app.api.deps import ORG_MEMBER_DEP, SESSION_DEP
from app.models.gateways import Gateway
from app.services.gateway_dispatch_router import _zeroclaw_host_port
from app.services.organizations import OrganizationContext

router = APIRouter(tags=["gateway_ui"])

# Hop-by-hop response headers that a reverse proxy must NOT forward.
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
    # Content-Length/Content-Encoding must be recomputed by the
    # framework after httpx has decoded the body.
    "content-length",
    "content-encoding",
}


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
    ctx: OrganizationContext = ORG_MEMBER_DEP,
    session: AsyncSession = SESSION_DEP,
) -> Response:
    """Forward a request to the zeroclaw pod's embedded web UI."""

    # Look up the gateway row. Org-scoped so users can't probe other
    # orgs' agents by guessing UUIDs.
    gateway = (
        await Gateway.objects.by_id(gateway_id)
        .filter_by(organization_id=ctx.organization.id)
        .first(session)
    )
    if gateway is None:
        return Response(status_code=http_status.HTTP_404_NOT_FOUND)

    host, port = _zeroclaw_host_port(gateway)

    # The pod's path_prefix is exactly `/api/gateways/<id>/ui`, so
    # we forward the full incoming path unchanged. Preserves query
    # string too.
    upstream_path = f"/api/gateways/{gateway_id}/ui"
    if sub_path:
        upstream_path = f"{upstream_path}/{sub_path}"
    upstream_url = f"http://{host}:{port}{upstream_path}"
    if request.url.query:
        upstream_url = f"{upstream_url}?{request.url.query}"

    # Strip hop-by-hop request headers and the Host header (httpx
    # sets its own based on the target URL).
    forward_headers = {
        k: v
        for k, v in request.headers.items()
        if k.lower() not in _HOP_BY_HOP and k.lower() != "host"
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

    return Response(
        content=upstream.content,
        status_code=upstream.status_code,
        headers=response_headers,
        media_type=upstream.headers.get("content-type"),
    )
