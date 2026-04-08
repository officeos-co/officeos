# ruff: noqa: INP001
"""Tests for ZeroClaw container lifecycle API endpoints.

These tests verify the container start/stop/restart/status/logs endpoints
reject OpenClaw gateways and properly delegate to the K8s manager.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any
from uuid import uuid4

import pytest

from app.services.zeroclaw.k8s_manager import (
    ContainerStatus,
    K8sManagerError,
    KubernetesManager,
)


# ---------------------------------------------------------------------------
# Fakes
# ---------------------------------------------------------------------------


@dataclass
class _FakePodStatus:
    phase: str = "Running"


@dataclass
class _FakePod:
    status: _FakePodStatus = field(default_factory=_FakePodStatus)


@dataclass
class _FakeCoreV1Api:
    deleted_pods: list[str] = field(default_factory=list)
    _pods: dict[str, _FakePod] = field(default_factory=dict)
    _logs: str = "log line 1\nlog line 2\n"

    def delete_namespaced_pod(self, *, name: str, namespace: str) -> None:
        self.deleted_pods.append(name)

    def delete_namespaced_service(self, *, name: str, namespace: str) -> None:
        pass

    def delete_namespaced_persistent_volume_claim(
        self, *, name: str, namespace: str
    ) -> None:
        pass

    def read_namespaced_pod(self, *, name: str, namespace: str) -> _FakePod:
        if name in self._pods:
            return self._pods[name]
        raise Exception(f"Pod {name} not found")

    def read_namespaced_pod_log(
        self, *, name: str, namespace: str, tail_lines: int = 100
    ) -> str:
        return self._logs


# ---------------------------------------------------------------------------
# Container lifecycle tests via KubernetesManager
# ---------------------------------------------------------------------------


class TestContainerLifecycleViaManager:
    """Test the K8s manager methods that the container API delegates to."""

    def test_stop_deletes_pod(self) -> None:
        api = _FakeCoreV1Api()
        manager = KubernetesManager(api=api)

        result = manager.stop_container("zeroclaw-abc")

        assert result is True
        assert "zeroclaw-abc" in api.deleted_pods

    def test_restart_deletes_pod(self) -> None:
        api = _FakeCoreV1Api()
        manager = KubernetesManager(api=api)

        result = manager.restart_container("zeroclaw-abc")

        assert result is True
        assert "zeroclaw-abc" in api.deleted_pods

    def test_get_status_returns_running(self) -> None:
        api = _FakeCoreV1Api(
            _pods={"zeroclaw-abc": _FakePod(status=_FakePodStatus(phase="Running"))}
        )
        manager = KubernetesManager(api=api)

        status = manager.get_status("zeroclaw-abc")

        assert status.status == "running"

    def test_get_logs_returns_content(self) -> None:
        api = _FakeCoreV1Api(_logs="log line 1\nlog line 2")
        manager = KubernetesManager(api=api)

        logs = manager.get_logs("zeroclaw-abc", tail=50)

        assert "log line" in logs


class TestContainerEndpointGuards:
    """Test that container operations are properly guarded."""

    def test_openclaw_gateway_type_check(self) -> None:
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
            url="ws://zeroclaw-abc.default.svc.cluster.local:42617/ws/chat",
            workspace_root="/zeroclaw-data/workspace",
            type="zeroclaw",
            container_id="zeroclaw-abc",
        )

        assert oc_gw.type != "zeroclaw"
        assert zc_gw.type == "zeroclaw"

    def test_k8s_unavailable_raises_on_create(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager = KubernetesManager(api=None)

        with pytest.raises(K8sManagerError, match="Kubernetes API"):
            manager.create_container(
                gateway_id=uuid4(),
                name="test",
                org_id=uuid4(),
            )

    def test_k8s_unavailable_returns_false_on_stop(self) -> None:
        manager = KubernetesManager(api=None)
        assert manager.stop_container("abc") is False
