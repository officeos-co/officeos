# ruff: noqa: INP001
"""Tests for ZeroClaw RPC client — REST API and WebSocket chat protocol."""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from typing import Any
from unittest.mock import AsyncMock

import pytest

import app.services.zeroclaw.zeroclaw_rpc as zeroclaw_rpc
from app.services.zeroclaw.zeroclaw_rpc import (
    ZeroClawAuthError,
    ZeroClawNotFoundError,
    ZeroClawRpcError,
)

# ---------------------------------------------------------------------------
# Fakes for HTTP
# ---------------------------------------------------------------------------


@dataclass
class _FakeHttpResponse:
    status_code: int = 200
    _json: Any = None

    def json(self) -> Any:
        return self._json

    def raise_for_status(self) -> None:
        if self.status_code >= 400:
            raise Exception(f"HTTP {self.status_code}")


@dataclass
class _FakeHttpClient:
    """Captures HTTP calls for assertion."""

    calls: list[dict[str, Any]] = field(default_factory=list)
    response: _FakeHttpResponse = field(default_factory=_FakeHttpResponse)

    async def get(self, url: str, **kwargs: Any) -> _FakeHttpResponse:
        self.calls.append({"method": "GET", "url": url, **kwargs})
        return self.response

    async def put(self, url: str, **kwargs: Any) -> _FakeHttpResponse:
        self.calls.append({"method": "PUT", "url": url, **kwargs})
        return self.response

    async def delete(self, url: str, **kwargs: Any) -> _FakeHttpResponse:
        self.calls.append({"method": "DELETE", "url": url, **kwargs})
        return self.response

    async def __aenter__(self) -> _FakeHttpClient:
        return self

    async def __aexit__(self, *args: Any) -> None:
        pass


def _patch_httpx(
    monkeypatch: pytest.MonkeyPatch,
    response: _FakeHttpResponse | None = None,
) -> _FakeHttpClient:
    """Patch httpx.AsyncClient to return a fake client."""
    fake = _FakeHttpClient(response=response or _FakeHttpResponse())

    class _FakeAsyncClientFactory:
        def __init__(self, **_kwargs: Any) -> None:
            pass

        async def __aenter__(self) -> _FakeHttpClient:
            return fake

        async def __aexit__(self, *args: Any) -> None:
            pass

    monkeypatch.setattr(zeroclaw_rpc.httpx, "AsyncClient", _FakeAsyncClientFactory)
    return fake


# ---------------------------------------------------------------------------
# Fakes for WebSocket
# ---------------------------------------------------------------------------


@dataclass
class _FakeWebSocket:
    """Simulates a WebSocket connection with predefined messages."""

    messages_to_receive: list[str] = field(default_factory=list)
    sent: list[str] = field(default_factory=list)
    _recv_index: int = 0

    async def recv(self) -> str:
        if self._recv_index >= len(self.messages_to_receive):
            raise Exception("No more messages")
        msg = self.messages_to_receive[self._recv_index]
        self._recv_index += 1
        return msg

    async def send(self, data: str) -> None:
        self.sent.append(data)


class _FakeWsConnect:
    """Fake for websockets.connect()."""

    def __init__(self, ws: _FakeWebSocket) -> None:
        self._ws = ws
        self.url: str | None = None

    def __call__(self, url: str, **_kwargs: Any) -> _FakeWsConnect:
        self.url = url
        return self

    async def __aenter__(self) -> _FakeWebSocket:
        return self._ws

    async def __aexit__(self, *args: Any) -> None:
        pass


# ---------------------------------------------------------------------------
# B1. REST API Calls
# ---------------------------------------------------------------------------


