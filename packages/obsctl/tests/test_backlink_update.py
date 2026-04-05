"""Tests for vault_cli.core.backlinks — backlink-aware rename/move orchestration.

These tests define the contract for finding notes that link to a renamed note
and rewriting their wikilinks. Uses a mocked VaultClient.
"""

import pytest
from unittest.mock import MagicMock


# ---------------------------------------------------------------------------
# Helpers — build a mock VaultClient with controllable note storage
# ---------------------------------------------------------------------------


def _make_client(notes_dict):
    """Create a mock VaultClient backed by a dict of {path: content}.

    Supports list_notes, read_note, write_note.
    """
    client = MagicMock()
    store = dict(notes_dict)  # mutable copy

    def _list_notes():
        return [
            {"path": p, "id": p.lower(), "mtime": 0, "size": len(c)}
            for p, c in store.items()
        ]

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

    client.list_notes.side_effect = _list_notes
    client.read_note.side_effect = _read_note
    client.write_note.side_effect = _write_note
    client._store = store  # expose for assertions
    return client


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

from vault_cli.core.backlinks import update_backlinks


class TestUpdateBacklinksBasic:
    """Core functionality: find and rewrite backlinks."""

    def test_simple_rename_updates_backlinks(self):
        """Renaming 'Alpha' to 'Beta' updates [[Alpha]] in other notes."""
        client = _make_client(
            {
                "Alpha.md": "# Alpha\nSelf reference [[Alpha]].",
                "Other.md": "See [[Alpha]] for details.",
                "Unrelated.md": "No links here.",
            }
        )
        result = update_backlinks(client, "Alpha", "Beta")
        assert result["total_links"] == 2
        assert result["total_notes"] == 2
        assert "[[Beta]]" in client._store["Other.md"]
        assert "[[Beta]]" in client._store["Alpha.md"]
        assert "[[Alpha]]" not in client._store["Other.md"]

    def test_no_backlinks_returns_zero(self):
        """If no notes link to the old name, nothing is updated."""
        client = _make_client(
            {
                "Alpha.md": "# Alpha\nContent.",
                "Other.md": "Unrelated stuff.",
            }
        )
        result = update_backlinks(client, "Alpha", "Beta")
        assert result["total_links"] == 0
        assert result["total_notes"] == 0
        # write_note should not be called
        client.write_note.assert_not_called()

    def test_multiple_links_in_one_note(self):
        """A note with multiple references to the old name."""
        client = _make_client(
            {
                "Alpha.md": "# Alpha",
                "Links.md": "[[Alpha]] and again [[Alpha]] here.",
            }
        )
        result = update_backlinks(client, "Alpha", "Beta")
        assert result["total_links"] == 2
        assert result["total_notes"] == 1
        assert client._store["Links.md"] == "[[Beta]] and again [[Beta]] here."


class TestUpdateBacklinksVariants:
    """Wikilink variants: display text, headings, frontmatter."""

    def test_display_text_preserved(self):
        """[[Alpha|my alias]] -> [[Beta|my alias]]."""
        client = _make_client(
            {
                "Alpha.md": "# Alpha",
                "Ref.md": "See [[Alpha|the original]].",
            }
        )
        update_backlinks(client, "Alpha", "Beta")
        assert "[[Beta|the original]]" in client._store["Ref.md"]

    def test_heading_preserved(self):
        """[[Alpha#Section]] -> [[Beta#Section]]."""
        client = _make_client(
            {
                "Alpha.md": "# Alpha\n## Section",
                "Ref.md": "See [[Alpha#Section]].",
            }
        )
        update_backlinks(client, "Alpha", "Beta")
        assert "[[Beta#Section]]" in client._store["Ref.md"]

    def test_heading_and_display_preserved(self):
        """[[Alpha#Section|link text]] -> [[Beta#Section|link text]]."""
        client = _make_client(
            {
                "Alpha.md": "# Alpha\n## Section",
                "Ref.md": "See [[Alpha#Section|read this]].",
            }
        )
        update_backlinks(client, "Alpha", "Beta")
        assert "[[Beta#Section|read this]]" in client._store["Ref.md"]

    def test_frontmatter_wikilinks_updated(self):
        """Wikilinks in frontmatter values are updated."""
        client = _make_client(
            {
                "Alpha.md": "# Alpha",
                "Ref.md": (
                    '---\ncategories:\n  - "[[Alpha]]"\n---\n\nBody with [[Alpha]].\n'
                ),
            }
        )
        update_backlinks(client, "Alpha", "Beta")
        assert '[[Beta]]"' in client._store["Ref.md"]
        assert "Body with [[Beta]]." in client._store["Ref.md"]


