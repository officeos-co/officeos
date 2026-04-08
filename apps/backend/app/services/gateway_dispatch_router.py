"""Gateway dispatch router — routes operations to the correct RPC client based on gateway type.

This module is the single entry point for all gateway communication. It checks
``gateway.type`` and delegates to either the OpenClaw RPC client or the ZeroClaw
RPC client.
"""

from __future__ import annotations

import os
from typing import Any, AsyncIterator

from app.services.zeroclaw import zeroclaw_rpc


class UnsupportedGatewayTypeError(ValueError):
    """Raised when gateway.type is not a known value."""


async def dispatch_health(gateway: Any) -> dict[str, Any]:
    """Check gateway health, routing by type."""
    if gateway.type == "openclaw":
        from app.services.openclaw.gateway_rpc import GatewayConfig, openclaw_call

        config = GatewayConfig(
            url=gateway.url,
            token=gateway.token,
            disable_device_pairing=gateway.disable_device_pairing,
            allow_insecure_tls=gateway.allow_insecure_tls,
        )
        try:
            payload = await openclaw_call("health", config=config)
            return {"connected": True, **payload}
        except Exception as exc:
            return {"connected": False, "error": str(exc)}

    if gateway.type == "zeroclaw":
        host, port = _zeroclaw_host_port(gateway)
        return await zeroclaw_rpc.health_check(host, port, gateway.token)

    raise UnsupportedGatewayTypeError(f"Unknown gateway type: {gateway.type!r}")


async def dispatch_sessions(gateway: Any) -> list[dict[str, Any]]:
    """List sessions, routing by type."""
    if gateway.type == "openclaw":
        from app.services.openclaw.gateway_rpc import GatewayConfig, openclaw_call

        config = GatewayConfig(
            url=gateway.url,
            token=gateway.token,
            disable_device_pairing=gateway.disable_device_pairing,
            allow_insecure_tls=gateway.allow_insecure_tls,
        )
        payload = await openclaw_call("sessions.list", config=config)
        return payload.get("sessions", []) if isinstance(payload, dict) else []

    if gateway.type == "zeroclaw":
        host, port = _zeroclaw_host_port(gateway)
        return await zeroclaw_rpc.list_sessions(host, port, gateway.token)

    raise UnsupportedGatewayTypeError(f"Unknown gateway type: {gateway.type!r}")


async def dispatch_session_history(
    gateway: Any, session_id: str
) -> list[dict[str, Any]]:
    """Get session message history, routing by type."""
    if gateway.type == "openclaw":
        from app.services.openclaw.gateway_rpc import GatewayConfig, openclaw_call

        config = GatewayConfig(
            url=gateway.url,
            token=gateway.token,
            disable_device_pairing=gateway.disable_device_pairing,
            allow_insecure_tls=gateway.allow_insecure_tls,
        )
        payload = await openclaw_call(
            "sessions.history", params={"session_id": session_id}, config=config
        )
        return payload.get("messages", []) if isinstance(payload, dict) else []

    if gateway.type == "zeroclaw":
        host, port = _zeroclaw_host_port(gateway)
        return await zeroclaw_rpc.get_session_messages(
            host, port, session_id, gateway.token
        )

    raise UnsupportedGatewayTypeError(f"Unknown gateway type: {gateway.type!r}")


async def dispatch_send_message(
    gateway: Any, session_id: str, content: str
) -> str:
    """Send a message and return the full response, routing by type."""
    if gateway.type == "openclaw":
        from app.services.openclaw.gateway_rpc import GatewayConfig, openclaw_call

        config = GatewayConfig(
            url=gateway.url,
            token=gateway.token,
            disable_device_pairing=gateway.disable_device_pairing,
            allow_insecure_tls=gateway.allow_insecure_tls,
        )
        payload = await openclaw_call(
            "sessions.message",
            params={"session_id": session_id, "content": content},
            config=config,
        )
        return payload.get("response", "") if isinstance(payload, dict) else ""

    if gateway.type == "zeroclaw":
        host, port = _zeroclaw_host_port(gateway)
        return await zeroclaw_rpc.send_message_sync(
            host, port, session_id, content, gateway.token
        )

    raise UnsupportedGatewayTypeError(f"Unknown gateway type: {gateway.type!r}")


def _zeroclaw_host_port(gateway: Any) -> tuple[str, int]:
    """Extract host and port for a ZeroClaw gateway.

    Host resolution order (first non-empty wins):

    1. ``$ZEROCLAW_AGENT_HOST`` — explicit override for dev setups
       where the backend runs in docker-compose and needs to reach
       a `kubectl port-forward` session on the Docker host. In that
       case set it to ``host.docker.internal``. Compose already sets
       this via `docker-compose.yml`.
    2. The hostname portion of ``gateway.url`` — only useful when the
       backend runs in the same cluster as the agent pod, so that
       `eaos-gateway-<id>.default.svc.cluster.local` resolves.
    3. ``localhost`` as the last-resort default, which only works
       when backend and agent are in the same network namespace.
    """

    override = os.environ.get("ZEROCLAW_AGENT_HOST", "").strip()
    port = gateway.host_port or 42617

    if override:
        return override, port

    if gateway.url:
        from urllib.parse import urlparse

        parsed = urlparse(gateway.url)
        if parsed.hostname:
            return parsed.hostname, parsed.port or port

    return "localhost", port
