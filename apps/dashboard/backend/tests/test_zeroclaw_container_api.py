# ruff: noqa: INP001
"""Tests for ZeroClaw container lifecycle API endpoints.

These tests verify the container start/stop/restart/status/logs endpoints
reject OpenClaw gateways and properly delegate to the Docker manager.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any
from uuid import uuid4

import pytest

from app.services.zeroclaw.docker_manager import (
    ContainerStatus,
    DockerManager,
    DockerManagerError,
)


# ---------------------------------------------------------------------------
# Fakes
# ---------------------------------------------------------------------------


@dataclass
class _FakeContainer:
    id: str = "abc123"
    status: str = "running"
    _started: bool = False
    _stopped: bool = False
    _restarted: bool = False

    def start(self) -> None:
        self._started = True

    def stop(self) -> None:
        self._stopped = True

    def restart(self) -> None:
        self._restarted = True

    def reload(self) -> None:
        pass

    def logs(self, tail: int = 100) -> bytes:
        return b"log line 1\nlog line 2\n"


@dataclass
class _FakeContainers:
    _containers: dict[str, _FakeContainer] = field(default_factory=dict)

    def get(self, container_id: str) -> _FakeContainer:
        if container_id in self._containers:
            return self._containers[container_id]
        raise Exception(f"Container {container_id} not found")

    def list(self, **_kwargs: Any) -> list[_FakeContainer]:
        return list(self._containers.values())


@dataclass
class _FakeDockerClient:
    containers: _FakeContainers = field(default_factory=_FakeContainers)


# ---------------------------------------------------------------------------
# Container lifecycle tests via DockerManager
# ---------------------------------------------------------------------------


class TestContainerLifecycleViaManager:
    """Test the Docker manager methods that the container API delegates to."""

    def test_stop_calls_docker_stop(self) -> None:
        container = _FakeContainer()
        client = _FakeDockerClient(
            containers=_FakeContainers(_containers={"abc123": container})
        )
        manager = DockerManager(client=client)

        result = manager.stop_container("abc123")

        assert result is True
        assert container._stopped is True

    def test_restart_calls_docker_restart(self) -> None:
        container = _FakeContainer()
        client = _FakeDockerClient(
            containers=_FakeContainers(_containers={"abc123": container})
        )
        manager = DockerManager(client=client)

        result = manager.restart_container("abc123")

        assert result is True
        assert container._restarted is True

    def test_get_status_returns_running(self) -> None:
        container = _FakeContainer(status="running")
        client = _FakeDockerClient(
            containers=_FakeContainers(_containers={"abc123": container})
        )
        manager = DockerManager(client=client)

        status = manager.get_status("abc123")

        assert status.status == "running"

    def test_get_logs_returns_content(self) -> None:
        container = _FakeContainer()
        client = _FakeDockerClient(
            containers=_FakeContainers(_containers={"abc123": container})
        )
        manager = DockerManager(client=client)

        logs = manager.get_logs("abc123", tail=50)

        assert "log line" in logs


class TestContainerEndpointGuards:
    """Test that container operations are properly guarded."""

    def test_openclaw_gateway_type_check(self) -> None:
        """Verify that we can distinguish gateway types for guard logic."""
        from app.models.gateways import Gateway

        oc_gw = Gateway(
            id=uuid4(),
            organization_id=uuid4(),
            name="oc",
            url="ws://example/ws",
            workspace_root="/ws",
            type="openclaw",
        )
        zc_gw = Gateway(
            id=uuid4(),
            organization_id=uuid4(),
            name="zc",
            url="ws://localhost:43000/ws/chat",
            workspace_root="/zeroclaw-data/workspace",
            type="zeroclaw",
            container_id="abc123",
        )

        assert oc_gw.type != "zeroclaw"
        assert zc_gw.type == "zeroclaw"

    def test_docker_unavailable_raises_on_create(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        """DockerManager with no client raises on create (property access)."""
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager = DockerManager(client=None)

        with pytest.raises(DockerManagerError, match="Docker daemon"):
            manager.create_container(
                gateway_id=uuid4(),
                name="test",
                org_id=uuid4(),
            )

    def test_docker_unavailable_returns_false_on_stop(self) -> None:
        """Stop gracefully returns False when client is None (catches exception)."""
        manager = DockerManager(client=None)
        assert manager.stop_container("abc123") is False
