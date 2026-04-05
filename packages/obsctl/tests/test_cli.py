"""Tests for CLI commands.

All commands are tested via Click's CliRunner with a mocked VaultClient.
No real HTTP calls are made.
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

PEOPLE_TEMPLATE_CONTENT = (
    "---\n"
    "categories:\n"
    '  - "[[People]]"\n'
    "tags: []\n"
    "---\n"
    "\n"
    "# {{name}}\n"
    "\n"
    "## Background\n"
    "\n"
    "## Notes\n"
)


def _mock_client():
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
            "Templates/People Template.md": PEOPLE_TEMPLATE_CONTENT,
        }
        if path in content_map:
            return {
                "path": path,
                "content": content_map[path],
                "ctime": 1709500000000,
                "mtime": 1709500000000,
                "metadata": {},
            }
        return None

    client.read_note.side_effect = _read_note
    return client


@pytest.fixture
def runner():
    return CliRunner()


# ---------------------------------------------------------------------------
# Version & Help
# ---------------------------------------------------------------------------


class TestVersionAndHelp:
    def test_version(self, runner):
        result = runner.invoke(cli, ["--version"])
        assert result.exit_code == 0
        assert "0.2.1" in result.output

    def test_help(self, runner):
        result = runner.invoke(cli, ["--help"])
        assert result.exit_code == 0
        assert "Obsidian vault CLI" in result.output

    def test_help_lists_all_commands(self, runner):
        result = runner.invoke(cli, ["--help"])
        expected_commands = [
            "read",
            "create",
            "write",
            "append",
            "prepend",
            "delete",
            "move",
            "rename",
            "files",
            "folders",
            "search",
            "backlinks",
            "links",
            "unresolved",
            "orphans",
            "tags",
            "tag",
            "properties",
            "property:read",
            "property:set",
            "property:remove",
            "templates",
            "template:read",
            "ping",
            "config",
        ]
        for cmd in expected_commands:
            assert cmd in result.output, f"Command '{cmd}' not in help output"


# ---------------------------------------------------------------------------
# Ping
# ---------------------------------------------------------------------------


class TestPing:
    @patch("vault_cli.cli.config_cmd.get_client")
    def test_ping_success(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["ping"])
        assert result.exit_code == 0
        assert "Connected" in result.output

    @patch("vault_cli.cli.config_cmd.get_client")
    def test_ping_failure(self, mock_get_client, runner):
        client = _mock_client()
        client.ping.side_effect = ConnectionError("refused")
        mock_get_client.return_value = client
        result = runner.invoke(cli, ["ping"])
        assert result.exit_code != 0

    @patch("vault_cli.cli.config_cmd.get_client")
    def test_ping_json(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["ping", "--json"])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert data["ok"] is True


# ---------------------------------------------------------------------------
# Read
# ---------------------------------------------------------------------------


class TestRead:
    @patch("vault_cli.cli.crud.get_client")
    def test_read_by_file(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["read", "--file", "ClosedClaw"])
        assert result.exit_code == 0
        assert "# ClosedClaw" in result.output

    @patch("vault_cli.cli.crud.get_client")
    def test_read_by_path(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["read", "--path", "Projects/ClosedClaw.md"])
        assert result.exit_code == 0
        assert "# ClosedClaw" in result.output

    @patch("vault_cli.cli.crud.get_client")
    def test_read_missing_file(self, mock_get_client, runner):
        client = _mock_client()
        client.read_note.side_effect = lambda p: None
        mock_get_client.return_value = client
        result = runner.invoke(cli, ["read", "--path", "Nonexistent.md"])
        assert result.exit_code != 0

    @patch("vault_cli.cli.crud.get_client")
    def test_read_no_args(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["read"])
        assert result.exit_code != 0

    @patch("vault_cli.cli.crud.get_client")
    def test_read_json(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["read", "--file", "ClosedClaw", "--json"])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert "content" in data
        assert "# ClosedClaw" in data["content"]
        assert "path" in data


# ---------------------------------------------------------------------------
# Create
# ---------------------------------------------------------------------------


class TestCreate:
    @patch("vault_cli.cli.crud.get_client")
    def test_create_basic(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli, ["create", "--name", "Test Note", "--content", "# Hello", "--yes"]
        )
        assert result.exit_code == 0
        assert "Created" in result.output
        client.write_note.assert_called_once()
        call_args = client.write_note.call_args
        assert call_args[0][0] == "Test Note.md"
        assert call_args[0][1] == "# Hello"

    @patch("vault_cli.cli.crud.get_client")
    def test_create_with_folder(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "create",
                "--name",
                "Person",
                "--folder",
                "References",
                "--content",
                "body",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        call_args = client.write_note.call_args
        assert call_args[0][0] == "References/Person.md"

    @patch("vault_cli.cli.crud.get_client")
    def test_create_json(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        content = '---\ncategories:\n  - "[[Ideas]]"\n---\n\nbody'
        result = runner.invoke(
            cli, ["create", "--name", "X", "--content", content, "--json", "--yes"]
        )
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert data["ok"] is True


# ---------------------------------------------------------------------------
# Write
# ---------------------------------------------------------------------------


class TestWrite:
    @patch("vault_cli.cli.crud.get_client")
    def test_write(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli, ["write", "--path", "test.md", "--content", "hello", "--yes"]
        )
        assert result.exit_code == 0
        assert "Written" in result.output
        client.write_note.assert_called_once_with("test.md", "hello")


# ---------------------------------------------------------------------------
# Append / Prepend
# ---------------------------------------------------------------------------


class TestAppendPrepend:
    @patch("vault_cli.cli.crud.get_client")
    def test_append(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli, ["append", "--file", "North Star", "--content", "## Extra"]
        )
        assert result.exit_code == 0
        assert "Appended" in result.output
        # Verify content was concatenated
        call_args = client.write_note.call_args[0]
        assert call_args[1].endswith("## Extra")
        assert "# North Star" in call_args[1]

    @patch("vault_cli.cli.crud.get_client")
    def test_prepend(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli, ["prepend", "--file", "North Star", "--content", "**Top**"]
        )
        assert result.exit_code == 0
        assert "Prepended" in result.output
        call_args = client.write_note.call_args[0]
        assert call_args[1].startswith("**Top**")


# ---------------------------------------------------------------------------
# Delete
# ---------------------------------------------------------------------------


class TestDelete:
    @patch("vault_cli.cli.crud.get_client")
    def test_delete(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(cli, ["delete", "--file", "North Star", "--yes"])
        assert result.exit_code == 0
        assert "Deleted" in result.output
        client.delete_note.assert_called_once_with("North Star.md")


# ---------------------------------------------------------------------------
# Move / Rename
# ---------------------------------------------------------------------------


class TestMoveRename:
    @patch("vault_cli.cli.relocate.get_client")
    def test_move(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli,
            [
                "move",
                "--file",
                "North Star",
                "--to",
                "References/North Star.md",
                "--yes",
            ],
        )
        assert result.exit_code == 0
        assert "Moved" in result.output
        client.move_note.assert_called_once_with(
            "North Star.md", "References/North Star.md"
        )

    @patch("vault_cli.cli.relocate.get_client")
    def test_rename(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli, ["rename", "--file", "ClosedClaw", "--name", "OpenClaw"]
        )
        assert result.exit_code == 0
        assert "Renamed" in result.output
        call_args = client.move_note.call_args[0]
        assert call_args[0] == "Projects/ClosedClaw.md"
        assert "OpenClaw" in call_args[1]


# ---------------------------------------------------------------------------
# Files / Folders
# ---------------------------------------------------------------------------


class TestFilesAndFolders:
    @patch("vault_cli.cli.files.get_client")
    def test_files_lists_all(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["files"])
        assert result.exit_code == 0
        assert "ClosedClaw" in result.output
        assert "North Star" in result.output

    @patch("vault_cli.cli.files.get_client")
    def test_files_filter_folder(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["files", "--folder", "Projects"])
        assert result.exit_code == 0
        assert "ClosedClaw" in result.output
        assert "North Star" not in result.output

    @patch("vault_cli.cli.files.get_client")
    def test_files_total(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["files", "--total"])
        assert result.exit_code == 0
        assert "5" in result.output

    @patch("vault_cli.cli.files.get_client")
    def test_files_total_json(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["files", "--total", "--json"])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert data["total"] == 5

    @patch("vault_cli.cli.files.get_client")
    def test_folders(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["folders"])
        assert result.exit_code == 0
        assert "Projects" in result.output
        assert "References" in result.output
        assert "Templates" in result.output

    @patch("vault_cli.cli.files.get_client")
    def test_folders_json(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["folders", "--json"])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert "Projects" in data
        assert "References" in data


# ---------------------------------------------------------------------------
# Search
# ---------------------------------------------------------------------------


class TestSearch:
    @patch("vault_cli.cli.search.get_client")
    def test_search_basic(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(cli, ["search", "--query", "agent framework"])
        assert result.exit_code == 0
        assert "ClosedClaw" in result.output

    @patch("vault_cli.cli.search.get_client")
    def test_search_total(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(cli, ["search", "--query", "agent framework", "--total"])
        assert result.exit_code == 0
        # Should be a number
        assert result.output.strip().isdigit()

    @patch("vault_cli.cli.search.get_client")
    def test_search_context(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli, ["search", "--query", "agent framework", "--context"]
        )
        assert result.exit_code == 0

    @patch("vault_cli.cli.search.get_client")
    def test_search_no_results(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(cli, ["search", "--query", "zzzznonexistentzzzz"])
        assert result.exit_code == 0

    @patch("vault_cli.cli.search.get_client")
    def test_search_json(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(cli, ["search", "--query", "agent framework", "--json"])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert isinstance(data, list)


# ---------------------------------------------------------------------------
# Backlinks / Links / Unresolved / Orphans
# ---------------------------------------------------------------------------


class TestGraph:
    @patch("vault_cli.cli.graph.get_client")
    @patch("vault_cli.cli.helpers.get_client")
    def test_backlinks(self, mock_helpers_client, mock_graph_client, runner):
        client = _mock_client()
        mock_graph_client.return_value = client
        mock_helpers_client.return_value = client
        result = runner.invoke(cli, ["backlinks", "--file", "ClosedClaw"])
        assert result.exit_code == 0

    @patch("vault_cli.cli.graph.get_client")
    @patch("vault_cli.cli.helpers.get_client")
    def test_backlinks_counts(self, mock_helpers_client, mock_graph_client, runner):
        client = _mock_client()
        mock_graph_client.return_value = client
        mock_helpers_client.return_value = client
        result = runner.invoke(cli, ["backlinks", "--file", "ClosedClaw", "--counts"])
        assert result.exit_code == 0
        # Should output a number
        assert result.output.strip().isdigit()

    @patch("vault_cli.cli.graph.get_client")
    @patch("vault_cli.cli.helpers.get_client")
    def test_backlinks_json(self, mock_helpers_client, mock_graph_client, runner):
        client = _mock_client()
        mock_graph_client.return_value = client
        mock_helpers_client.return_value = client
        result = runner.invoke(cli, ["backlinks", "--file", "ClosedClaw", "--json"])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert "backlinks" in data
        assert "file" in data

    @patch("vault_cli.cli.graph.get_client")
    @patch("vault_cli.cli.helpers.get_client")
    def test_links(self, mock_helpers_client, mock_graph_client, runner):
        client = _mock_client()
        mock_graph_client.return_value = client
        mock_helpers_client.return_value = client
        result = runner.invoke(cli, ["links", "--file", "ClosedClaw"])
        assert result.exit_code == 0

    @patch("vault_cli.cli.graph.get_client")
    @patch("vault_cli.cli.helpers.get_client")
    def test_links_json(self, mock_helpers_client, mock_graph_client, runner):
        client = _mock_client()
        mock_graph_client.return_value = client
        mock_helpers_client.return_value = client
        result = runner.invoke(cli, ["links", "--file", "ClosedClaw", "--json"])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert "links" in data

    @patch("vault_cli.cli.graph.get_client")
    def test_unresolved(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["unresolved"])
        assert result.exit_code == 0

    @patch("vault_cli.cli.graph.get_client")
    def test_unresolved_json(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["unresolved", "--json"])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert "unresolved" in data
        assert "count" in data

    @patch("vault_cli.cli.graph.get_client")
    def test_orphans(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["orphans"])
        assert result.exit_code == 0

    @patch("vault_cli.cli.graph.get_client")
    def test_orphans_json(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["orphans", "--json"])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert "orphans" in data


# ---------------------------------------------------------------------------
# Tags
# ---------------------------------------------------------------------------


class TestTags:
    @patch("vault_cli.cli.tags.get_client")
    def test_tags_list(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["tags"])
        assert result.exit_code == 0
        assert "ai" in result.output

    @patch("vault_cli.cli.tags.get_client")
    def test_tags_counts(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["tags", "--counts"])
        assert result.exit_code == 0
        assert "ai:" in result.output or "ai: " in result.output

    @patch("vault_cli.cli.tags.get_client")
    def test_tags_sort_count(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["tags", "--counts", "--sort", "count"])
        assert result.exit_code == 0

    @patch("vault_cli.cli.tags.get_client")
    def test_tags_json(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["tags", "--json"])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert "ai" in data

    @patch("vault_cli.cli.tags.get_client")
    def test_tag_specific(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["tag", "--name", "ai"])
        assert result.exit_code == 0

    @patch("vault_cli.cli.tags.get_client")
    def test_tag_verbose(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["tag", "--name", "ai", "--verbose"])
        assert result.exit_code == 0

    @patch("vault_cli.cli.tags.get_client")
    def test_tag_json(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["tag", "--name", "ai", "--json"])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert data["tag"] == "ai"
        assert "notes" in data
        assert "count" in data


# ---------------------------------------------------------------------------
# Properties
# ---------------------------------------------------------------------------


class TestProperties:
    @patch("vault_cli.cli.properties.get_client")
    def test_properties(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["properties", "--file", "ClosedClaw"])
        assert result.exit_code == 0
        assert "status" in result.output
        assert "active" in result.output

    @patch("vault_cli.cli.properties.get_client")
    def test_properties_json(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(cli, ["properties", "--file", "ClosedClaw", "--json"])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert data["properties"]["status"] == "active"

    @patch("vault_cli.cli.properties.get_client")
    def test_property_read(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(
            cli, ["property:read", "--name", "status", "--file", "ClosedClaw"]
        )
        assert result.exit_code == 0
        assert "active" in result.output

    @patch("vault_cli.cli.properties.get_client")
    def test_property_read_missing(self, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        result = runner.invoke(
            cli, ["property:read", "--name", "nonexistent", "--file", "ClosedClaw"]
        )
        assert result.exit_code == 0
        assert "not found" in result.output

    @patch("vault_cli.cli.properties.get_client")
    def test_property_set(self, mock_get_client, runner):
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
        assert "Set" in result.output
        client.write_note.assert_called_once()

    @patch("vault_cli.cli.properties.get_client")
    def test_property_remove(self, mock_get_client, runner):
        client = _mock_client()
        mock_get_client.return_value = client
        result = runner.invoke(
            cli, ["property:remove", "--name", "status", "--file", "ClosedClaw"]
        )
        assert result.exit_code == 0
        assert "Removed" in result.output
        client.write_note.assert_called_once()


# ---------------------------------------------------------------------------
# Templates
# ---------------------------------------------------------------------------


class TestTemplates:
    @patch("vault_cli.cli.templates.get_client")
    @patch("vault_cli.cli.templates.get_config")
    def test_templates_list(self, mock_config, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        mock_config.return_value = {"templates_folder": "Templates"}
        result = runner.invoke(cli, ["templates"])
        assert result.exit_code == 0
        assert "People Template" in result.output

    @patch("vault_cli.cli.templates.get_client")
    @patch("vault_cli.cli.templates.get_config")
    def test_template_read(self, mock_config, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        mock_config.return_value = {"templates_folder": "Templates"}
        result = runner.invoke(cli, ["template:read", "--name", "People Template"])
        assert result.exit_code == 0
        assert "{{name}}" in result.output

    @patch("vault_cli.cli.templates.get_client")
    @patch("vault_cli.cli.templates.get_config")
    def test_template_read_json(self, mock_config, mock_get_client, runner):
        mock_get_client.return_value = _mock_client()
        mock_config.return_value = {"templates_folder": "Templates"}
        result = runner.invoke(
            cli, ["template:read", "--name", "People Template", "--json"]
        )
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert "content" in data
        assert "{{name}}" in data["content"]


# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------


class TestConfig:
    @patch("vault_cli.cli.config_cmd.get_config")
    def test_config_show(self, mock_config, runner):
        mock_config.return_value = {
            "vault": {
                "host": "localhost",
                "port": 5984,
                "database": "obsidian",
                "username": "admin",
                "password": "supersecret",
                "protocol": "http",
            },
            "templates_folder": "Templates",
            "output_format": "text",
        }
        result = runner.invoke(cli, ["config", "show"])
        assert result.exit_code == 0
        assert "localhost" in result.output
        # Password should be masked
        assert "supersecret" not in result.output
        assert "****" in result.output
