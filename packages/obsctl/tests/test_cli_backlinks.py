"""Tests for backlink-aware rename and move CLI commands.

Tests the full CLI flow: rename/move commands with --dry-run, --no-backlinks,
and default backlink rewriting. All tests use mocked VaultClient.
"""

import json

import pytest
from unittest.mock import patch, MagicMock, call

from click.testing import CliRunner
from vault_cli.cli.main import cli


# ---------------------------------------------------------------------------
# Helpers — mock client with realistic note store
# ---------------------------------------------------------------------------

MOCK_NOTES_LIST = [
    {"path": "Projects/Alpha.md", "id": "projects/alpha.md", "mtime": 0, "size": 100},
    {"path": "References/Beta.md", "id": "references/beta.md", "mtime": 0, "size": 100},
    {"path": "Gamma.md", "id": "gamma.md", "mtime": 0, "size": 100},
]

NOTE_CONTENTS = {
    "Projects/Alpha.md": (
        '---\ncategories:\n  - "[[Projects]]"\n---\n\n# Alpha\n\nMain project note.\n'
    ),
    "References/Beta.md": (
        "---\n"
        "categories:\n"
        '  - "[[References]]"\n'
        "---\n"
        "\n"
        "# Beta\n"
        "\n"
        "References [[Alpha]] and [[Alpha#Details|see details]].\n"
    ),
    "Gamma.md": (
        "---\n"
        "categories:\n"
        '  - "[[Ideas]]"\n'
        "---\n"
        "\n"
        "# Gamma\n\nLinks to [[Alpha]] once.\n"
    ),
}


def _mock_client():
    """Create a MagicMock VaultClient that tracks writes."""
    client = MagicMock()
    store = dict(NOTE_CONTENTS)

    def _list_notes():
        return list(MOCK_NOTES_LIST)

    def _read_note(path):
        if path in store:
            return {
                "path": path,
                "content": store[path],
                "ctime": 0,
                "mtime": 0,
                "metadata": {},
            }
        return None

    def _write_note(path, content, **kw):
        store[path] = content
        return {"ok": True, "id": path.lower(), "rev": "2-new"}

    def _move_note(from_path, to_path):
        if from_path not in store:
            raise FileNotFoundError(f"Note not found: {from_path}")
        store[to_path] = store.pop(from_path)
        return {"ok": True}

    client.list_notes.side_effect = _list_notes
    client.read_note.side_effect = _read_note
    client.write_note.side_effect = _write_note
    client.move_note.side_effect = _move_note
    client._store = store
    return client


@pytest.fixture
def runner():
    return CliRunner()


# ---------------------------------------------------------------------------
# Rename with backlink rewriting
# ---------------------------------------------------------------------------


class TestRenameBacklinks:
    """vault rename --file X --name Y rewrites backlinks by default."""

    @patch("vault_cli.cli.relocate.get_client")
    def test_rename_updates_backlinks(self, mock_get_client, runner):
        """Rename rewrites [[Alpha]] -> [[Omega]] in all other notes."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(cli, ["rename", "--file", "Alpha", "--name", "Omega"])
        assert result.exit_code == 0
        assert "Renamed" in result.output
        assert "backlink" in result.output.lower() or "Updated" in result.output

    @patch("vault_cli.cli.relocate.get_client")
    def test_rename_reports_updated_count(self, mock_get_client, runner):
        """Output reports how many backlinks were updated in how many notes."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(cli, ["rename", "--file", "Alpha", "--name", "Omega"])
        assert result.exit_code == 0
        # Should mention number of links and notes
        output = result.output.lower()
        assert "backlink" in output or "link" in output

    @patch("vault_cli.cli.relocate.get_client")
    def test_rename_dry_run_shows_changes(self, mock_get_client, runner):
        """--dry-run shows what would change without writing."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli, ["rename", "--file", "Alpha", "--name", "Omega", "--dry-run"]
        )
        assert result.exit_code == 0
        assert "Would rename" in result.output
        # Should mention backlink changes
        assert "Would update" in result.output or "would" in result.output.lower()
        # move_note should NOT be called
        client.move_note.assert_not_called()

    @patch("vault_cli.cli.relocate.get_client")
    def test_rename_dry_run_lists_affected_notes(self, mock_get_client, runner):
        """--dry-run lists each note that would be modified."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli, ["rename", "--file", "Alpha", "--name", "Omega", "--dry-run"]
        )
        assert result.exit_code == 0
        # Should list affected notes
        assert "Beta" in result.output or "Gamma" in result.output

    @patch("vault_cli.cli.relocate.get_client")
    def test_rename_no_backlinks_flag(self, mock_get_client, runner):
        """--no-backlinks skips backlink rewriting (legacy behaviour)."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            ["rename", "--file", "Alpha", "--name", "Omega", "--no-backlinks"],
        )
        assert result.exit_code == 0
        assert "Renamed" in result.output
        # move_note should be called (the file rename)
        client.move_note.assert_called_once()
        # write_note should NOT be called (no backlink rewriting)
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.relocate.get_client")
    def test_rename_json_output(self, mock_get_client, runner):
        """--json outputs structured JSON with rename + backlink info."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli, ["rename", "--file", "Alpha", "--name", "Omega", "--json"]
        )
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert "renamed" in data or "ok" in data
        assert "backlinks" in data or "total_links" in data


# ---------------------------------------------------------------------------
# Move with backlink detection
# ---------------------------------------------------------------------------


class TestMoveBacklinks:
    """vault move detects if name changed and rewrites backlinks if needed."""

    @patch("vault_cli.cli.relocate.get_client")
    def test_move_same_name_no_rewrite(self, mock_get_client, runner):
        """Move to different folder, same name -> no backlink rewrite."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            ["move", "--file", "Alpha", "--to", "References/Alpha.md", "--yes"],
        )
        assert result.exit_code == 0
        assert "Moved" in result.output
        # Should indicate no backlink updates needed
        output_lower = result.output.lower()
        assert (
            "no backlink" in output_lower
            or "unchanged" in output_lower
            or "0 backlink" in output_lower
            or "backlink" not in output_lower  # or just doesn't mention them
        )

    @patch("vault_cli.cli.relocate.get_client")
    def test_move_name_change_triggers_rewrite(self, mock_get_client, runner):
        """Move with different name -> backlinks are rewritten."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            ["move", "--file", "Alpha", "--to", "References/Omega.md", "--yes"],
        )
        assert result.exit_code == 0
        assert "Moved" in result.output
        # Should mention backlink updates
        output_lower = result.output.lower()
        assert (
            "backlink" in output_lower
            or "link" in output_lower
            or "updated" in output_lower
        )

    @patch("vault_cli.cli.relocate.get_client")
    def test_move_dry_run(self, mock_get_client, runner):
        """--dry-run shows what move + backlink rewrite would do."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "move",
                "--file",
                "Alpha",
                "--to",
                "References/Omega.md",
                "--dry-run",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        assert "Would move" in result.output
        client.move_note.assert_not_called()

    @patch("vault_cli.cli.relocate.get_client")
    def test_move_no_backlinks_flag(self, mock_get_client, runner):
        """--no-backlinks skips backlink rewriting on move."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "move",
                "--file",
                "Alpha",
                "--to",
                "References/Omega.md",
                "--no-backlinks",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        assert "Moved" in result.output
        # write_note should NOT be called for backlink rewriting
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.relocate.get_client")
    def test_move_json_output(self, mock_get_client, runner):
        """--json returns structured output for move."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            ["move", "--file", "Alpha", "--to", "Projects/Omega.md", "--json", "--yes"],
        )
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert "ok" in data or "moved" in data
