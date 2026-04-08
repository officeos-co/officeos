# ruff: noqa: INP001
"""Tests for the gateway dispatch router — routes by gateway.type."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any

import pytest

import app.services.gateway_dispatch_router as dispatch_router
import app.services.zeroclaw.zeroclaw_rpc as zeroclaw_rpc_mod
from app.services.gateway_dispatch_router import UnsupportedGatewayTypeError

# ---------------------------------------------------------------------------
# Fakes
# ---------------------------------------------------------------------------


@dataclass
class _FakeGateway:
    type: str = "openclaw"
    url: str = "ws://gateway.example/ws"
    token: str | None = "test-token"
    disable_device_pairing: bool = True
    allow_insecure_tls: bool = False
    host_port: int | None = None


# ---------------------------------------------------------------------------
# Capture helpers
# ---------------------------------------------------------------------------


def _capture_openclaw_call(monkeypatch: pytest.MonkeyPatch) -> list[dict[str, Any]]:
    """Patch openclaw_call and return a list that captures calls."""
    captured: list[dict[str, Any]] = []

    async def _fake_openclaw_call(
        method: str, params: dict[str, Any] | None = None, *, config: Any
    ) -> dict[str, Any]:
        captured.append({"method": method, "params": params, "config": config})
        if method == "sessions.list":
            return {"sessions": [{"id": "oc-s1"}]}
        if method == "sessions.history":
            return {"messages": [{"role": "user", "content": "hi"}]}
        if method == "sessions.message":
            return {"response": "OpenClaw says hi"}
        return {"ok": True}

    # We need to patch it where the dispatch_router imports it from
    monkeypatch.setattr(
        "app.services.openclaw.gateway_rpc.openclaw_call", _fake_openclaw_call
    )
    return captured


def _capture_zeroclaw_rpc(monkeypatch: pytest.MonkeyPatch) -> list[dict[str, Any]]:
    """Patch zeroclaw_rpc functions and return a list that captures calls."""
    captured: list[dict[str, Any]] = []

    async def _fake_health(host: str, port: int, token: str | None = None) -> dict[str, Any]:
        captured.append({"fn": "health_check", "host": host, "port": port})
        return {"connected": True}

    async def _fake_sessions(host: str, port: int, token: str | None = None) -> list[dict[str, Any]]:
        captured.append({"fn": "list_sessions", "host": host, "port": port})
        return [{"id": "zc-s1"}]

    async def _fake_history(
        host: str, port: int, session_id: str, token: str | None = None
    ) -> list[dict[str, Any]]:
        captured.append({"fn": "get_session_messages", "session_id": session_id})
        return [{"role": "user", "content": "hi from zc"}]

    async def _fake_send(
        host: str, port: int, session_id: str, content: str, token: str | None = None, **kw: Any
    ) -> str:
        captured.append({"fn": "send_message_sync", "content": content})
        return "ZeroClaw says hi"

    monkeypatch.setattr(dispatch_router.zeroclaw_rpc, "health_check", _fake_health)
    monkeypatch.setattr(dispatch_router.zeroclaw_rpc, "list_sessions", _fake_sessions)
    monkeypatch.setattr(dispatch_router.zeroclaw_rpc, "get_session_messages", _fake_history)
    monkeypatch.setattr(dispatch_router.zeroclaw_rpc, "send_message_sync", _fake_send)
    return captured


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------


class TestDispatchHealth:
    @pytest.mark.asyncio
    async def test_openclaw_calls_openclaw_rpc(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        captured = _capture_openclaw_call(monkeypatch)
        gw = _FakeGateway(type="openclaw")

        result = await dispatch_router.dispatch_health(gw)

        assert len(captured) == 1
        assert captured[0]["method"] == "health"
        assert result["connected"] is True

    @pytest.mark.asyncio
    async def test_zeroclaw_calls_zeroclaw_rpc(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        captured = _capture_zeroclaw_rpc(monkeypatch)
        gw = _FakeGateway(type="zeroclaw", host_port=43001)

        result = await dispatch_router.dispatch_health(gw)

        assert len(captured) == 1
        assert captured[0]["fn"] == "health_check"
        assert captured[0]["port"] == 43001
        assert result["connected"] is True


class TestDispatchSessions:
    @pytest.mark.asyncio
    async def test_openclaw(self, monkeypatch: pytest.MonkeyPatch) -> None:
        captured = _capture_openclaw_call(monkeypatch)
        gw = _FakeGateway(type="openclaw")

        result = await dispatch_router.dispatch_sessions(gw)

        assert captured[0]["method"] == "sessions.list"
        assert result == [{"id": "oc-s1"}]

    @pytest.mark.asyncio
    async def test_zeroclaw(self, monkeypatch: pytest.MonkeyPatch) -> None:
        captured = _capture_zeroclaw_rpc(monkeypatch)
        gw = _FakeGateway(type="zeroclaw", host_port=43002)

        result = await dispatch_router.dispatch_sessions(gw)

        assert captured[0]["fn"] == "list_sessions"
        assert result == [{"id": "zc-s1"}]


class TestDispatchSendMessage:
    @pytest.mark.asyncio
    async def test_openclaw(self, monkeypatch: pytest.MonkeyPatch) -> None:
        captured = _capture_openclaw_call(monkeypatch)
        gw = _FakeGateway(type="openclaw")

        result = await dispatch_router.dispatch_send_message(gw, "s1", "hello")

        assert captured[0]["method"] == "sessions.message"
        assert result == "OpenClaw says hi"

    @pytest.mark.asyncio
    async def test_zeroclaw(self, monkeypatch: pytest.MonkeyPatch) -> None:
        captured = _capture_zeroclaw_rpc(monkeypatch)
        gw = _FakeGateway(type="zeroclaw", host_port=43003)

        result = await dispatch_router.dispatch_send_message(gw, "s1", "hello")

        assert captured[0]["fn"] == "send_message_sync"
        assert result == "ZeroClaw says hi"


class TestDispatchHistory:
    @pytest.mark.asyncio
    async def test_zeroclaw(self, monkeypatch: pytest.MonkeyPatch) -> None:
        captured = _capture_zeroclaw_rpc(monkeypatch)
        gw = _FakeGateway(type="zeroclaw", host_port=43004)

        result = await dispatch_router.dispatch_session_history(gw, "s1")

        assert captured[0]["fn"] == "get_session_messages"
        assert result[0]["content"] == "hi from zc"


class TestDispatchUnknownType:
    @pytest.mark.asyncio
    async def test_unknown_type_raises(self) -> None:
        gw = _FakeGateway(type="foobar")

        with pytest.raises(UnsupportedGatewayTypeError, match="foobar"):
            await dispatch_router.dispatch_health(gw)

    @pytest.mark.asyncio
    async def test_unknown_type_raises_for_sessions(self) -> None:
        gw = _FakeGateway(type="foobar")

        with pytest.raises(UnsupportedGatewayTypeError):
            await dispatch_router.dispatch_sessions(gw)

    @pytest.mark.asyncio
    async def test_unknown_type_raises_for_send(self) -> None:
        gw = _FakeGateway(type="foobar")

        with pytest.raises(UnsupportedGatewayTypeError):
            await dispatch_router.dispatch_send_message(gw, "s1", "hi")
