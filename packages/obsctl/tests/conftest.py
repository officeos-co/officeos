"""Shared test fixtures for obsidian-vault-cli tests."""

import pytest
from unittest.mock import MagicMock, patch
import hashlib
import json


# ---------------------------------------------------------------------------
# Sample note content used across multiple test modules
# ---------------------------------------------------------------------------

SAMPLE_NOTES = [
    {
        "path": "Projects/ClosedClaw.md",
        "content": (
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
        ),
    },
    {
        "path": "Projects/ElevenStoic.md",
        "content": (
            "---\n"
            "categories:\n"
            '  - "[[Projects]]"\n'
            "tags:\n"
            "  - business\n"
            "  - stoicism\n"
            "status: active\n"
            "---\n"
            "\n"
            "# ElevenStoic\n"
            "\n"
            "A stoic philosophy brand. See [[ClosedClaw]] for the tech side.\n"
            "Inspired by [[Marcus Aurelius]].\n"
        ),
    },
    {
        "path": "References/Agent Loop.md",
        "content": (
            "---\n"
            "categories:\n"
            '  - "[[Concepts]]"\n'
            "tags:\n"
            "  - ai\n"
            "  - architecture\n"
            "---\n"
            "\n"
            "# Agent Loop\n"
            "\n"
            "The core pattern for autonomous agents:\n"
            "1. Observe\n"
            "2. Think\n"
            "3. Act\n"
            "\n"
            "Used in [[ClosedClaw]].\n"
        ),
    },
    {
        "path": "North Star.md",
        "content": (
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
        ),
    },
    {
        "path": "Templates/People Template.md",
        "content": (
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
        ),
    },
]


def _chunk_id(data: str) -> str:
    """Generate a LiveSync chunk ID: h: + first 12 chars of SHA-256."""
    h = hashlib.sha256(data.encode("utf-8")).hexdigest()
    return f"h:{h[:12]}"


def _build_couch_rows(notes: list[dict]) -> list[dict]:
    """Build mock CouchDB _all_docs rows from sample notes."""
    rows = []
    for note in notes:
        doc_id = note["path"].lower()
        chunk_id = _chunk_id(note["content"])
        rows.append(
            {
                "id": doc_id,
                "key": doc_id,
                "doc": {
                    "_id": doc_id,
                    "_rev": "1-abc123",
                    "path": note["path"],
                    "children": [chunk_id],
                    "ctime": 1709500000000,
                    "mtime": 1709500000000,
                    "size": len(note["content"].encode("utf-8")),
                    "type": "plain",
                    "eden": {},
                },
            }
        )
    # Add chunk rows (these should be filtered out by list_notes)
    for note in notes:
        chunk_id = _chunk_id(note["content"])
        rows.append(
            {
                "id": chunk_id,
                "key": chunk_id,
                "doc": {
                    "_id": chunk_id,
                    "type": "leaf",
                    "data": note["content"],
                },
            }
        )
    # Add system doc
    rows.append(
        {
            "id": "_design/test",
            "key": "_design/test",
            "doc": {"_id": "_design/test", "views": {}},
        }
    )
    # Add a deleted doc
    rows.append(
        {
            "id": "old/deleted-note.md",
            "key": "old/deleted-note.md",
            "doc": {
                "_id": "old/deleted-note.md",
                "_rev": "2-def456",
                "path": "Old/Deleted Note.md",
                "children": [],
                "deleted": True,
                "ctime": 1709400000000,
                "mtime": 1709400000000,
                "size": 0,
                "type": "plain",
            },
        }
    )
    # Add LiveSync version doc (should be filtered)
    rows.append(
        {
            "id": "obsydian_livesync_version",
            "key": "obsydian_livesync_version",
            "doc": {
                "_id": "obsydian_livesync_version",
                "version": 1,
            },
        }
    )
    return rows


@pytest.fixture
def sample_notes():
    """List of sample notes with known content, wikilinks, tags, and frontmatter."""
    return SAMPLE_NOTES


@pytest.fixture
def sample_couch_rows():
    """Mock CouchDB _all_docs rows including chunks, system docs, and deleted docs."""
    return _build_couch_rows(SAMPLE_NOTES)


@pytest.fixture
def mock_vault_client():
    """
    A VaultClient instance with mocked HTTP layer.

    Returns a tuple of (client, mock_session) where mock_session is the
    mocked requests.Session so tests can configure responses.
    """
    with patch("vault_cli.core.client.requests") as mock_requests:
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        from vault_cli.core.client import VaultClient

        client = VaultClient(
            host="localhost",
            port=5984,
            database="obsidian",
            username="admin",
            password="secret",
            protocol="http",
        )
        yield client, mock_session


@pytest.fixture
def cli_runner():
    """Click CliRunner instance for testing CLI commands."""
    from click.testing import CliRunner

    return CliRunner()
