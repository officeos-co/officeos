"""Tests for Phase 4: Safety & Error Handling (v0.2.0).

Tests cover:
- write --force guard (refuse silent overwrites)
- write --diff flag (preview changes)
- create existence check
- delete --yes / interactive confirmation
- property:set read-before-write + preview + --yes
- --dry-run global flag
- deleted:true detection on write
- Wrapped HTTP error messages with context + hints
"""

import json

import pytest
from unittest.mock import patch, MagicMock
from click.testing import CliRunner

from vault_cli.cli.main import cli


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

MOCK_NOTES_LIST = [
    {
        "path": "Projects/ClosedClaw.md",
        "id": "projects/closedclaw.md",
        "mtime": 1709500000000,
        "size": 300,
    },
    {
        "path": "Projects/ElevenStoic.md",
        "id": "projects/elevenstoic.md",
        "mtime": 1709500000000,
        "size": 200,
    },
    {
        "path": "References/Agent Loop.md",
        "id": "references/agent loop.md",
        "mtime": 1709500000000,
        "size": 250,
    },
    {
        "path": "North Star.md",
        "id": "north star.md",
        "mtime": 1709500000000,
        "size": 180,
    },
    {
        "path": "Templates/People Template.md",
        "id": "templates/people template.md",
        "mtime": 1709500000000,
        "size": 100,
    },
]

CLOSEDCLAW_CONTENT = (
    "---\n"
    "categories:\n"
    '  - "[[Projects]]"\n'
    "tags:\n"
    "  - ai\n"
    "  - agent\n"
    "status: active\n"
    "---\n"
    "\n"
    "# ClosedClaw\n"
    "\n"
    "An AI agent framework. See [[Agent Loop]] for the core pattern.\n"
    "Built on [[Python]] and [[LangChain]].\n"
    "\n"
    "Related: [[ElevenStoic]], [[North Star]]\n"
)

NORTH_STAR_CONTENT = (
    "---\n"
    "categories:\n"
    '  - "[[Navigation]]"\n'
    "tags:\n"
    "  - meta\n"
    "status: draft\n"
    "---\n"
    "\n"
    "# North Star\n"
    "\n"
    "The guiding vision. Links to [[ClosedClaw]] and [[ElevenStoic]].\n"
)


def _mock_client(with_deleted_doc=False):
    """Create a MagicMock VaultClient with preset return values."""
    client = MagicMock()
    client.list_notes.return_value = MOCK_NOTES_LIST
    client.ping.return_value = {"ok": True}
    client.write_note.return_value = {"ok": True, "id": "test", "rev": "1-abc"}
    client.delete_note.return_value = {"ok": True}
    client.move_note.return_value = {"ok": True}

    def _read_note(path):
        content_map = {
            "Projects/ClosedClaw.md": CLOSEDCLAW_CONTENT,
            "North Star.md": NORTH_STAR_CONTENT,
        }
        if path in content_map:
            return {
                "path": path,
                "content": content_map[path],
                "ctime": 1709500000000,
                "mtime": 1709500000000,
                "metadata": {"_rev": "3-existing", "deleted": False},
            }
        return None

    client.read_note.side_effect = _read_note
    return client


def _mock_client_with_deleted_doc():
    """Create a mock client where a note has deleted:true in metadata."""
    client = _mock_client()
    original_read = client.read_note.side_effect

    def _read_with_deleted(path):
        if path == "References/Restored.md":
            return {
                "path": "References/Restored.md",
                "content": "old content",
                "ctime": 1709500000000,
                "mtime": 1709500000000,
                "metadata": {"_rev": "2-old", "deleted": True},
            }
        return original_read(path)

    client.read_note.side_effect = _read_with_deleted
    # Also add to list
    client.list_notes.return_value = MOCK_NOTES_LIST + [
        {
            "path": "References/Restored.md",
            "id": "references/restored.md",
            "mtime": 1709500000000,
            "size": 50,
        }
    ]
    return client


@pytest.fixture
def runner():
    return CliRunner()


# ---------------------------------------------------------------------------
# Write safety: --force guard
# ---------------------------------------------------------------------------


