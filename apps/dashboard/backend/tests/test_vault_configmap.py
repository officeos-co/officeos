# ruff: noqa: INP001
"""Tests for app.services.zeroclaw.vault_configmap.

The K8s side of Phase 3 vault provisioning. These tests mock the
kubernetes client entirely; we only care that the helper builds a
well-formed ConfigMap body, picks create vs replace correctly, and
returns the right result structure on success and failure.
"""

from __future__ import annotations

from unittest.mock import MagicMock
from uuid import UUID

from app.services.zeroclaw.vault_configmap import (
    agent_vault_configmap_name,
    apply_agent_vault_configmap,
    delete_agent_vault_configmap,
)


_AGENT_ID = UUID("00000000-0000-0000-0000-00000000abcd")
_NAMESPACE = "default"


def _make_api(*, read_raises: Exception | None = None) -> MagicMock:
    api = MagicMock()
    if read_raises is not None:
        api.read_namespaced_config_map.side_effect = read_raises
    return api


def _sample_rendered() -> dict[str, str]:
    return {
        "SOUL.md": "# Soul\nBe kind.",
        "IDENTITY.md": "# Identity\nName: Nova",
        "AGENTS.md": "# Agents\n",
        "EMPTY.md": "",  # will be dropped
    }


# ---------------------------------------------------------------------------
# Naming
# ---------------------------------------------------------------------------


class TestName:
    def test_name_is_deterministic(self) -> None:
        assert (
            agent_vault_configmap_name(_AGENT_ID)
            == f"eaos-agent-{_AGENT_ID}-vault"
        )


# ---------------------------------------------------------------------------
# apply_agent_vault_configmap
# ---------------------------------------------------------------------------


class _NotFoundError(Exception):
    def __init__(self) -> None:
        self.status = 404


class TestApplyCreatePath:
    def test_create_when_not_existing(self) -> None:
        api = _make_api(read_raises=_NotFoundError())
        api.create_namespaced_config_map.return_value = None

        result = apply_agent_vault_configmap(
            api=api,
            namespace=_NAMESPACE,
            agent_id=_AGENT_ID,
            rendered_files=_sample_rendered(),
        )

        assert result.error is None
        assert result.action == "created"
        assert result.name == f"eaos-agent-{_AGENT_ID}-vault"
        assert set(result.keys) == {"SOUL.md", "IDENTITY.md", "AGENTS.md"}

        api.create_namespaced_config_map.assert_called_once()
        body = api.create_namespaced_config_map.call_args.kwargs["body"]
        assert body["kind"] == "ConfigMap"
        assert body["metadata"]["name"] == result.name
        assert body["metadata"]["namespace"] == _NAMESPACE
        # Labels include the Phase 3 markers
        labels = body["metadata"]["labels"]
        assert labels["app"] == "zeroclaw"
        assert labels["agent-id"] == str(_AGENT_ID)
        assert labels["eaos-role"] == "agent-vault"
        # Empty files are dropped
        assert "EMPTY.md" not in body["data"]
        assert body["data"]["SOUL.md"].startswith("# Soul")

    def test_create_not_called_when_rendered_is_empty(self) -> None:
        api = _make_api()

        result = apply_agent_vault_configmap(
            api=api,
            namespace=_NAMESPACE,
            agent_id=_AGENT_ID,
            rendered_files={},
        )

        assert result.error is not None
        assert "no rendered files" in result.error
        api.read_namespaced_config_map.assert_not_called()
        api.create_namespaced_config_map.assert_not_called()
        api.replace_namespaced_config_map.assert_not_called()

    def test_all_empty_values_dropped_and_reported(self) -> None:
        api = _make_api()

        result = apply_agent_vault_configmap(
            api=api,
            namespace=_NAMESPACE,
            agent_id=_AGENT_ID,
            rendered_files={"SOUL.md": "", "IDENTITY.md": ""},
        )

        assert result.error is not None
        assert "no rendered files" in result.error

    def test_extra_labels_merge(self) -> None:
        api = _make_api(read_raises=_NotFoundError())
        api.create_namespaced_config_map.return_value = None

        apply_agent_vault_configmap(
            api=api,
            namespace=_NAMESPACE,
            agent_id=_AGENT_ID,
            rendered_files=_sample_rendered(),
            labels={"gateway-id": "gw-42", "org-id": "org-1"},
        )

        body = api.create_namespaced_config_map.call_args.kwargs["body"]
        labels = body["metadata"]["labels"]
        assert labels["gateway-id"] == "gw-42"
        assert labels["org-id"] == "org-1"
        # Defaults still present
        assert labels["app"] == "zeroclaw"


