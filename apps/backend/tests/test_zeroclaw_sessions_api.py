# ruff: noqa: INP001
"""Tests for ZeroClaw session/status operations via the dispatch router.

These tests verify that session-related operations are correctly routed
to the ZeroClaw RPC client for ZeroClaw gateways.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any

import pytest

import app.services.gateway_dispatch_router as dispatch_router


# ---------------------------------------------------------------------------
# Fakes
# ---------------------------------------------------------------------------


@dataclass
class _FakeZeroClawGateway:
    type: str = "zeroclaw"
    url: str = "ws://localhost:43000/ws/chat"
    token: str = "zc-token"
    host_port: int = 43000
    disable_device_pairing: bool = False
    allow_insecure_tls: bool = False


def _patch_zeroclaw_rpc(monkeypatch: pytest.MonkeyPatch) -> list[dict[str, Any]]:
    """Patch ZeroClaw RPC functions to capture calls."""
    captured: list[dict[str, Any]] = []

    async def _fake_health(host: str, port: int, token: str | None = None) -> dict[str, Any]:
        captured.append({"fn": "health_check", "host": host, "port": port, "token": token})
        return {"connected": True, "version": "0.6.7"}

    async def _fake_sessions(host: str, port: int, token: str | None = None) -> list[dict[str, Any]]:
        captured.append({"fn": "list_sessions", "host": host, "port": port})
        return [
            {"id": "s1", "name": "Session 1", "message_count": 5},
            {"id": "s2", "name": "Session 2", "message_count": 2},
        ]

    async def _fake_history(
        host: str, port: int, session_id: str, token: str | None = None
    ) -> list[dict[str, Any]]:
        captured.append({"fn": "get_session_messages", "session_id": session_id})
        return [
            {"role": "user", "content": "Hello ZeroClaw"},
            {"role": "assistant", "content": "Hi! How can I help?"},
        ]

    async def _fake_send(
        host: str, port: int, session_id: str, content: str, token: str | None = None, **kw: Any
    ) -> str:
        captured.append({"fn": "send_message_sync", "session_id": session_id, "content": content})
        return "ZeroClaw response"

    monkeypatch.setattr(dispatch_router.zeroclaw_rpc, "health_check", _fake_health)
    monkeypatch.setattr(dispatch_router.zeroclaw_rpc, "list_sessions", _fake_sessions)
    monkeypatch.setattr(dispatch_router.zeroclaw_rpc, "get_session_messages", _fake_history)
    monkeypatch.setattr(dispatch_router.zeroclaw_rpc, "send_message_sync", _fake_send)
    return captured


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------


class TestZeroClawGatewayStatus:
    @pytest.mark.asyncio
    async def test_returns_connected(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        captured = _patch_zeroclaw_rpc(monkeypatch)
        gw = _FakeZeroClawGateway()

        result = await dispatch_router.dispatch_health(gw)

        assert result["connected"] is True
        assert captured[0]["fn"] == "health_check"
        assert captured[0]["port"] == 43000
        assert captured[0]["token"] == "zc-token"

    @pytest.mark.asyncio
    async def test_returns_disconnected_on_failure(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        async def _failing_health(host: str, port: int, token: str | None = None) -> dict[str, Any]:
            return {"connected": False, "error": "Connection refused"}

        monkeypatch.setattr(dispatch_router.zeroclaw_rpc, "health_check", _failing_health)
        gw = _FakeZeroClawGateway()

        result = await dispatch_router.dispatch_health(gw)

        assert result["connected"] is False
        assert "error" in result


class TestZeroClawListSessions:
    @pytest.mark.asyncio
    async def test_returns_sessions(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        captured = _patch_zeroclaw_rpc(monkeypatch)
        gw = _FakeZeroClawGateway()

        sessions = await dispatch_router.dispatch_sessions(gw)

        assert len(sessions) == 2
        assert sessions[0]["id"] == "s1"
        assert captured[0]["fn"] == "list_sessions"


class TestZeroClawSessionHistory:
    @pytest.mark.asyncio
    async def test_returns_messages(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        captured = _patch_zeroclaw_rpc(monkeypatch)
        gw = _FakeZeroClawGateway()

        messages = await dispatch_router.dispatch_session_history(gw, "s1")

        assert len(messages) == 2
        assert messages[0]["role"] == "user"
        assert messages[1]["content"] == "Hi! How can I help?"
        assert captured[0]["session_id"] == "s1"


class TestZeroClawSendMessage:
    @pytest.mark.asyncio
    async def test_returns_response(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        captured = _patch_zeroclaw_rpc(monkeypatch)
        gw = _FakeZeroClawGateway()

        response = await dispatch_router.dispatch_send_message(
            gw, "s1", "What's the weather?"
        )

        assert response == "ZeroClaw response"
        assert captured[0]["fn"] == "send_message_sync"
        assert captured[0]["content"] == "What's the weather?"