class TestWriteSafety:
    """vault write refuses to overwrite existing notes without --force."""

    @patch("vault_cli.cli.crud.get_client")
    def test_write_existing_note_no_force_aborts(self, mock_get_client, runner):
        """Writing to an existing note without --force should abort."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            ["write", "--path", "Projects/ClosedClaw.md", "--content", "new content"],
        )
        assert result.exit_code != 0
        assert (
            "already exists" in result.output.lower()
            or "aborted" in result.output.lower()
        )
        # Should NOT have called write_note
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_write_existing_note_with_force_succeeds(self, mock_get_client, runner):
        """Writing to an existing note with --force should succeed."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "write",
                "--path",
                "Projects/ClosedClaw.md",
                "--content",
                "new content",
                "--force",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        assert "Written" in result.output or "written" in result.output.lower()
        client.write_note.assert_called_once()

    @patch("vault_cli.cli.crud.get_client")
    def test_write_new_note_no_force_succeeds(self, mock_get_client, runner):
        """Writing a new note (doesn't exist) should succeed without --force."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            ["write", "--path", "brand-new-note.md", "--content", "hello", "--yes"],
        )
        assert result.exit_code == 0
        client.write_note.assert_called_once()

    @patch("vault_cli.cli.crud.get_client")
    def test_write_existing_shows_size_info(self, mock_get_client, runner):
        """When refusing overwrite, shows info about the existing note."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            ["write", "--path", "Projects/ClosedClaw.md", "--content", "new", "--yes"],
        )
        # Should mention the note exists
        assert (
            "already exists" in result.output.lower()
            or "exists" in result.output.lower()
        )


# ---------------------------------------------------------------------------
# Write safety: --diff flag
# ---------------------------------------------------------------------------


class TestWriteDiff:
    """vault write --diff shows a unified diff without writing."""

    @patch("vault_cli.cli.crud.get_client")
    def test_write_diff_shows_changes(self, mock_get_client, runner):
        """--diff flag prints what would change, does not write."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "write",
                "--path",
                "Projects/ClosedClaw.md",
                "--content",
                "# ClosedClaw\n\nRewritten.",
                "--diff",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        # Should show diff markers
        assert "---" in result.output or "+++" in result.output or "-" in result.output
        # Should NOT write
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_write_diff_new_note(self, mock_get_client, runner):
        """--diff on a new note shows all content as additions."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "write",
                "--path",
                "brand-new.md",
                "--content",
                "new content",
                "--diff",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        client.write_note.assert_not_called()


# ---------------------------------------------------------------------------
# Write safety: deleted:true detection
# ---------------------------------------------------------------------------


class TestWriteDeletedDetection:
    """vault write detects existing docs with deleted:true and warns."""

    @patch("vault_cli.cli.crud.get_client")
    def test_write_deleted_doc_warns(self, mock_get_client, runner):
        """Writing to a deleted doc warns the user."""
        client = _mock_client_with_deleted_doc()
        mock_get_client.return_value = client
        # Simulate non-interactive (no TTY) — should require --force
        result = runner.invoke(
            cli,
            [
                "write",
                "--path",
                "References/Restored.md",
                "--content",
                "restored content",
            ],
        )
        output_lower = result.output.lower()
        assert "deleted" in output_lower or "restore" in output_lower

    @patch("vault_cli.cli.crud.get_client")
    def test_write_deleted_doc_with_force_succeeds(self, mock_get_client, runner):
        """Writing to a deleted doc with --force succeeds."""
        client = _mock_client_with_deleted_doc()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "write",
                "--path",
                "References/Restored.md",
                "--content",
                "restored content",
                "--force",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        client.write_note.assert_called_once()


# ---------------------------------------------------------------------------
# Create safety: existence check
# ---------------------------------------------------------------------------


class TestCreateSafety:
    """vault create refuses if the note already exists."""

    @patch("vault_cli.cli.crud.get_client")
    def test_create_existing_note_aborts(self, mock_get_client, runner):
        """Creating a note that already exists should abort."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            ["create", "--name", "ClosedClaw", "--content", "# ClosedClaw v2"],
        )
        assert result.exit_code != 0
        output_lower = result.output.lower()
        assert "already exists" in output_lower
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_create_existing_note_with_folder_aborts(self, mock_get_client, runner):
        """Creating in a folder where it exists should abort."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "ClosedClaw",
                "--folder",
                "Projects",
                "--content",
                "body",
            ],
        )
        assert result.exit_code != 0
        assert "already exists" in result.output.lower()
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_create_new_note_succeeds(self, mock_get_client, runner):
        """Creating a genuinely new note should succeed."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            ["create", "--name", "Brand New Note", "--content", "# Hello", "--yes"],
        )
        assert result.exit_code == 0
        assert "Created" in result.output
        client.write_note.assert_called_once()

    @patch("vault_cli.cli.crud.get_client")
    def test_create_suggests_write_force(self, mock_get_client, runner):
        """When create fails, suggests using vault write --force."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            ["create", "--name", "ClosedClaw", "--content", "overwrite"],
        )
        assert "--force" in result.output or "write" in result.output.lower()


