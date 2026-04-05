"""Tests for vault rule guardrails — CLI integration layer.

Tests cover:
- --yes flag accepts all warnings and proceeds
- --strict flag turns warnings into hard errors (exit code 2)
- Default interactive mode prompts and waits for y/N
- Exit code 2 for rejected rule violations (distinct from error code 1)
- --force on write does NOT bypass rule checks
- Warning output format (parseable: starts with "⚠ Rule:")
- Guardrails on create, write, and move commands
"""

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

CONTENT_WITH_CATEGORIES = '---\ncategories:\n  - "[[Projects]]"\n---\n\n# My Note\n'

CONTENT_NO_CATEGORIES = "# My Note\n\nJust text.\n"

CONTENT_REFERENCES_CATEGORY = (
    '---\ncategories:\n  - "[[References]]"\n---\n\n# External\n'
)


def _mock_client():
    """Create a MagicMock VaultClient."""
    client = MagicMock()
    client.list_notes.return_value = MOCK_NOTES_LIST
    client.ping.return_value = {"ok": True}
    client.write_note.return_value = {"ok": True, "id": "test", "rev": "1-abc"}
    client.delete_note.return_value = {"ok": True}
    client.move_note.return_value = {"ok": True}

    def _read_note(path):
        content_map = {
            "Projects/ClosedClaw.md": CONTENT_WITH_CATEGORIES,
            "North Star.md": (
                '---\ncategories:\n  - "[[Navigation]]"\n---\n\n# North Star\n'
            ),
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


@pytest.fixture
def runner():
    return CliRunner()


# ---------------------------------------------------------------------------
# Create command: --yes flag
# ---------------------------------------------------------------------------


class TestCreateGuardrailsYes:
    """vault create --yes accepts rule warnings and proceeds."""

    @patch("vault_cli.cli.crud.get_client")
    def test_create_new_folder_with_yes_proceeds(self, mock_get_client, runner):
        """--yes should accept the new-folder warning and create."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "My Idea",
                "--folder",
                "Random Stuff",
                "--content",
                CONTENT_WITH_CATEGORIES,
                "--yes",
            ],
        )
        assert result.exit_code == 0
        assert "Created" in result.output
        # Should log the accepted warning
        assert "Rule:" in result.output or "accepted" in result.output.lower()
        client.write_note.assert_called_once()

    @patch("vault_cli.cli.crud.get_client")
    def test_create_missing_categories_with_yes_proceeds(self, mock_get_client, runner):
        """--yes should accept missing-categories warning and create."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "Plain Note",
                "--content",
                CONTENT_NO_CATEGORIES,
                "--yes",
            ],
        )
        assert result.exit_code == 0
        assert "Created" in result.output
        client.write_note.assert_called_once()


# ---------------------------------------------------------------------------
# Create command: --strict flag
# ---------------------------------------------------------------------------