class TestUpdateBacklinksCodeExclusion:
    """Code blocks are not modified."""

    def test_code_block_wikilinks_untouched(self):
        """[[Alpha]] inside code blocks stays as-is."""
        client = _make_client(
            {
                "Alpha.md": "# Alpha",
                "Ref.md": (
                    "Normal [[Alpha]] here.\n"
                    "```\n"
                    "[[Alpha]] in code\n"
                    "```\n"
                    "After [[Alpha]] too.\n"
                ),
            }
        )
        result = update_backlinks(client, "Alpha", "Beta")
        content = client._store["Ref.md"]
        assert "Normal [[Beta]] here." in content
        assert "After [[Beta]] too." in content
        assert "[[Alpha]] in code" in content
        # Total links = 2 (code block excluded)
        assert result["total_links"] == 2


class TestUpdateBacklinksCaseSensitivity:
    """Case-insensitive matching."""

    def test_case_insensitive_match(self):
        """[[alpha]] matches 'Alpha' rename."""
        client = _make_client(
            {
                "Alpha.md": "# Alpha",
                "Ref.md": "See [[alpha]] here.",
            }
        )
        update_backlinks(client, "Alpha", "Beta")
        assert "[[Beta]]" in client._store["Ref.md"]


class TestUpdateBacklinksDryRun:
    """Dry-run mode: report changes without writing."""

    def test_dry_run_does_not_write(self):
        """In dry-run mode, no write_note calls are made."""
        client = _make_client(
            {
                "Alpha.md": "# Alpha",
                "Ref.md": "See [[Alpha]].",
            }
        )
        result = update_backlinks(client, "Alpha", "Beta", dry_run=True)
        client.write_note.assert_not_called()
        # But still reports what would change
        assert result["total_links"] > 0
        assert result["total_notes"] > 0

    def test_dry_run_returns_details(self):
        """Dry-run returns per-note details of what would change."""
        client = _make_client(
            {
                "Alpha.md": "# Alpha",
                "One.md": "[[Alpha]] once.",
                "Two.md": "[[Alpha]] and [[Alpha]].",
            }
        )
        result = update_backlinks(client, "Alpha", "Beta", dry_run=True)
        assert result["total_links"] == 3
        assert result["total_notes"] == 2
        # Check details list
        assert len(result["details"]) == 2
        paths = [d["path"] for d in result["details"]]
        assert "One.md" in paths
        assert "Two.md" in paths


class TestUpdateBacklinksReturnValue:
    """Return value structure."""

    def test_return_structure(self):
        """Returns dict with total_links, total_notes, details."""
        client = _make_client(
            {
                "Alpha.md": "# Alpha",
                "Ref.md": "See [[Alpha]] and [[Alpha#Sec]].",
            }
        )
        result = update_backlinks(client, "Alpha", "Beta")
        assert "total_links" in result
        assert "total_notes" in result
        assert "details" in result
        assert isinstance(result["details"], list)

    def test_detail_entries_have_path_and_count(self):
        """Each detail entry has path and count."""
        client = _make_client(
            {
                "Alpha.md": "# Alpha",
                "Ref.md": "[[Alpha]] twice [[Alpha]].",
            }
        )
        result = update_backlinks(client, "Alpha", "Beta")
        detail = result["details"][0]
        assert "path" in detail
        assert "count" in detail
        assert detail["path"] == "Ref.md"
        assert detail["count"] == 2
