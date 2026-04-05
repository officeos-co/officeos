# ruff: noqa: INP001
"""Tests for ZeroClaw Kubernetes manager."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any
from uuid import uuid4

import pytest

from app.services.zeroclaw.k8s_manager import (
    DEFAULT_IMAGE,
    ZEROCLAW_PORT,
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
    metadata: Any = None


@dataclass
class _FakeCoreV1Api:
    """Captures K8s API calls for assertion."""

    created_pods: list[dict[str, Any]] = field(default_factory=list)
    created_pvcs: list[dict[str, Any]] = field(default_factory=list)
    created_services: list[dict[str, Any]] = field(default_factory=list)
    deleted_pods: list[str] = field(default_factory=list)
    deleted_services: list[str] = field(default_factory=list)
    deleted_pvcs: list[str] = field(default_factory=list)
    _pods: dict[str, _FakePod] = field(default_factory=dict)
    _logs: str = "log line 1\nlog line 2\n"
    _fail_on_read: bool = False

    def create_namespaced_pod(self, *, namespace: str, body: dict) -> None:
        self.created_pods.append({"namespace": namespace, "body": body})

    def create_namespaced_persistent_volume_claim(
        self, *, namespace: str, body: dict
    ) -> None:
        self.created_pvcs.append({"namespace": namespace, "body": body})

    def create_namespaced_service(self, *, namespace: str, body: dict) -> None:
        self.created_services.append({"namespace": namespace, "body": body})

    def delete_namespaced_pod(self, *, name: str, namespace: str) -> None:
        self.deleted_pods.append(name)

    def delete_namespaced_service(self, *, name: str, namespace: str) -> None:
        self.deleted_services.append(name)

    def delete_namespaced_persistent_volume_claim(
        self, *, name: str, namespace: str
    ) -> None:
        self.deleted_pvcs.append(name)

    def read_namespaced_pod(self, *, name: str, namespace: str) -> _FakePod:
        if self._fail_on_read:
            raise Exception("Pod not found")
        if name in self._pods:
            return self._pods[name]
        raise Exception(f"Pod {name} not found")

    def read_namespaced_pod_log(
        self, *, name: str, namespace: str, tail_lines: int = 100
    ) -> str:
        return self._logs


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

_GW_ID = uuid4()
_ORG_ID = uuid4()


def _make_manager(
    api: _FakeCoreV1Api | None = None,
) -> tuple[KubernetesManager, _FakeCoreV1Api]:
    a = api or _FakeCoreV1Api()
    return KubernetesManager(api=a), a


def _create_with_defaults(
    manager: KubernetesManager,
    *,
    gateway_id=None,
    name="test-agent",
    org_id=None,
    image=None,
    provider=None,
    model=None,
    memory=None,
    vault_database=None,
    vault_user_database=None,
):
    return manager.create_container(
        gateway_id=gateway_id or _GW_ID,
        name=name,
        org_id=org_id or _ORG_ID,
        image=image,
        provider=provider,
        model=model,
        memory=memory,
        vault_database=vault_database,
        vault_user_database=vault_user_database,
    )


# ---------------------------------------------------------------------------
# Pod Creation
# ---------------------------------------------------------------------------


class TestPodCreation:
    def test_returns_pod_name_and_token(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager, _ = _make_manager()

        result = _create_with_defaults(manager)

        assert result.container_id.startswith("zeroclaw-")
        assert result.host_port == ZEROCLAW_PORT
        assert len(result.token) >= 32

    def test_creates_pvc_pod_and_service(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager, api = _make_manager()

        _create_with_defaults(manager)

        assert len(api.created_pvcs) == 1
        assert len(api.created_pods) == 1
        assert len(api.created_services) == 1

    def test_pod_has_correct_image(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager, api = _make_manager()

        _create_with_defaults(manager)

        container = api.created_pods[0]["body"]["spec"]["containers"][0]
        assert container["image"] == DEFAULT_IMAGE

    def test_pod_has_custom_image(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager, api = _make_manager()

        _create_with_defaults(manager, image="my-registry/zeroclaw:v2")

        container = api.created_pods[0]["body"]["spec"]["containers"][0]
        assert container["image"] == "my-registry/zeroclaw:v2"

    def test_pod_has_correct_env_vars(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test-key")
        manager, api = _make_manager()

        _create_with_defaults(manager, provider="anthropic")

        container = api.created_pods[0]["body"]["spec"]["containers"][0]
        env = {e["name"]: e["value"] for e in container["env"]}
        assert env["API_KEY"] == "sk-test-key"
        assert env["PROVIDER"] == "anthropic"
        assert env["ZEROCLAW_GATEWAY_PORT"] == str(ZEROCLAW_PORT)
        assert env["ZEROCLAW_ALLOW_PUBLIC_BIND"] == "true"

    def test_pod_has_resource_limits(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager, api = _make_manager()

        _create_with_defaults(manager)

        container = api.created_pods[0]["body"]["spec"]["containers"][0]
        assert container["resources"]["limits"]["memory"] == "512Mi"
        assert container["resources"]["limits"]["cpu"] == "2"

    def test_pod_mounts_pvc(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager, api = _make_manager()

        _create_with_defaults(manager)

        pod_spec = api.created_pods[0]["body"]["spec"]
        mount = pod_spec["containers"][0]["volumeMounts"][0]
        assert mount["mountPath"] == "/zeroclaw-data"
        vol = pod_spec["volumes"][0]
        assert "persistentVolumeClaim" in vol

    def test_service_exposes_correct_port(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager, api = _make_manager()

        _create_with_defaults(manager)

        svc = api.created_services[0]["body"]
        assert svc["spec"]["ports"][0]["port"] == ZEROCLAW_PORT

    def test_labels_include_gateway_and_org_id(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        gw_id = uuid4()
        org_id = uuid4()
        manager, api = _make_manager()

        _create_with_defaults(manager, gateway_id=gw_id, org_id=org_id)

        labels = api.created_pods[0]["body"]["metadata"]["labels"]
        assert labels["gateway-id"] == str(gw_id)
        assert labels["org-id"] == str(org_id)
        assert labels["app"] == "zeroclaw"
        assert labels["managed-by"] == "openclaw-mission-control"

    def test_fails_without_api_key(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.delenv("ZEROCLAW_LLM_API_KEY", raising=False)
        manager, _ = _make_manager()

        with pytest.raises(K8sManagerError, match="ZEROCLAW_LLM_API_KEY"):
            _create_with_defaults(manager)


# ---------------------------------------------------------------------------
# Boot Command
# ---------------------------------------------------------------------------


class TestBootCommand:
    def test_command_runs_onboard_then_daemon(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager, api = _make_manager()

        _create_with_defaults(manager)

        container = api.created_pods[0]["body"]["spec"]["containers"][0]
        cmd = container["command"]
        assert cmd[0] == "sh"
        assert cmd[1] == "-c"
        assert "zeroclaw onboard --quick" in cmd[2]
        assert "zeroclaw daemon" in cmd[2]
        assert "&&" in cmd[2]

    def test_command_includes_provider_and_memory(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager, api = _make_manager()

        _create_with_defaults(manager, provider="openai", memory="markdown")

        boot = api.created_pods[0]["body"]["spec"]["containers"][0]["command"][2]
        assert "--provider openai" in boot
        assert "--memory markdown" in boot

    def test_command_includes_model_when_specified(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager, api = _make_manager()

        _create_with_defaults(manager, model="gpt-4o")

        boot = api.created_pods[0]["body"]["spec"]["containers"][0]["command"][2]
        assert "--model gpt-4o" in boot

    def test_command_omits_model_when_not_specified(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager, api = _make_manager()

        _create_with_defaults(manager)

        boot = api.created_pods[0]["body"]["spec"]["containers"][0]["command"][2]
        assert "--model" not in boot


# ---------------------------------------------------------------------------
# Pod Lifecycle
# ---------------------------------------------------------------------------


class TestPodLifecycle:
    def test_stop_deletes_pod(self) -> None:
        manager, api = _make_manager()

        result = manager.stop_container("zeroclaw-abc")

        assert result is True
        assert "zeroclaw-abc" in api.deleted_pods

    def test_stop_nonexistent_returns_false(self) -> None:
        api = _FakeCoreV1Api()

        def _fail(*, name, namespace):
            raise Exception("not found")

        api.delete_namespaced_pod = _fail
        manager = KubernetesManager(api=api)

        assert manager.stop_container("nonexistent") is False

    def test_remove_deletes_pod_and_service(self) -> None:
        manager, api = _make_manager()

        result = manager.remove_container("zeroclaw-abc")

        assert result is True
        assert "zeroclaw-abc" in api.deleted_pods
        assert "zeroclaw-abc" in api.deleted_services

    def test_remove_with_volume_deletes_pvc(self) -> None:
        manager, api = _make_manager()

        manager.remove_container("zeroclaw-abc", remove_volume=True)

        assert "zeroclaw-data-abc" in api.deleted_pvcs

    def test_remove_without_volume_keeps_pvc(self) -> None:
        manager, api = _make_manager()

        manager.remove_container("zeroclaw-abc", remove_volume=False)

        assert len(api.deleted_pvcs) == 0

    def test_restart_deletes_pod(self) -> None:
        manager, api = _make_manager()

        result = manager.restart_container("zeroclaw-abc")

        assert result is True
        assert "zeroclaw-abc" in api.deleted_pods

    def test_get_status_running(self) -> None:
        api = _FakeCoreV1Api(
            _pods={"zeroclaw-abc": _FakePod(status=_FakePodStatus(phase="Running"))}
        )
        manager = KubernetesManager(api=api)

        status = manager.get_status("zeroclaw-abc")

        assert status.status == "running"

    def test_get_status_pending(self) -> None:
        api = _FakeCoreV1Api(
            _pods={"zeroclaw-abc": _FakePod(status=_FakePodStatus(phase="Pending"))}
        )
        manager = KubernetesManager(api=api)

        status = manager.get_status("zeroclaw-abc")

        assert status.status == "pending"

    def test_get_status_failed(self) -> None:
        api = _FakeCoreV1Api(
            _pods={"zeroclaw-abc": _FakePod(status=_FakePodStatus(phase="Failed"))}
        )
        manager = KubernetesManager(api=api)

        status = manager.get_status("zeroclaw-abc")

        assert status.status == "failed"

    def test_get_status_not_found(self) -> None:
        manager, _ = _make_manager()

        status = manager.get_status("nonexistent")

        assert status.status == "not_found"

    def test_get_logs(self) -> None:
        api = _FakeCoreV1Api(_logs="line1\nline2\nline3")
        manager = KubernetesManager(api=api)

        logs = manager.get_logs("zeroclaw-abc", tail=50)

        assert "line1" in logs


# ---------------------------------------------------------------------------
# K8s Unavailable
# ---------------------------------------------------------------------------


class TestK8sUnavailable:
    def test_raises_on_create(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        manager = KubernetesManager(api=None)

        with pytest.raises(K8sManagerError, match="Kubernetes API"):
            _create_with_defaults(manager)

    def test_stop_returns_false(self) -> None:
        manager = KubernetesManager(api=None)
        assert manager.stop_container("abc") is False


# ---------------------------------------------------------------------------
# Service URL
# ---------------------------------------------------------------------------


# ---------------------------------------------------------------------------
# Vault Integration
# ---------------------------------------------------------------------------


class TestVaultIntegration:
    def test_vault_env_vars_injected_when_configured(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        monkeypatch.setenv("ZEROCLAW_VAULT_HOST", "couchdb.example.com")
        monkeypatch.setenv("ZEROCLAW_VAULT_PORT", "443")
        monkeypatch.setenv("ZEROCLAW_VAULT_PROTOCOL", "https")
        monkeypatch.setenv("ZEROCLAW_VAULT_USERNAME", "admin")
        monkeypatch.setenv("ZEROCLAW_VAULT_PASSWORD", "secret")
        manager, api = _make_manager()

        _create_with_defaults(manager, vault_database="org-vault")

        container = api.created_pods[0]["body"]["spec"]["containers"][0]
        env = {e["name"]: e["value"] for e in container["env"]}
        assert env["VAULT_HOST"] == "couchdb.example.com"
        assert env["VAULT_PORT"] == "443"
        assert env["VAULT_PROTOCOL"] == "https"
        assert env["VAULT_DATABASE"] == "org-vault"
        assert env["VAULT_USERNAME"] == "admin"
        assert env["VAULT_PASSWORD"] == "secret"

    def test_vault_user_database_injected_when_provided(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        monkeypatch.setenv("ZEROCLAW_VAULT_HOST", "couchdb.example.com")
        manager, api = _make_manager()

        _create_with_defaults(
            manager, vault_database="org-vault", vault_user_database="user-vault"
        )

        container = api.created_pods[0]["body"]["spec"]["containers"][0]
        env = {e["name"]: e["value"] for e in container["env"]}
        assert env["VAULT_USER_DATABASE"] == "user-vault"

    def test_no_vault_env_vars_when_host_not_configured(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        monkeypatch.delenv("ZEROCLAW_VAULT_HOST", raising=False)
        manager, api = _make_manager()

        _create_with_defaults(manager)

        container = api.created_pods[0]["body"]["spec"]["containers"][0]
        env_names = {e["name"] for e in container["env"]}
        assert "VAULT_HOST" not in env_names
        assert "VAULT_DATABASE" not in env_names

    def test_boot_command_includes_vault_setup_when_configured(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        monkeypatch.setenv("ZEROCLAW_VAULT_HOST", "couchdb.example.com")
        manager, api = _make_manager()

        _create_with_defaults(manager)

        boot = api.created_pods[0]["body"]["spec"]["containers"][0]["command"][2]
        assert "pip install" in boot
        assert "obsidian-vault-cli" in boot
        assert "vault config set" in boot

    def test_boot_command_skips_vault_when_not_configured(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_LLM_API_KEY", "sk-test")
        monkeypatch.delenv("ZEROCLAW_VAULT_HOST", raising=False)
        manager, api = _make_manager()

        _create_with_defaults(manager)

        boot = api.created_pods[0]["body"]["spec"]["containers"][0]["command"][2]
        assert "obsidian-vault-cli" not in boot
        assert "vault config set" not in boot


# ---------------------------------------------------------------------------
# Service URL
# ---------------------------------------------------------------------------


class TestServiceUrl:
    def test_generates_cluster_local_url(self) -> None:
        manager = KubernetesManager(api=_FakeCoreV1Api(), namespace="default")
        gw_id = uuid4()

        url = manager._service_url(gw_id)

        short = str(gw_id)[:8]
        assert url == f"ws://zeroclaw-{short}.default.svc.cluster.local:{ZEROCLAW_PORT}/ws/chat"

    def test_custom_namespace(self) -> None:
        manager = KubernetesManager(api=_FakeCoreV1Api(), namespace="zeroclaw-agents")
        gw_id = uuid4()

        url = manager._service_url(gw_id)

        assert "zeroclaw-agents.svc.cluster.local" in url
