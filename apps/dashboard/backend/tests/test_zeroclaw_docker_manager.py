# ruff: noqa: INP001
"""Tests for ZeroClaw Docker container manager."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any
from uuid import UUID, uuid4

import pytest

from app.services.zeroclaw.docker_manager import (
    DEFAULT_IMAGE,
    MANAGED_BY_LABEL,
    ZEROCLAW_INTERNAL_PORT,
    ContainerStatus,
    DockerManager,
    DockerManagerError,
)

# ---------------------------------------------------------------------------
# Fakes
# ---------------------------------------------------------------------------

FAKE_CONTAINER_ID = "abc123deadbeef"


@dataclass
class _FakeContainer:
    id: str = FAKE_CONTAINER_ID
    status: str = "running"
    ports: dict[str, list[dict[str, str]]] | None = None
    attrs: dict[str, Any] = field(default_factory=dict)
    _stopped: bool = False
    _removed: bool = False
    _remove_v: bool = False
    _restarted: bool = False
    _reloaded: bool = False
    _logs: bytes = b"line1\nline2\nline3\n"

    def stop(self) -> None:
        self._stopped = True

    def remove(self, *, v: bool = False) -> None:
        self._removed = True
        self._remove_v = v

    def restart(self) -> None:
        self._restarted = True

    def reload(self) -> None:
        self._reloaded = True

    def logs(self, tail: int = 100) -> bytes:
        lines = self._logs.split(b"\n")
        return b"\n".join(lines[-tail:])


@dataclass
class _FakeContainers:
    """Fake docker client.containers namespace."""

    run_calls: list[dict[str, Any]] = field(default_factory=list)
    _containers: dict[str, _FakeContainer] = field(default_factory=dict)
    _list_containers: list[_FakeContainer] = field(default_factory=list)

    def run(self, image: str, **kwargs: Any) -> _FakeContainer:
        call = {"image": image, **kwargs}
        self.run_calls.append(call)
        container = _FakeContainer()
        return container

    def get(self, container_id: str) -> _FakeContainer:
        if container_id in self._containers:
            return self._containers[container_id]
        raise Exception(f"Container {container_id} not found")

    def list(self, **_kwargs: Any) -> list[_FakeContainer]:
        return self._list_containers


@dataclass
class _FakeDockerClient:
    containers: _FakeContainers = field(default_factory=_FakeContainers)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

_GW_ID = uuid4()
_ORG_ID = uuid4()


def _make_manager(
    client: _FakeDockerClient | None = None,
) -> tuple[DockerManager, _FakeDockerClient]:
    c = client or _FakeDockerClient()
    return DockerManager(client=c), c


def _create_with_defaults(
    manager: DockerManager,
    *,
    gateway_id: UUID | None = None,
    name: str = "test-agent",
    org_id: UUID | None = None,
    image: str | None = None,
    provider: str | None = None,
    model: str | None = None,
    memory: str | None = None,
) -> Any:
    return manager.create_container(
        gateway_id=gateway_id or _GW_ID,
        name=name,
        org_id=org_id or _ORG_ID,
        image=image,
        provider=provider,
        model=model,
        memory=memory,
    )


# ---------------------------------------------------------------------------
# A1. Container Creation
# ---------------------------------------------------------------------------


class TestContainerCreation:
    def test_returns_id_port_token(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        monkeypatch.setenv("ZEROCLAW_PORT_RANGE_START", "43000")
        monkeypatch.setenv("ZEROCLAW_PORT_RANGE_END", "43010")
        manager, _ = _make_manager()

        result = _create_with_defaults(manager)

        assert result.container_id == FAKE_CONTAINER_ID
        assert 43000 <= result.host_port < 43010
        assert len(result.token) >= 32

    def test_sets_correct_env_vars(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        manager, client = _make_manager()

        _create_with_defaults(manager, provider="anthropic")

        call = client.containers.run_calls[0]
        env = call["environment"]
        assert env["API_KEY"] == "sk-test-key"
        assert env["PROVIDER"] == "anthropic"
        assert env["ZEROCLAW_GATEWAY_PORT"] == str(ZEROCLAW_INTERNAL_PORT)
        assert env["ZEROCLAW_ALLOW_PUBLIC_BIND"] == "true"

    def test_maps_port_correctly(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        monkeypatch.setenv("ZEROCLAW_PORT_RANGE_START", "43500")
        manager, client = _make_manager()

        result = _create_with_defaults(manager)

        call = client.containers.run_calls[0]
        assert call["ports"] == {f"{ZEROCLAW_INTERNAL_PORT}/tcp": result.host_port}

    def test_sets_labels(self, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        gw_id = uuid4()
        org_id = uuid4()
        manager, client = _make_manager()

        _create_with_defaults(manager, gateway_id=gw_id, org_id=org_id)

        call = client.containers.run_calls[0]
        labels = call["labels"]
        assert labels["managed-by"] == MANAGED_BY_LABEL
        assert labels["gateway-id"] == str(gw_id)
        assert labels["org-id"] == str(org_id)

    def test_sets_resource_limits(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        manager, client = _make_manager()

        _create_with_defaults(manager)

        call = client.containers.run_calls[0]
        assert call["mem_limit"] == "512m"
        assert call["cpu_period"] == 100000
        assert call["cpu_quota"] == 200000

    def test_creates_named_volume(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        gw_id = uuid4()
        manager, client = _make_manager()

        _create_with_defaults(manager, gateway_id=gw_id)

        call = client.containers.run_calls[0]
        volume_name = f"zeroclaw-{gw_id}"
        assert volume_name in call["volumes"]
        assert call["volumes"][volume_name]["bind"] == "/zeroclaw-data"

    def test_uses_default_image(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        manager, client = _make_manager()

        _create_with_defaults(manager)

        call = client.containers.run_calls[0]
        assert call["image"] == DEFAULT_IMAGE

    def test_uses_custom_image(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        manager, client = _make_manager()

        _create_with_defaults(manager, image="my-registry/zeroclaw:custom")

        call = client.containers.run_calls[0]
        assert call["image"] == "my-registry/zeroclaw:custom"

    def test_reads_central_api_key(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-central-key-123")
        manager, client = _make_manager()

        _create_with_defaults(manager)

        call = client.containers.run_calls[0]
        assert call["environment"]["API_KEY"] == "sk-central-key-123"

    def test_fails_without_api_key(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.delenv("ZEROCLAW_LLM_API_KEY", raising=False)
        manager, _ = _make_manager()

        with pytest.raises(DockerManagerError, match="ZEROCLAW_LLM_API_KEY"):
            _create_with_defaults(manager)


# ---------------------------------------------------------------------------
# A1b. Boot Command (onboard --quick + daemon)
# ---------------------------------------------------------------------------


class TestBootCommand:
    def test_command_runs_onboard_then_daemon(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        manager, client = _make_manager()

        _create_with_defaults(manager)

        call = client.containers.run_calls[0]
        cmd = call["command"]
        assert cmd[0] == "sh"
        assert cmd[1] == "-c"
        boot_script = cmd[2]
        assert "zeroclaw onboard --quick" in boot_script
        assert "zeroclaw daemon" in boot_script
        assert "&&" in boot_script

    def test_command_includes_api_key_provider_memory(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        manager, client = _make_manager()

        _create_with_defaults(manager, provider="anthropic", memory="markdown")

        boot_script = client.containers.run_calls[0]["command"][2]
        assert "--api-key $API_KEY" in boot_script
        assert "--provider anthropic" in boot_script
        assert "--memory markdown" in boot_script

    def test_command_includes_model_when_specified(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        manager, client = _make_manager()

        _create_with_defaults(manager, model="anthropic/claude-sonnet-4-20250514")

        boot_script = client.containers.run_calls[0]["command"][2]
        assert "--model anthropic/claude-sonnet-4-20250514" in boot_script

    def test_command_omits_model_when_not_specified(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        manager, client = _make_manager()

        _create_with_defaults(manager)

        boot_script = client.containers.run_calls[0]["command"][2]
        assert "--model" not in boot_script

    def test_command_uses_default_provider_and_memory(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        manager, client = _make_manager()

        _create_with_defaults(manager)

        boot_script = client.containers.run_calls[0]["command"][2]
        assert "--provider openrouter" in boot_script
        assert "--memory sqlite" in boot_script


# ---------------------------------------------------------------------------
# A2. Port Allocation
# ---------------------------------------------------------------------------


class TestPortAllocation:
    def test_picks_lowest_available(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        monkeypatch.setenv("ZEROCLAW_PORT_RANGE_START", "43000")
        monkeypatch.setenv("ZEROCLAW_PORT_RANGE_END", "43005")

        existing = _FakeContainer(
            ports={f"{ZEROCLAW_INTERNAL_PORT}/tcp": [{"HostPort": "43000"}]}
        )
        client = _FakeDockerClient(
            containers=_FakeContainers(_list_containers=[existing])
        )
        manager, _ = _make_manager(client)

        result = _create_with_defaults(manager)

        assert result.host_port == 43001

    def test_respects_range_bounds(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        monkeypatch.setenv("ZEROCLAW_PORT_RANGE_START", "50000")
        monkeypatch.setenv("ZEROCLAW_PORT_RANGE_END", "50010")
        manager, _ = _make_manager()

        result = _create_with_defaults(manager)

        assert 50000 <= result.host_port < 50010

    def test_raises_when_range_exhausted(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        monkeypatch.setenv("ZEROCLAW_PORT_RANGE_START", "43000")
        monkeypatch.setenv("ZEROCLAW_PORT_RANGE_END", "43002")

        existing = [
            _FakeContainer(
                ports={f"{ZEROCLAW_INTERNAL_PORT}/tcp": [{"HostPort": str(p)}]}
            )
            for p in [43000, 43001]
        ]
        client = _FakeDockerClient(
            containers=_FakeContainers(_list_containers=existing)
        )
        manager, _ = _make_manager(client)

        with pytest.raises(DockerManagerError, match="No available ports"):
            _create_with_defaults(manager)


# ---------------------------------------------------------------------------
# A3. Container Lifecycle
# ---------------------------------------------------------------------------


class TestContainerLifecycle:
    def test_stop_container_calls_docker_stop(self) -> None:
        container = _FakeContainer()
        client = _FakeDockerClient(
            containers=_FakeContainers(
                _containers={FAKE_CONTAINER_ID: container}
            )
        )
        manager, _ = _make_manager(client)

        result = manager.stop_container(FAKE_CONTAINER_ID)

        assert result is True
        assert container._stopped is True

    def test_stop_nonexistent_returns_false(self) -> None:
        manager, _ = _make_manager()

        result = manager.stop_container("nonexistent")

        assert result is False

    def test_remove_container(self) -> None:
        container = _FakeContainer()
        client = _FakeDockerClient(
            containers=_FakeContainers(
                _containers={FAKE_CONTAINER_ID: container}
            )
        )
        manager, _ = _make_manager(client)

        result = manager.remove_container(FAKE_CONTAINER_ID)

        assert result is True
        assert container._removed is True

    def test_remove_container_with_volume_flag(self) -> None:
        container = _FakeContainer()
        client = _FakeDockerClient(
            containers=_FakeContainers(
                _containers={FAKE_CONTAINER_ID: container}
            )
        )
        manager, _ = _make_manager(client)

        result = manager.remove_container(FAKE_CONTAINER_ID, remove_volume=True)

        assert result is True
        assert container._remove_v is True

    def test_restart_container(self) -> None:
        container = _FakeContainer()
        client = _FakeDockerClient(
            containers=_FakeContainers(
                _containers={FAKE_CONTAINER_ID: container}
            )
        )
        manager, _ = _make_manager(client)

        result = manager.restart_container(FAKE_CONTAINER_ID)

        assert result is True
        assert container._restarted is True

    def test_get_status_running(self) -> None:
        container = _FakeContainer(status="running")
        client = _FakeDockerClient(
            containers=_FakeContainers(
                _containers={FAKE_CONTAINER_ID: container}
            )
        )
        manager, _ = _make_manager(client)

        status = manager.get_status(FAKE_CONTAINER_ID)

        assert status.status == "running"

    def test_get_status_stopped(self) -> None:
        container = _FakeContainer(status="exited")
        client = _FakeDockerClient(
            containers=_FakeContainers(
                _containers={FAKE_CONTAINER_ID: container}
            )
        )
        manager, _ = _make_manager(client)

        status = manager.get_status(FAKE_CONTAINER_ID)

        assert status.status == "exited"

    def test_get_status_nonexistent(self) -> None:
        manager, _ = _make_manager()

        status = manager.get_status("nonexistent")

        assert status.status == "not_found"

    def test_get_logs_returns_tail(self) -> None:
        container = _FakeContainer(_logs=b"line1\nline2\nline3\nline4\n")
        client = _FakeDockerClient(
            containers=_FakeContainers(
                _containers={FAKE_CONTAINER_ID: container}
            )
        )
        manager, _ = _make_manager(client)

        logs = manager.get_logs(FAKE_CONTAINER_ID, tail=2)

        assert isinstance(logs, str)
        assert len(logs) > 0


# ---------------------------------------------------------------------------
# A4. Docker Unavailable
# ---------------------------------------------------------------------------


class TestDockerUnavailable:
    def test_raises_service_error(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        manager = DockerManager(client=None)

        with pytest.raises(DockerManagerError, match="Docker daemon"):
            _create_with_defaults(manager)