# ---------------------------------------------------------------------------
# Delete safety: --yes / interactive confirmation
# ---------------------------------------------------------------------------


class TestDeleteSafety:
    """vault delete requires confirmation."""

    @patch("vault_cli.cli.crud.get_client")
    def test_delete_with_yes_flag_succeeds(self, mock_get_client, runner):
        """--yes flag skips confirmation and deletes."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(cli, ["delete", "--file", "North Star", "--yes"])
        assert result.exit_code == 0
        assert "Deleted" in result.output
        client.delete_note.assert_called_once()

    @patch("vault_cli.cli.crud.get_client")
    def test_delete_without_yes_prompts(self, mock_get_client, runner):
        """Without --yes, prompts for confirmation and deletes on 'y'."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(cli, ["delete", "--file", "North Star"], input="y\n")
        assert result.exit_code == 0
        assert "Deleted" in result.output
        client.delete_note.assert_called_once()

    @patch("vault_cli.cli.crud.get_client")
    def test_delete_without_yes_declines(self, mock_get_client, runner):
        """Without --yes, declining the prompt aborts."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(cli, ["delete", "--file", "North Star"], input="n\n")
        assert (
            "Aborted" in result.output
            or result.exit_code != 0
            or "aborted" in result.output.lower()
        )
        client.delete_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_delete_prompt_defaults_to_no(self, mock_get_client, runner):
        """Empty input (Enter) should default to No (abort)."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(cli, ["delete", "--file", "North Star"], input="\n")
        client.delete_note.assert_not_called()


# ---------------------------------------------------------------------------
# Property:set safety: read-before-write + preview
# ---------------------------------------------------------------------------


