# ruff: noqa: INP001
"""Integration-level tests for AgentLifecycleService vault + ConfigMap seeding.

These tests verify that the `_seed_agent_vault` hook composes the
vault-provisioning module and the K8s ConfigMap module correctly. The
database layer is NOT exercised — we focus on the non-DB wiring that
Phase 3 commit 11 added.
"""

from __future__ import annotations

import os
from unittest.mock import AsyncMock, MagicMock, patch
from uuid import UUID

import pytest

from app.services.openclaw.provisioning_db import AgentLifecycleService


class TestK8sHelpers:
    """Tiny static helpers on AgentLifecycleService — Phase 3 additions."""

    def test_k8s_namespace_defaults_to_default(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.delenv("POD_NAMESPACE", raising=False)
        assert AgentLifecycleService._k8s_namespace() == "default"

    def test_k8s_namespace_reads_pod_namespace_env(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("POD_NAMESPACE", "eaos-prod")
        assert AgentLifecycleService._k8s_namespace() == "eaos-prod"

    def test_k8s_api_or_none_returns_none_when_not_in_cluster(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        # We're not running inside a K8s pod, so load_incluster_config
        # should raise and the helper should return None.
        # (This test depends on the fact that pytest is not running
        # inside a Kubernetes pod, which is normally the case for local
        # dev and CI runners.)
        result = AgentLifecycleService._k8s_api_or_none()
        assert result is None


class TestSeedAgentVaultGating:
    """`_seed_agent_vault` must be a no-op without a vault configured."""

    @pytest.mark.asyncio
    async def test_no_op_when_vault_provisioning_disabled(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        # Ensure vault host is NOT set
        monkeypatch.delenv("ZEROCLAW_VAULT_HOST", raising=False)

        svc = AgentLifecycleService.__new__(AgentLifecycleService)  # bypass __init__
        svc.session = MagicMock()
        svc.logger = MagicMock()

        with patch(
            "app.services.openclaw.provisioning_db._render_provisioning_agent_files"
        ) as render_mock:
            await svc._seed_agent_vault(
                agent=MagicMock(id=UUID("00000000-0000-0000-0000-00000000abcd")),
                board=MagicMock(),
                gateway=MagicMock(),
                auth_token="t",
                user=None,
            )

        # If the gate bailed correctly, we never reached the render step
        render_mock.assert_not_called()

    @pytest.mark.asyncio
    async def test_full_happy_path_vault_and_configmap(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_VAULT_HOST", "couchdb.test")

        svc = AgentLifecycleService.__new__(AgentLifecycleService)
        svc.session = MagicMock()
        svc.session.add = MagicMock()
        svc.session.commit = AsyncMock()
        svc.session.refresh = AsyncMock()
        svc.logger = MagicMock()

        agent_id = UUID("00000000-0000-0000-0000-00000000abcd")
        agent = MagicMock(id=agent_id, vault_database=None)

        gateway = MagicMock()
        gateway.id = UUID("00000000-0000-0000-0000-00000000ffff")
        gateway.organization_id = UUID("00000000-0000-0000-0000-00000000aaaa")

        rendered = {
            "SOUL.md": "# Soul",
            "IDENTITY.md": "# Identity",
            "AGENTS.md": "# Agents",
        }

        mock_api = MagicMock()

        with (
            patch(
                "app.services.openclaw.provisioning_db._build_provisioning_context",
                return_value={"agent_name": "x"},
            ),
            patch(
                "app.services.openclaw.provisioning_db._render_provisioning_agent_files",
                return_value=rendered,
            ),
            patch(
                "app.services.openclaw.provisioning_db.provision_agent_vault"
            ) as mock_vault,
            patch(
                "app.services.openclaw.provisioning_db.apply_agent_vault_configmap"
            ) as mock_cm,
            patch.object(
                AgentLifecycleService,
                "_k8s_api_or_none",
                staticmethod(lambda: mock_api),
            ),
        ):
            from app.services.openclaw.vault_provisioning import (
                VaultProvisioningResult,
            )
            from app.services.zeroclaw.vault_configmap import (
                VaultConfigMapResult,
            )

            mock_vault.return_value = VaultProvisioningResult(
                database=f"agent-{agent_id}",
                created=True,
                files_written=tuple(sorted(rendered)),
                error=None,
            )
            mock_cm.return_value = VaultConfigMapResult(
                name=f"eaos-agent-{agent_id}-vault",
                keys=tuple(sorted(rendered)),
                action="created",
                error=None,
            )

            await svc._seed_agent_vault(
                agent=agent,
                board=MagicMock(),
                gateway=gateway,
                auth_token="token-abc",
                user=None,
            )

        # Vault side
        mock_vault.assert_called_once()
        assert mock_vault.call_args.kwargs["agent_id"] == agent_id
        assert mock_vault.call_args.kwargs["rendered_files"] == rendered

        # ConfigMap side: should have been invoked with the SAME rendered dict
        mock_cm.assert_called_once()
        cm_kwargs = mock_cm.call_args.kwargs
        assert cm_kwargs["agent_id"] == agent_id
        assert cm_kwargs["rendered_files"] == rendered
        assert cm_kwargs["api"] is mock_api

        # Agent row was updated with the vault_database and committed
        assert agent.vault_database == f"agent-{agent_id}"
        svc.session.add.assert_called_once_with(agent)
        svc.session.commit.assert_awaited()
        svc.session.refresh.assert_awaited()

    @pytest.mark.asyncio
    async def test_vault_error_skips_configmap_step(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_VAULT_HOST", "couchdb.test")

        svc = AgentLifecycleService.__new__(AgentLifecycleService)
        svc.session = MagicMock()
        svc.session.add = MagicMock()
        svc.session.commit = AsyncMock()
        svc.session.refresh = AsyncMock()
        svc.logger = MagicMock()

        agent = MagicMock(id=UUID("00000000-0000-0000-0000-00000000abcd"))

        with (
            patch(
                "app.services.openclaw.provisioning_db._build_provisioning_context",
                return_value={},
            ),
            patch(
                "app.services.openclaw.provisioning_db._render_provisioning_agent_files",
                return_value={"SOUL.md": "# x"},
            ),
            patch(
                "app.services.openclaw.provisioning_db.provision_agent_vault"
            ) as mock_vault,
            patch(
                "app.services.openclaw.provisioning_db.apply_agent_vault_configmap"
            ) as mock_cm,
        ):
            from app.services.openclaw.vault_provisioning import (
                VaultProvisioningResult,
            )

            mock_vault.return_value = VaultProvisioningResult(
                database="agent-x",
                created=False,
                files_written=(),
                error="couchdb unavailable",
            )

            await svc._seed_agent_vault(
                agent=agent,
                board=MagicMock(),
                gateway=MagicMock(),
                auth_token="t",
                user=None,
            )

        # Vault was attempted, failed, so ConfigMap was never touched
        mock_vault.assert_called_once()
        mock_cm.assert_not_called()
        # The agent row was NOT updated
        svc.session.add.assert_not_called()

    @pytest.mark.asyncio
    async def test_skips_configmap_when_no_in_cluster_api(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        """Happy path in vault, but no K8s client → skip ConfigMap."""
        monkeypatch.setenv("ZEROCLAW_VAULT_HOST", "couchdb.test")

        svc = AgentLifecycleService.__new__(AgentLifecycleService)
        svc.session = MagicMock()
        svc.session.add = MagicMock()
        svc.session.commit = AsyncMock()
        svc.session.refresh = AsyncMock()
        svc.logger = MagicMock()

        agent_id = UUID("00000000-0000-0000-0000-00000000abcd")
        agent = MagicMock(id=agent_id, vault_database=None)

        with (
            patch(
                "app.services.openclaw.provisioning_db._build_provisioning_context",
                return_value={},
            ),
            patch(
                "app.services.openclaw.provisioning_db._render_provisioning_agent_files",
                return_value={"SOUL.md": "# x"},
            ),
            patch(
                "app.services.openclaw.provisioning_db.provision_agent_vault"
            ) as mock_vault,
            patch(
                "app.services.openclaw.provisioning_db.apply_agent_vault_configmap"
            ) as mock_cm,
            patch.object(
                AgentLifecycleService,
                "_k8s_api_or_none",
                staticmethod(lambda: None),
            ),
        ):
            from app.services.openclaw.vault_provisioning import (
                VaultProvisioningResult,
            )

            mock_vault.return_value = VaultProvisioningResult(
                database=f"agent-{agent_id}",
                created=True,
                files_written=("SOUL.md",),
                error=None,
            )

            await svc._seed_agent_vault(
                agent=agent,
                board=MagicMock(),
                gateway=MagicMock(),
                auth_token="t",
                user=None,
            )

        mock_vault.assert_called_once()
        mock_cm.assert_not_called()
        # Agent row DID get its vault_database set — vault side succeeded
        assert agent.vault_database == f"agent-{agent_id}"