class TestRestApiCalls:
    @pytest.mark.asyncio
    async def test_health_check_returns_connected(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        _patch_httpx(
            monkeypatch,
            _FakeHttpResponse(status_code=200, _json={"version": "0.6.7"}),
        )

        result = await zeroclaw_rpc.health_check("localhost", 42617, "tok")

        assert result["connected"] is True
        assert result["version"] == "0.6.7"

    @pytest.mark.asyncio
    async def test_health_check_unreachable_returns_disconnected(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        class _FailingClient:
            def __init__(self, **_kw: Any) -> None:
                pass

            async def __aenter__(self) -> _FailingClient:
                return self

            async def __aexit__(self, *args: Any) -> None:
                pass

            async def get(self, url: str, **_kw: Any) -> Any:
                raise ConnectionError("Connection refused")

        monkeypatch.setattr(zeroclaw_rpc.httpx, "AsyncClient", _FailingClient)

        result = await zeroclaw_rpc.health_check("localhost", 42617)

        assert result["connected"] is False
        assert "error" in result

    @pytest.mark.asyncio
    async def test_get_status_returns_parsed_response(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        _patch_httpx(
            monkeypatch,
            _FakeHttpResponse(
                status_code=200,
                _json={"agent": "running", "sessions": 3, "uptime": "2h"},
            ),
        )

        result = await zeroclaw_rpc.get_status("localhost", 42617, "tok")

        assert result["agent"] == "running"
        assert result["sessions"] == 3

    @pytest.mark.asyncio
    async def test_get_status_auth_failure(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        _patch_httpx(
            monkeypatch, _FakeHttpResponse(status_code=401, _json={})
        )

        with pytest.raises(ZeroClawAuthError):
            await zeroclaw_rpc.get_status("localhost", 42617, "bad-token")

    @pytest.mark.asyncio
    async def test_list_sessions_returns_sessions(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        sessions = [
            {"id": "s1", "name": "Session 1"},
            {"id": "s2", "name": "Session 2"},
        ]
        _patch_httpx(
            monkeypatch,
            _FakeHttpResponse(status_code=200, _json=sessions),
        )

        result = await zeroclaw_rpc.list_sessions("localhost", 42617, "tok")

        assert len(result) == 2
        assert result[0]["id"] == "s1"

    @pytest.mark.asyncio
    async def test_list_sessions_empty(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        _patch_httpx(
            monkeypatch, _FakeHttpResponse(status_code=200, _json=[])
        )

        result = await zeroclaw_rpc.list_sessions("localhost", 42617, "tok")

        assert result == []

    @pytest.mark.asyncio
    async def test_get_session_messages(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        messages = [
            {"role": "user", "content": "Hello"},
            {"role": "assistant", "content": "Hi!"},
        ]
        _patch_httpx(
            monkeypatch,
            _FakeHttpResponse(status_code=200, _json=messages),
        )

        result = await zeroclaw_rpc.get_session_messages(
            "localhost", 42617, "s1", "tok"
        )

        assert len(result) == 2
        assert result[0]["role"] == "user"

    @pytest.mark.asyncio
    async def test_get_session_messages_not_found(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        _patch_httpx(
            monkeypatch, _FakeHttpResponse(status_code=404, _json={})
        )

        with pytest.raises(ZeroClawNotFoundError):
            await zeroclaw_rpc.get_session_messages(
                "localhost", 42617, "nonexistent", "tok"
            )

    @pytest.mark.asyncio
    async def test_delete_session(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        _patch_httpx(
            monkeypatch, _FakeHttpResponse(status_code=200, _json={})
        )

        result = await zeroclaw_rpc.delete_session(
            "localhost", 42617, "s1", "tok"
        )

        assert result is True

    @pytest.mark.asyncio
    async def test_get_config(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        _patch_httpx(
            monkeypatch,
            _FakeHttpResponse(
                status_code=200,
                _json={"provider": "openrouter", "model": "claude-sonnet"},
            ),
        )

        result = await zeroclaw_rpc.get_config("localhost", 42617, "tok")

        assert result["provider"] == "openrouter"

    @pytest.mark.asyncio
    async def test_set_config(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        fake = _patch_httpx(
            monkeypatch, _FakeHttpResponse(status_code=200, _json={})
        )

        result = await zeroclaw_rpc.set_config(
            "localhost", 42617, {"model": "gpt-4o"}, "tok"
        )

        assert result is True
        assert fake.calls[0]["method"] == "PUT"
        assert fake.calls[0]["json"] == {"model": "gpt-4o"}


# ---------------------------------------------------------------------------
# B2. Auth Header
# ---------------------------------------------------------------------------


class TestAuthHeader:
    @pytest.mark.asyncio
    async def test_rest_calls_include_bearer_token(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        fake = _patch_httpx(
            monkeypatch,
            _FakeHttpResponse(status_code=200, _json={"ok": True}),
        )

        await zeroclaw_rpc.get_status("localhost", 42617, "my-secret-token")

        assert fake.calls[0]["headers"] == {
            "Authorization": "Bearer my-secret-token"
        }

    @pytest.mark.asyncio
    async def test_rest_calls_without_token_skip_auth_header(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        fake = _patch_httpx(
            monkeypatch,
            _FakeHttpResponse(status_code=200, _json={"ok": True}),
        )

        await zeroclaw_rpc.get_status("localhost", 42617, None)

        assert fake.calls[0]["headers"] == {}


# ---------------------------------------------------------------------------
# B3. WebSocket Chat
# ---------------------------------------------------------------------------


class TestWebSocketChat:
    @pytest.mark.asyncio
    async def test_send_message_connects_with_session_and_token(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        ws = _FakeWebSocket(
            messages_to_receive=[
                json.dumps({"type": "session_start", "session_id": "s1"}),
                json.dumps({"type": "done", "full_response": "Hi"}),
            ]
        )
        fake_connect = _FakeWsConnect(ws)
        monkeypatch.setattr(zeroclaw_rpc.websockets, "connect", fake_connect)

        events = []
        async for event in zeroclaw_rpc.send_message(
            "localhost", 42617, "s1", "Hello", "my-token"
        ):
            events.append(event)

        assert fake_connect.url == "ws://localhost:42617/ws/chat?session_id=s1&token=my-token"

    @pytest.mark.asyncio
    async def test_send_message_sends_correct_format(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        ws = _FakeWebSocket(
            messages_to_receive=[
                json.dumps({"type": "session_start", "session_id": "s1"}),
                json.dumps({"type": "done", "full_response": "Hi"}),
            ]
        )
        fake_connect = _FakeWsConnect(ws)
        monkeypatch.setattr(zeroclaw_rpc.websockets, "connect", fake_connect)

        async for _ in zeroclaw_rpc.send_message(
            "localhost", 42617, "s1", "Hello", "tok"
        ):
            pass

        assert len(ws.sent) == 1
        sent_msg = json.loads(ws.sent[0])
        assert sent_msg == {"type": "message", "content": "Hello"}

    @pytest.mark.asyncio
    async def test_send_message_yields_chunk_events(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        ws = _FakeWebSocket(
            messages_to_receive=[
                json.dumps({"type": "session_start", "session_id": "s1"}),
                json.dumps({"type": "chunk", "content": "Hi "}),
                json.dumps({"type": "chunk", "content": "there!"}),
                json.dumps({"type": "done", "full_response": "Hi there!"}),
            ]
        )
        fake_connect = _FakeWsConnect(ws)
        monkeypatch.setattr(zeroclaw_rpc.websockets, "connect", fake_connect)

        events = []
        async for event in zeroclaw_rpc.send_message(
            "localhost", 42617, "s1", "Hello", "tok"
        ):
            events.append(event)

        chunk_events = [e for e in events if e.get("type") == "chunk"]
        assert len(chunk_events) == 2
        assert chunk_events[0]["content"] == "Hi "
        assert chunk_events[1]["content"] == "there!"

    @pytest.mark.asyncio
    async def test_send_message_yields_tool_call_events(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        ws = _FakeWebSocket(
            messages_to_receive=[
                json.dumps({"type": "session_start", "session_id": "s1"}),
                json.dumps(
                    {"type": "tool_call", "name": "shell", "args": {"cmd": "ls"}}
                ),
                json.dumps(
                    {"type": "tool_result", "name": "shell", "output": "file.txt"}
                ),
                json.dumps({"type": "done", "full_response": "Done"}),
            ]
        )
        fake_connect = _FakeWsConnect(ws)
        monkeypatch.setattr(zeroclaw_rpc.websockets, "connect", fake_connect)

        events = []
        async for event in zeroclaw_rpc.send_message(
            "localhost", 42617, "s1", "list files", "tok"
        ):
            events.append(event)

        tool_calls = [e for e in events if e.get("type") == "tool_call"]
        assert len(tool_calls) == 1
        assert tool_calls[0]["name"] == "shell"

    @pytest.mark.asyncio
    async def test_send_message_completes_on_done(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        ws = _FakeWebSocket(
            messages_to_receive=[
                json.dumps({"type": "session_start", "session_id": "s1"}),
                json.dumps({"type": "chunk", "content": "Hi"}),
                json.dumps({"type": "done", "full_response": "Hi"}),
                # This should never be reached:
                json.dumps({"type": "chunk", "content": "SHOULD NOT SEE"}),
            ]
        )
        fake_connect = _FakeWsConnect(ws)
        monkeypatch.setattr(zeroclaw_rpc.websockets, "connect", fake_connect)

        events = []
        async for event in zeroclaw_rpc.send_message(
            "localhost", 42617, "s1", "Hello", "tok"
        ):
            events.append(event)

        types = [e.get("type") for e in events]
        assert "done" in types
        assert types[-1] == "done"
        assert "SHOULD NOT SEE" not in str(events)

    @pytest.mark.asyncio
    async def test_send_message_raises_on_error_event(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        ws = _FakeWebSocket(
            messages_to_receive=[
                json.dumps({"type": "session_start", "session_id": "s1"}),
                json.dumps({"type": "error", "message": "Provider unavailable"}),
            ]
        )
        fake_connect = _FakeWsConnect(ws)
        monkeypatch.setattr(zeroclaw_rpc.websockets, "connect", fake_connect)

        with pytest.raises(ZeroClawRpcError, match="Provider unavailable"):
            async for _ in zeroclaw_rpc.send_message(
                "localhost", 42617, "s1", "Hello", "tok"
            ):
                pass

    @pytest.mark.asyncio
    async def test_send_message_sync_collects_full_response(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        ws = _FakeWebSocket(
            messages_to_receive=[
                json.dumps({"type": "session_start", "session_id": "s1"}),
                json.dumps({"type": "chunk", "content": "Hello "}),
                json.dumps({"type": "chunk", "content": "world!"}),
                json.dumps({"type": "done", "full_response": "Hello world!"}),
            ]
        )
        fake_connect = _FakeWsConnect(ws)
        monkeypatch.setattr(zeroclaw_rpc.websockets, "connect", fake_connect)

        result = await zeroclaw_rpc.send_message_sync(
            "localhost", 42617, "s1", "Hi", "tok"
        )

        assert result == "Hello world!"

    @pytest.mark.asyncio
    async def test_send_message_timeout(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        import asyncio

        class _HangingWebSocket:
            async def recv(self) -> str:
                await asyncio.sleep(10)  # Will be interrupted by timeout
                return ""

            async def send(self, data: str) -> None:
                pass

        # First message is session_start, second hangs
        call_count = 0

        class _HangingWsAfterStart:
            async def recv(self) -> str:
                nonlocal call_count
                call_count += 1
                if call_count == 1:
                    return json.dumps(
                        {"type": "session_start", "session_id": "s1"}
                    )
                # Second recv (after send) should hang
                await asyncio.sleep(10)
                return ""

            async def send(self, data: str) -> None:
                pass

        class _FakeHangConnect:
            def __call__(self, url: str, **_kw: Any) -> _FakeHangConnect:
                return self

            async def __aenter__(self) -> _HangingWsAfterStart:
                return _HangingWsAfterStart()

            async def __aexit__(self, *args: Any) -> None:
                pass

        monkeypatch.setattr(
            zeroclaw_rpc.websockets, "connect", _FakeHangConnect()
        )

        with pytest.raises(ZeroClawRpcError, match="Timeout"):
            async for _ in zeroclaw_rpc.send_message(
                "localhost", 42617, "s1", "Hello", "tok", timeout=0.1
            ):
                pass