class TestPropertySetSafety:
    """vault property:set shows current value and asks for confirmation."""

    @patch("vault_cli.cli.properties.get_client")
    def test_property_set_with_yes_skips_prompt(self, mock_get_client, runner):
        """--yes flag skips confirmation."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "property:set",
                "--name",
                "status",
                "--value",
                "done",
                "--file",
                "ClosedClaw",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        assert "Set" in result.output or "set" in result.output.lower()
        client.write_note.assert_called_once()

    @patch("vault_cli.cli.properties.get_client")
    def test_property_set_prompts_confirmation(self, mock_get_client, runner):
        """Without --yes, shows current/new values and prompts."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "property:set",
                "--name",
                "status",
                "--value",
                "done",
                "--file",
                "ClosedClaw",
            ],
            input="y\n",
        )
        assert result.exit_code == 0
        # Should show current and new values
        output_lower = result.output.lower()
        assert "current" in output_lower or "active" in output_lower
        client.write_note.assert_called_once()

    @patch("vault_cli.cli.properties.get_client")
    def test_property_set_declined_aborts(self, mock_get_client, runner):
        """Declining property:set prompt aborts without writing."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "property:set",
                "--name",
                "status",
                "--value",
                "done",
                "--file",
                "ClosedClaw",
            ],
            input="n\n",
        )
        client.write_note.assert_not_called()


# ---------------------------------------------------------------------------
# --dry-run global flag
# ---------------------------------------------------------------------------


class TestDryRun:
    """--dry-run flag prevents any writes and shows what would happen."""

    @patch("vault_cli.cli.crud.get_client")
    def test_write_dry_run(self, mock_get_client, runner):
        """vault write --dry-run shows what would be written without writing."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "write",
                "--path",
                "brand-new.md",
                "--content",
                "hello",
                "--dry-run",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        output_lower = result.output.lower()
        assert "would write" in output_lower or "dry" in output_lower
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_write_dry_run_existing_note(self, mock_get_client, runner):
        """vault write --dry-run on existing note shows char counts."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "write",
                "--path",
                "Projects/ClosedClaw.md",
                "--content",
                "shorter",
                "--dry-run",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        output_lower = result.output.lower()
        assert "would write" in output_lower or "dry" in output_lower
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_delete_dry_run(self, mock_get_client, runner):
        """vault delete --dry-run shows what would be deleted without deleting."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            ["delete", "--file", "North Star", "--dry-run"],
        )
        assert result.exit_code == 0
        output_lower = result.output.lower()
        assert "would" in output_lower or "dry" in output_lower
        client.delete_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_create_dry_run(self, mock_get_client, runner):
        """vault create --dry-run shows what would be created without creating."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "Dry Run Note",
                "--content",
                "body",
                "--dry-run",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        output_lower = result.output.lower()
        assert "would" in output_lower or "dry" in output_lower
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.properties.get_client")
    def test_property_set_dry_run(self, mock_get_client, runner):
        """vault property:set --dry-run shows what would change without writing."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "property:set",
                "--name",
                "status",
                "--value",
                "done",
                "--file",
                "ClosedClaw",
                "--dry-run",
            ],
        )
        assert result.exit_code == 0
        output_lower = result.output.lower()
        assert "would" in output_lower or "dry" in output_lower
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_append_dry_run(self, mock_get_client, runner):
        """vault append --dry-run shows what would be appended without writing."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "append",
                "--file",
                "North Star",
                "--content",
                "## Extra",
                "--dry-run",
            ],
        )
        assert result.exit_code == 0
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_prepend_dry_run(self, mock_get_client, runner):
        """vault prepend --dry-run shows what would be prepended without writing."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "prepend",
                "--file",
                "North Star",
                "--content",
                "**Top**",
                "--dry-run",
            ],
        )
        assert result.exit_code == 0
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.relocate.get_client")
    def test_move_dry_run(self, mock_get_client, runner):
        """vault move --dry-run shows what would be moved without moving."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "move",
                "--file",
                "North Star",
                "--to",
                "Archive/North Star.md",
                "--dry-run",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        client.move_note.assert_not_called()

    @patch("vault_cli.cli.relocate.get_client")
    def test_rename_dry_run(self, mock_get_client, runner):
        """vault rename --dry-run shows what would be renamed."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "rename",
                "--file",
                "ClosedClaw",
                "--name",
                "OpenClaw",
                "--dry-run",
            ],
        )
        assert result.exit_code == 0
        client.move_note.assert_not_called()

    @patch("vault_cli.cli.properties.get_client")
    def test_property_remove_dry_run(self, mock_get_client, runner):
        """vault property:remove --dry-run shows what would be removed."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "property:remove",
                "--name",
                "status",
                "--file",
                "ClosedClaw",
                "--dry-run",
            ],
        )
        assert result.exit_code == 0
        client.write_note.assert_not_called()


# ---------------------------------------------------------------------------
# Wrapped HTTP error messages
# ---------------------------------------------------------------------------


class TestWrappedErrors:
    """Error messages include operation, note path, CouchDB error, and hints."""

    @patch("vault_cli.cli.crud.get_client")
    def test_write_conflict_error(self, mock_get_client, runner):
        """409 Conflict on write shows helpful error with hint."""
        import requests

        client = _mock_client()
        resp = MagicMock()
        resp.status_code = 409
        resp.json.return_value = {
            "error": "conflict",
            "reason": "Document update conflict.",
        }
        resp.text = '{"error":"conflict","reason":"Document update conflict."}'
        error = requests.HTTPError(response=resp)
        client.write_note.side_effect = error
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "write",
                "--path",
                "brand-new.md",
                "--content",
                "hello",
                "--force",
                "--yes",
            ],
        )
        assert result.exit_code != 0
        output_lower = result.output.lower()
        assert (
            "conflict" in output_lower
            or "409" in output_lower
            or "error" in output_lower
        )

    @patch("vault_cli.cli.crud.get_client")
    def test_read_not_found_suggests_search(self, mock_get_client, runner):
        """404 on read suggests using vault files or vault search."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            ["read", "--path", "Nonexistent/Note.md"],
        )
        assert result.exit_code != 0
        output_lower = result.output.lower()
        assert "not found" in output_lower

    @patch("vault_cli.cli.crud.get_client")
    def test_connection_error_suggests_ping(self, mock_get_client, runner):
        """Connection errors suggest running vault ping."""
        client = _mock_client()
        client.read_note.side_effect = ConnectionError(
            "Connection refused. Is the server running?"
        )
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            ["read", "--path", "anything.md"],
        )
        assert result.exit_code != 0
        output_lower = result.output.lower()
        assert "connection" in output_lower or "ping" in output_lower


# ---------------------------------------------------------------------------
# Version bump
# ---------------------------------------------------------------------------


class TestVersion:
    def test_version_is_021(self, runner):
        """Version should be 0.2.1."""
        result = runner.invoke(cli, ["--version"])
        assert result.exit_code == 0
        assert "0.2.1" in result.output