class TestApplyReplacePath:
    def test_replace_when_existing(self) -> None:
        api = _make_api()
        # read succeeds → ConfigMap exists → replace path
        api.read_namespaced_config_map.return_value = MagicMock()

        result = apply_agent_vault_configmap(
            api=api,
            namespace=_NAMESPACE,
            agent_id=_AGENT_ID,
            rendered_files=_sample_rendered(),
        )

        assert result.error is None
        assert result.action == "replaced"
        api.replace_namespaced_config_map.assert_called_once()
        api.create_namespaced_config_map.assert_not_called()


class TestApplyFailurePaths:
    def test_read_non_404_aborts(self) -> None:
        class ServerError(Exception):
            def __init__(self) -> None:
                self.status = 503

        api = _make_api(read_raises=ServerError())

        result = apply_agent_vault_configmap(
            api=api,
            namespace=_NAMESPACE,
            agent_id=_AGENT_ID,
            rendered_files=_sample_rendered(),
        )

        assert result.error is not None
        assert "read failed" in result.error
        api.create_namespaced_config_map.assert_not_called()
        api.replace_namespaced_config_map.assert_not_called()

    def test_create_exception_reported(self) -> None:
        api = _make_api(read_raises=_NotFoundError())
        api.create_namespaced_config_map.side_effect = RuntimeError(
            "quota exceeded"
        )

        result = apply_agent_vault_configmap(
            api=api,
            namespace=_NAMESPACE,
            agent_id=_AGENT_ID,
            rendered_files=_sample_rendered(),
        )

        assert result.error is not None
        assert "create failed" in result.error
        assert result.action is None

    def test_replace_exception_reported(self) -> None:
        api = _make_api()
        api.read_namespaced_config_map.return_value = MagicMock()
        api.replace_namespaced_config_map.side_effect = RuntimeError("conflict")

        result = apply_agent_vault_configmap(
            api=api,
            namespace=_NAMESPACE,
            agent_id=_AGENT_ID,
            rendered_files=_sample_rendered(),
        )

        assert result.error is not None
        assert "replace failed" in result.error
        assert result.action is None


# ---------------------------------------------------------------------------
# delete_agent_vault_configmap
# ---------------------------------------------------------------------------


class TestDelete:
    def test_delete_returns_true_on_success(self) -> None:
        api = _make_api()
        api.delete_namespaced_config_map.return_value = None

        assert (
            delete_agent_vault_configmap(
                api=api, namespace=_NAMESPACE, agent_id=_AGENT_ID
            )
            is True
        )

        api.delete_namespaced_config_map.assert_called_once_with(
            name=f"eaos-agent-{_AGENT_ID}-vault", namespace=_NAMESPACE
        )

    def test_delete_returns_true_on_404(self) -> None:
        api = _make_api()
        api.delete_namespaced_config_map.side_effect = _NotFoundError()

        assert (
            delete_agent_vault_configmap(
                api=api, namespace=_NAMESPACE, agent_id=_AGENT_ID
            )
            is True
        )

    def test_delete_returns_false_on_other_error(self) -> None:
        class ServerError(Exception):
            def __init__(self) -> None:
                self.status = 500

        api = _make_api()
        api.delete_namespaced_config_map.side_effect = ServerError()

        assert (
            delete_agent_vault_configmap(
                api=api, namespace=_NAMESPACE, agent_id=_AGENT_ID
            )
            is False
        )
