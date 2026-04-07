# ruff: noqa: INP001
"""Tests for app.services.openclaw.vault_provisioning.

These tests pin the behaviour of the per-agent Obsidian vault seeding
that Phase 3 added. The real VaultClient is patched out; we only care
that the provisioning helper:

1. Respects the `vault_provisioning_enabled()` gate
2. Calls ensure_database() with the deterministic per-agent name
3. Filters to the personality-file subset before writing notes
4. Writes every remaining file via write_note()
5. Returns a populated VaultProvisioningResult with the error field
   set on any failure
"""

from __future__ import annotations

import os
from unittest.mock import MagicMock, patch
from uuid import UUID, uuid4

import pytest


_AGENT_ID = UUID("00000000-0000-0000-0000-00000000abcd")


# ---------------------------------------------------------------------------
# vault_provisioning_enabled
# ---------------------------------------------------------------------------


class TestVaultProvisioningEnabled:
    def test_disabled_without_vault_host(self, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.delenv("ZEROCLAW_VAULT_HOST", raising=False)
        from app.services.openclaw.vault_provisioning import vault_provisioning_enabled

        assert vault_provisioning_enabled() is False

    def test_enabled_when_vault_host_set(self, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.setenv("ZEROCLAW_VAULT_HOST", "couchdb.example.com")
        from app.services.openclaw.vault_provisioning import vault_provisioning_enabled

        assert vault_provisioning_enabled() is True

    def test_disabled_when_vault_host_empty_string(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_VAULT_HOST", "")
        from app.services.openclaw.vault_provisioning import vault_provisioning_enabled

        assert vault_provisioning_enabled() is False


# ---------------------------------------------------------------------------
# _agent_vault_database naming
# ---------------------------------------------------------------------------


class TestAgentVaultDatabase:
    def test_deterministic_from_agent_id(self) -> None:
        from app.services.openclaw.vault_provisioning import _agent_vault_database

        result = _agent_vault_database(_AGENT_ID)
        assert result == f"agent-{_AGENT_ID}"

    def test_different_agents_get_different_databases(self) -> None:
        from app.services.openclaw.vault_provisioning import _agent_vault_database

        a = _agent_vault_database(uuid4())
        b = _agent_vault_database(uuid4())
        assert a != b


# ---------------------------------------------------------------------------
# provision_agent_vault
# ---------------------------------------------------------------------------


def _personality_rendered() -> dict[str, str]:
    """Sample rendered file map matching the DEFAULT_GATEWAY_FILES subset."""
    return {
        "SOUL.md": "# Soul\nBe kind.",
        "IDENTITY.md": "# Identity\nName: Nova",
        "AGENTS.md": "# Agents\nCoordinate via lead.",
        "USER.md": "# User\nharrokrog",
        "MEMORY.md": "# Memory\n(empty)",
        "TOOLS.md": "# Tools\n- shell",
        "HEARTBEAT.md": "# Heartbeat\nevery 10m",
    }


class TestProvisionAgentVault:
    @patch("app.services.openclaw.vault_provisioning._vault_client")
    def test_creates_database_and_writes_all_personality_files(
        self, mock_client_factory: MagicMock
    ) -> None:
        mock_client = MagicMock()
        mock_client.ensure_database.return_value = {
            "ok": True,
            "name": f"agent-{_AGENT_ID}",
            "created": True,
        }
        mock_client.write_note.return_value = {"ok": True, "id": "x", "rev": "1-abc"}
        mock_client.protocol = "http"
        mock_client.host = "couchdb.test"
        mock_client.port = 5984
        mock_client_factory.return_value = mock_client

        from app.services.openclaw.vault_provisioning import provision_agent_vault

        result = provision_agent_vault(
            agent_id=_AGENT_ID,
            rendered_files=_personality_rendered(),
        )

        assert result.error is None
        assert result.database == f"agent-{_AGENT_ID}"
        assert result.created is True
        assert set(result.files_written) == {
            "SOUL.md",
            "IDENTITY.md",
            "AGENTS.md",
            "USER.md",
            "MEMORY.md",
            "TOOLS.md",
            "HEARTBEAT.md",
        }
        mock_client.ensure_database.assert_called_once_with(f"agent-{_AGENT_ID}")
        # One write_note call per personality file
        assert mock_client.write_note.call_count == 7

    @patch("app.services.openclaw.vault_provisioning._vault_client")
    def test_skips_non_personality_files(self, mock_client_factory: MagicMock) -> None:
        mock_client = MagicMock()
        mock_client.ensure_database.return_value = {
            "ok": True,
            "name": f"agent-{_AGENT_ID}",
            "created": True,
        }
        mock_client.write_note.return_value = {"ok": True}
        mock_client.protocol = "http"
        mock_client.host = "couchdb.test"
        mock_client.port = 5984
        mock_client_factory.return_value = mock_client

        from app.services.openclaw.vault_provisioning import provision_agent_vault

        rendered = _personality_rendered()
        rendered["SOME_UNKNOWN_FILE.md"] = "ignored"
        rendered["gateway-config.json"] = "ignored"

        result = provision_agent_vault(
            agent_id=_AGENT_ID,
            rendered_files=rendered,
        )

        assert result.error is None
        written_names = set(result.files_written)
        assert "SOME_UNKNOWN_FILE.md" not in written_names
        assert "gateway-config.json" not in written_names

    @patch("app.services.openclaw.vault_provisioning._vault_client")
    def test_skips_empty_file_contents(self, mock_client_factory: MagicMock) -> None:
        mock_client = MagicMock()
        mock_client.ensure_database.return_value = {
            "ok": True,
            "name": f"agent-{_AGENT_ID}",
            "created": True,
        }
        mock_client.write_note.return_value = {"ok": True}
        mock_client.protocol = "http"
        mock_client.host = "couchdb.test"
        mock_client.port = 5984
        mock_client_factory.return_value = mock_client

        from app.services.openclaw.vault_provisioning import provision_agent_vault

        rendered = {
            "SOUL.md": "# Non-empty",
            "IDENTITY.md": "",  # empty — should be skipped
            "AGENTS.md": "# Non-empty",
        }

        result = provision_agent_vault(
            agent_id=_AGENT_ID,
            rendered_files=rendered,
        )

        assert result.error is None
        assert "IDENTITY.md" not in result.files_written
        assert set(result.files_written) == {"SOUL.md", "AGENTS.md"}

    @patch("app.services.openclaw.vault_provisioning._vault_client")
    def test_returns_error_when_no_personality_files(
        self, mock_client_factory: MagicMock
    ) -> None:
        from app.services.openclaw.vault_provisioning import provision_agent_vault

        result = provision_agent_vault(
            agent_id=_AGENT_ID,
            rendered_files={"unrelated.txt": "content"},
        )

        assert result.error == "no personality files to seed"
        assert result.files_written == ()
        # Vault client factory should not even have been consulted
        mock_client_factory.assert_not_called()

    @patch("app.services.openclaw.vault_provisioning._vault_client")
    def test_ensure_database_failure_is_caught(
        self, mock_client_factory: MagicMock
    ) -> None:
        mock_client = MagicMock()
        mock_client.ensure_database.side_effect = PermissionError(
            "CouchDB authentication failed"
        )
        mock_client.protocol = "http"
        mock_client.host = "couchdb.test"
        mock_client.port = 5984
        mock_client_factory.return_value = mock_client

        from app.services.openclaw.vault_provisioning import provision_agent_vault

        result = provision_agent_vault(
            agent_id=_AGENT_ID,
            rendered_files=_personality_rendered(),
        )

        assert result.error is not None
        assert "ensure_database failed" in result.error
        assert result.files_written == ()
        # write_note should not have been called
        mock_client.write_note.assert_not_called()

    @patch("app.services.openclaw.vault_provisioning._vault_client")
    def test_write_note_failure_stops_and_returns_partial_progress(
        self, mock_client_factory: MagicMock
    ) -> None:
        mock_client = MagicMock()
        mock_client.ensure_database.return_value = {
            "ok": True,
            "name": f"agent-{_AGENT_ID}",
            "created": True,
        }
        # First write succeeds, second fails
        mock_client.write_note.side_effect = [
            {"ok": True, "id": "x", "rev": "1-abc"},
            RuntimeError("CouchDB write timed out"),
        ]
        mock_client.protocol = "http"
        mock_client.host = "couchdb.test"
        mock_client.port = 5984
        mock_client_factory.return_value = mock_client

        from app.services.openclaw.vault_provisioning import provision_agent_vault

        result = provision_agent_vault(
            agent_id=_AGENT_ID,
            rendered_files={
                "AGENTS.md": "first",
                "IDENTITY.md": "second",
            },
        )

        assert result.error is not None
        assert "write_note(IDENTITY.md) failed" in result.error
        # AGENTS.md wrote successfully before the failure
        assert result.files_written == ("AGENTS.md",)

    @patch("app.services.openclaw.vault_provisioning._vault_client")
    def test_existing_database_is_not_an_error(
        self, mock_client_factory: MagicMock
    ) -> None:
        mock_client = MagicMock()
        # CouchDB returns 412 → ensure_database returns created=False
        mock_client.ensure_database.return_value = {
            "ok": True,
            "name": f"agent-{_AGENT_ID}",
            "created": False,
        }
        mock_client.write_note.return_value = {"ok": True, "id": "x", "rev": "1-abc"}
        mock_client.protocol = "http"
        mock_client.host = "couchdb.test"
        mock_client.port = 5984
        mock_client_factory.return_value = mock_client

        from app.services.openclaw.vault_provisioning import provision_agent_vault

        result = provision_agent_vault(
            agent_id=_AGENT_ID,
            rendered_files=_personality_rendered(),
        )

        assert result.error is None
        assert result.created is False
        # Writing still happened
        assert len(result.files_written) == 7


# ---------------------------------------------------------------------------
# VaultClient construction from env
# ---------------------------------------------------------------------------


class TestVaultClientFactory:
    def test_vault_client_reads_env_vars(
        self, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("ZEROCLAW_VAULT_HOST", "obsidian.example.com")
        monkeypatch.setenv("ZEROCLAW_VAULT_PORT", "443")
        monkeypatch.setenv("ZEROCLAW_VAULT_PROTOCOL", "https")
        monkeypatch.setenv("ZEROCLAW_VAULT_USERNAME", "admin")
        monkeypatch.setenv("ZEROCLAW_VAULT_PASSWORD", "supersecret")

        from app.services.openclaw.vault_provisioning import _vault_client

        client = _vault_client()

        assert client.host == "obsidian.example.com"
        assert client.port == 443
        assert client.protocol == "https"
        assert client.username == "admin"
        assert client.password == "supersecret"
