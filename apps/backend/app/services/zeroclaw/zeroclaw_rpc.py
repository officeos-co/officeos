"""ZeroClaw RPC client — speaks ZeroClaw's native REST API + WebSocket protocol.

Unlike the OpenClaw RPC client (Protocol v3 envelope), this client talks directly
to ZeroClaw's HTTP API endpoints and WebSocket chat interface.
"""

from __future__ import annotations

import json
from typing import Any, AsyncIterator

import httpx
import websockets


class ZeroClawRpcError(Exception):
    """Raised for RPC-level failures communicating with a ZeroClaw instance."""


class ZeroClawAuthError(ZeroClawRpcError):
    """Raised when authentication with ZeroClaw fails."""


class ZeroClawNotFoundError(ZeroClawRpcError):
    """Raised when a requested resource does not exist on ZeroClaw."""


def _base_url(host: str, port: int) -> str:
    return f"http://{host}:{port}"


def _auth_headers(token: str | None) -> dict[str, str]:
    if token:
        return {"Authorization": f"Bearer {token}"}
    return {}


def _raise_for_status(response: httpx.Response) -> None:
    if response.status_code == 401:
        raise ZeroClawAuthError("Authentication failed")
    if response.status_code == 404:
        raise ZeroClawNotFoundError("Resource not found")
    response.raise_for_status()


async def health_check(host: str, port: int, token: str | None = None) -> dict[str, Any]:
    """Check ZeroClaw instance health via GET /api/health."""
    try:
        async with httpx.AsyncClient(timeout=5.0) as client:
            resp = await client.get(
                f"{_base_url(host, port)}/api/health",
                headers=_auth_headers(token),
            )
            resp.raise_for_status()
            return {"connected": True, **resp.json()}
    except Exception as exc:
        if isinstance(exc, ZeroClawAuthError):
            raise
        return {"connected": False, "error": str(exc)}


async def get_status(host: str, port: int, token: str | None = None) -> dict[str, Any]:
    """Get ZeroClaw instance status via GET /api/status."""
    async with httpx.AsyncClient(timeout=10.0) as client:
        resp = await client.get(
            f"{_base_url(host, port)}/api/status",
            headers=_auth_headers(token),
        )
        _raise_for_status(resp)
        return resp.json()


async def list_sessions(
    host: str, port: int, token: str | None = None
) -> list[dict[str, Any]]:
    """List sessions via GET /api/sessions."""
    async with httpx.AsyncClient(timeout=10.0) as client:
        resp = await client.get(
            f"{_base_url(host, port)}/api/sessions",
            headers=_auth_headers(token),
        )
        _raise_for_status(resp)
        data = resp.json()
        return data if isinstance(data, list) else data.get("sessions", [])


async def get_session_messages(
    host: str, port: int, session_id: str, token: str | None = None
) -> list[dict[str, Any]]:
    """Get session message history via GET /api/sessions/{id}/messages."""
    async with httpx.AsyncClient(timeout=10.0) as client:
        resp = await client.get(
            f"{_base_url(host, port)}/api/sessions/{session_id}/messages",
            headers=_auth_headers(token),
        )
        _raise_for_status(resp)
        data = resp.json()
        return data if isinstance(data, list) else data.get("messages", [])


async def delete_session(
    host: str, port: int, session_id: str, token: str | None = None
) -> bool:
    """Delete a session via DELETE /api/sessions/{id}."""
    async with httpx.AsyncClient(timeout=10.0) as client:
        resp = await client.delete(
            f"{_base_url(host, port)}/api/sessions/{session_id}",
            headers=_auth_headers(token),
        )
        _raise_for_status(resp)
        return True


async def get_config(host: str, port: int, token: str | None = None) -> dict[str, Any]:
    """Get ZeroClaw config via GET /api/config."""
    async with httpx.AsyncClient(timeout=10.0) as client:
        resp = await client.get(
            f"{_base_url(host, port)}/api/config",
            headers=_auth_headers(token),
        )
        _raise_for_status(resp)
        return resp.json()


async def set_config(
    host: str, port: int, config: dict[str, Any], token: str | None = None
) -> bool:
    """Update ZeroClaw config via PUT /api/config."""
    async with httpx.AsyncClient(timeout=10.0) as client:
        resp = await client.put(
            f"{_base_url(host, port)}/api/config",
            json=config,
            headers=_auth_headers(token),
        )
        _raise_for_status(resp)
        return True


async def send_message(
    host: str,
    port: int,
    session_id: str,
    content: str,
    token: str | None = None,
    *,
    timeout: float = 120.0,
) -> AsyncIterator[dict[str, Any]]:
    """Send a message via WebSocket and yield streaming response events.

    Connects to ws://host:port/ws/chat, sends the message, and yields
    events (chunk, tool_call, tool_result, done, error) until completion.
    """
    url = f"ws://{host}:{port}/ws/chat?session_id={session_id}"
    if token:
        url += f"&token={token}"

    async with websockets.connect(url, open_timeout=timeout) as ws:
        # Wait for session_start event
        raw = await ws.recv()
        start_msg = json.loads(raw) if isinstance(raw, str) else json.loads(raw.decode())
        if start_msg.get("type") != "session_start":
            yield start_msg

        # Send the message
        await ws.send(json.dumps({"type": "message", "content": content}))

        # Yield streaming events until done/error
        import asyncio

        while True:
            try:
                raw = await asyncio.wait_for(ws.recv(), timeout=timeout)
            except asyncio.TimeoutError:
                raise ZeroClawRpcError(f"Timeout after {timeout}s waiting for response")
            event = json.loads(raw) if isinstance(raw, str) else json.loads(raw.decode())
            event_type = event.get("type", "")

            if event_type == "error":
                raise ZeroClawRpcError(event.get("message", "Unknown error"))

            yield event

            if event_type == "done":
                break


async def send_message_sync(
    host: str,
    port: int,
    session_id: str,
    content: str,
    token: str | None = None,
    *,
    timeout: float = 120.0,
) -> str:
    """Send a message and return the full response (blocking until done)."""
    chunks: list[str] = []
    full_response = ""

    async for event in send_message(
        host, port, session_id, content, token, timeout=timeout
    ):
        if event.get("type") == "chunk":
            chunks.append(event.get("content", ""))
        elif event.get("type") == "done":
            full_response = event.get("full_response", "".join(chunks))
            break

    return full_response or "".join(chunks)