class TestCreateGuardrailsStrict:
    """vault create --strict turns rule violations into hard errors."""

    @patch("vault_cli.cli.crud.get_client")
    def test_create_new_folder_strict_fails(self, mock_get_client, runner):
        """--strict should abort with exit code 2 on new folder."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "My Idea",
                "--folder",
                "Random Stuff",
                "--content",
                CONTENT_WITH_CATEGORIES,
                "--strict",
            ],
        )
        assert result.exit_code == 2
        assert "Rule" in result.output or "strict" in result.output.lower()
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_create_missing_categories_strict_fails(self, mock_get_client, runner):
        """--strict should abort on missing categories."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "Plain Note",
                "--content",
                CONTENT_NO_CATEGORIES,
                "--strict",
            ],
        )
        assert result.exit_code == 2
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_create_no_violations_strict_succeeds(self, mock_get_client, runner):
        """--strict should succeed when there are no violations."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "Good Note",
                "--folder",
                "Projects",
                "--content",
                CONTENT_WITH_CATEGORIES,
                "--strict",
            ],
        )
        assert result.exit_code == 0
        assert "Created" in result.output
        client.write_note.assert_called_once()


# ---------------------------------------------------------------------------
# Create command: interactive mode (default)
# ---------------------------------------------------------------------------


class TestCreateGuardrailsInteractive:
    """Default mode prompts for confirmation on rule violations."""

    @patch("vault_cli.cli.crud.get_client")
    def test_create_new_folder_prompts_accept(self, mock_get_client, runner):
        """Accepting the prompt should proceed."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "My Idea",
                "--folder",
                "Random Stuff",
                "--content",
                CONTENT_WITH_CATEGORIES,
            ],
            input="y\n",
        )
        assert result.exit_code == 0
        assert "Created" in result.output
        client.write_note.assert_called_once()

    @patch("vault_cli.cli.crud.get_client")
    def test_create_new_folder_prompts_reject(self, mock_get_client, runner):
        """Rejecting the prompt should abort with exit code 2."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "My Idea",
                "--folder",
                "Random Stuff",
                "--content",
                CONTENT_WITH_CATEGORIES,
            ],
            input="n\n",
        )
        assert result.exit_code == 2
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_create_default_is_no(self, mock_get_client, runner):
        """Empty input (Enter) should default to No (abort)."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "My Idea",
                "--folder",
                "Random Stuff",
                "--content",
                CONTENT_WITH_CATEGORIES,
            ],
            input="\n",
        )
        assert result.exit_code == 2
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_create_no_violation_no_prompt(self, mock_get_client, runner):
        """No violations = no prompt, just proceed."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "Good Note",
                "--folder",
                "Projects",
                "--content",
                CONTENT_WITH_CATEGORIES,
            ],
        )
        assert result.exit_code == 0
        assert "Proceed anyway?" not in result.output
        client.write_note.assert_called_once()


# ---------------------------------------------------------------------------
# Write command: guardrails
# ---------------------------------------------------------------------------


class TestWriteGuardrails:
    """vault write applies guardrails to new notes."""

    @patch("vault_cli.cli.crud.get_client")
    def test_write_new_folder_with_yes(self, mock_get_client, runner):
        """--yes bypasses the folder warning on write."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "write",
                "--path",
                "NewFolder/Note.md",
                "--content",
                CONTENT_WITH_CATEGORIES,
                "--yes",
            ],
        )
        assert result.exit_code == 0
        assert "Written" in result.output
        client.write_note.assert_called_once()

    @patch("vault_cli.cli.crud.get_client")
    def test_write_new_folder_strict_fails(self, mock_get_client, runner):
        """--strict fails on folder violation."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "write",
                "--path",
                "NewFolder/Note.md",
                "--content",
                CONTENT_WITH_CATEGORIES,
                "--strict",
            ],
        )
        assert result.exit_code == 2
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_write_force_does_not_bypass_rules(self, mock_get_client, runner):
        """--force is for overwrite, not for rule bypass. Rules still apply."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "write",
                "--path",
                "NewFolder/Note.md",
                "--content",
                CONTENT_WITH_CATEGORIES,
                "--force",
                "--strict",
            ],
        )
        # --force + --strict: rules still trigger exit 2
        assert result.exit_code == 2
        client.write_note.assert_not_called()

    @patch("vault_cli.cli.crud.get_client")
    def test_write_missing_categories_interactive_reject(self, mock_get_client, runner):
        """Interactive rejection of missing categories."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "write",
                "--path",
                "Projects/NoCat.md",
                "--content",
                CONTENT_NO_CATEGORIES,
            ],
            input="n\n",
        )
        assert result.exit_code == 2
        client.write_note.assert_not_called()


# ---------------------------------------------------------------------------
# Move command: guardrails
# ---------------------------------------------------------------------------


class TestMoveGuardrails:
    """vault move applies guardrails on the target path."""

    @patch("vault_cli.cli.relocate.get_client")
    def test_move_to_new_folder_with_yes(self, mock_get_client, runner):
        """--yes bypasses folder warning on move."""
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
                "--yes",
            ],
        )
        assert result.exit_code == 0
        assert "Moved" in result.output

    @patch("vault_cli.cli.relocate.get_client")
    def test_move_to_new_folder_strict_fails(self, mock_get_client, runner):
        """--strict fails on move to non-existent folder."""
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
                "--strict",
            ],
        )
        assert result.exit_code == 2
        client.move_note.assert_not_called()

    @patch("vault_cli.cli.relocate.get_client")
    def test_move_to_existing_folder_no_warning(self, mock_get_client, runner):
        """Moving to an existing folder should not trigger warnings."""
        client = _mock_client()
        mock_get_client.return_value = client
        # Move North Star (has [[Navigation]] category) to Projects/ (existing, not References)
        result = runner.invoke(
            cli,
            [
                "move",
                "--file",
                "North Star",
                "--to",
                "Projects/North Star.md",
            ],
        )
        assert result.exit_code == 0
        assert "Proceed anyway?" not in result.output


# ---------------------------------------------------------------------------
# Warning output format
# ---------------------------------------------------------------------------


class TestWarningOutputFormat:
    """Warning output is parseable by agents."""

    @patch("vault_cli.cli.crud.get_client")
    def test_warning_starts_with_rule_marker(self, mock_get_client, runner):
        """Warnings should contain '⚠ Rule:' for parseability."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "My Idea",
                "--folder",
                "Random Stuff",
                "--content",
                CONTENT_WITH_CATEGORIES,
                "--yes",
            ],
        )
        assert "⚠ Rule:" in result.output

    @patch("vault_cli.cli.crud.get_client")
    def test_warning_includes_rule_name(self, mock_get_client, runner):
        """Warning includes the rule name after the marker."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "My Idea",
                "--folder",
                "Random Stuff",
                "--content",
                CONTENT_WITH_CATEGORIES,
                "--strict",
            ],
        )
        assert "Folder placement" in result.output

    @patch("vault_cli.cli.crud.get_client")
    def test_yes_mode_logs_accepted(self, mock_get_client, runner):
        """--yes mode should log that warnings were accepted."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "My Idea",
                "--folder",
                "Random Stuff",
                "--content",
                CONTENT_WITH_CATEGORIES,
                "--yes",
            ],
        )
        assert "accepted" in result.output.lower() or "--yes" in result.output


# ---------------------------------------------------------------------------
# Exit codes
# ---------------------------------------------------------------------------


class TestExitCodes:
    """Exit codes: 0 = success, 1 = error, 2 = rule violation rejected."""

    @patch("vault_cli.cli.crud.get_client")
    def test_exit_code_0_no_violations(self, mock_get_client, runner):
        """Clean create returns exit code 0."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "Good Note",
                "--folder",
                "Projects",
                "--content",
                CONTENT_WITH_CATEGORIES,
            ],
        )
        assert result.exit_code == 0

    @patch("vault_cli.cli.crud.get_client")
    def test_exit_code_2_strict_violation(self, mock_get_client, runner):
        """Rule violation in strict mode returns exit code 2."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "My Idea",
                "--folder",
                "Random Stuff",
                "--content",
                CONTENT_WITH_CATEGORIES,
                "--strict",
            ],
        )
        assert result.exit_code == 2

    @patch("vault_cli.cli.crud.get_client")
    def test_exit_code_2_interactive_rejected(self, mock_get_client, runner):
        """Rejected rule violation in interactive mode returns exit code 2."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "My Idea",
                "--folder",
                "Random Stuff",
                "--content",
                CONTENT_WITH_CATEGORIES,
            ],
            input="n\n",
        )
        assert result.exit_code == 2

    @patch("vault_cli.cli.crud.get_client")
    def test_exit_code_1_for_real_errors(self, mock_get_client, runner):
        """Real errors (not rule violations) still return exit code 1."""
        client = _mock_client()
        mock_get_client.return_value = client
        # Creating an existing note is a real error, not a rule violation
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "ClosedClaw",
                "--content",
                "overwrite",
            ],
        )
        assert result.exit_code == 1


# ---------------------------------------------------------------------------
# Category mismatch on CLI
# ---------------------------------------------------------------------------


class TestCategoryMismatchCLI:
    """Folder ↔ category mismatch guardrails at CLI level."""

    @patch("vault_cli.cli.crud.get_client")
    def test_references_category_at_root_warns(self, mock_get_client, runner):
        """Creating at root with [[References]] category triggers warning."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "External Thing",
                "--content",
                CONTENT_REFERENCES_CATEGORY,
                "--strict",
            ],
        )
        assert result.exit_code == 2
        assert "Placement rules" in result.output or "Reference" in result.output

    @patch("vault_cli.cli.crud.get_client")
    def test_references_category_in_references_ok(self, mock_get_client, runner):
        """Creating in References/ with [[References]] category is fine."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "External Thing",
                "--folder",
                "References",
                "--content",
                CONTENT_REFERENCES_CATEGORY,
            ],
        )
        assert result.exit_code == 0
        assert "Proceed anyway?" not in result.output


# ---------------------------------------------------------------------------
# Dry-run + guardrails interaction
# ---------------------------------------------------------------------------


class TestDryRunAndGuardrails:
    """--dry-run should still show rule violations (but not write)."""

    @patch("vault_cli.cli.crud.get_client")
    def test_dry_run_still_shows_warnings(self, mock_get_client, runner):
        """--dry-run should not suppress rule warnings."""
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "My Idea",
                "--folder",
                "Random Stuff",
                "--content",
                CONTENT_WITH_CATEGORIES,
                "--dry-run",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        # Should show the warning even in dry-run
        assert "Rule:" in result.output or "Folder placement" in result.output
        client.write_note.assert_not_called()
