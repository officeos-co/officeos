"""Tests for vault_cli.core.frontmatter — Frontmatter parsing and manipulation.

These tests define the contract for frontmatter helpers used throughout the CLI.
"""

import pytest


class TestParseFrontmatter:
    """parse_frontmatter() splits note content into (metadata_dict, body)."""

    def test_note_with_frontmatter(self):
        """Parses a standard note with YAML frontmatter."""
        from vault_cli.core.frontmatter import parse_frontmatter

        content = (
            "---\n"
            "title: Test Note\n"
            "status: active\n"
            "---\n"
            "\n"
            "# Test Note\n"
            "\n"
            "Body content here.\n"
        )
        metadata, body = parse_frontmatter(content)
        assert metadata["title"] == "Test Note"
        assert metadata["status"] == "active"
        assert "# Test Note" in body
        assert "Body content here." in body

    def test_note_without_frontmatter(self):
        """Note without frontmatter returns empty dict and full content as body."""
        from vault_cli.core.frontmatter import parse_frontmatter

        content = "# Just a Heading\n\nSome text.\n"
        metadata, body = parse_frontmatter(content)
        assert metadata == {}
        assert body == content

    def test_frontmatter_with_list_tags(self):
        """Parses frontmatter containing YAML lists (block style)."""
        from vault_cli.core.frontmatter import parse_frontmatter

        content = "---\ntags:\n  - ai\n  - agent\n  - python\n---\n\nBody.\n"
        metadata, body = parse_frontmatter(content)
        assert "tags" in metadata
        assert isinstance(metadata["tags"], list)
        assert "ai" in metadata["tags"]
        assert "agent" in metadata["tags"]
        assert "python" in metadata["tags"]

    def test_frontmatter_with_inline_list(self):
        """Parses frontmatter containing inline YAML lists."""
        from vault_cli.core.frontmatter import parse_frontmatter

        content = "---\ntags: [ai, agent]\n---\n\nBody.\n"
        metadata, body = parse_frontmatter(content)
        assert isinstance(metadata["tags"], list)
        assert "ai" in metadata["tags"]
        assert "agent" in metadata["tags"]

    def test_frontmatter_with_wikilink_values(self):
        """Parses frontmatter values that contain [[wikilinks]]."""
        from vault_cli.core.frontmatter import parse_frontmatter

        content = (
            '---\ncategories:\n  - "[[People]]"\n  - "[[Projects]]"\n---\n\nBody.\n'
        )
        metadata, body = parse_frontmatter(content)
        assert "categories" in metadata
        cats = metadata["categories"]
        assert isinstance(cats, list)
        # The values should contain the wikilink strings
        assert any("[[People]]" in str(c) for c in cats)
        assert any("[[Projects]]" in str(c) for c in cats)

    def test_frontmatter_with_boolean_values(self):
        """Parses boolean values in frontmatter."""
        from vault_cli.core.frontmatter import parse_frontmatter

        content = "---\npublished: true\ndraft: false\n---\n\nBody.\n"
        metadata, body = parse_frontmatter(content)
        assert metadata["published"] is True
        assert metadata["draft"] is False

    def test_frontmatter_with_numeric_values(self):
        """Parses numeric values in frontmatter."""
        from vault_cli.core.frontmatter import parse_frontmatter

        content = "---\nversion: 3\nscore: 9.5\n---\n\nBody.\n"
        metadata, body = parse_frontmatter(content)
        assert metadata["version"] == 3
        assert metadata["score"] == 9.5

    def test_empty_content(self):
        """Empty string returns empty dict and empty body."""
        from vault_cli.core.frontmatter import parse_frontmatter

        metadata, body = parse_frontmatter("")
        assert metadata == {}
        assert body == ""

    def test_only_frontmatter_no_body(self):
        """Note with only frontmatter and no body content."""
        from vault_cli.core.frontmatter import parse_frontmatter

        content = "---\ntitle: Metadata Only\n---\n"
        metadata, body = parse_frontmatter(content)
        assert metadata["title"] == "Metadata Only"
        assert body.strip() == ""

    def test_unclosed_frontmatter(self):
        """Unclosed frontmatter (no closing ---) treats everything as body."""
        from vault_cli.core.frontmatter import parse_frontmatter

        content = "---\ntitle: Broken\nstatus: oops\n\n# No Closing Fence\n"
        metadata, body = parse_frontmatter(content)
        # With unclosed frontmatter, the implementation should either:
        # - Return empty metadata and full content as body, OR
        # - Attempt to parse what it can
        # The safe behavior is to treat it as no frontmatter
        assert isinstance(metadata, dict)
        assert isinstance(body, str)

    def test_frontmatter_with_empty_values(self):
        """Handles frontmatter keys with empty/null values."""
        from vault_cli.core.frontmatter import parse_frontmatter

        content = "---\ntitle:\ntags: []\n---\n\nBody.\n"
        metadata, body = parse_frontmatter(content)
        assert "title" in metadata
        assert "tags" in metadata
        assert (
            metadata["tags"] == [] or metadata["tags"] is None or metadata["tags"] == ""
        )


class TestBuildNote:
    """build_note() combines frontmatter dict and body into valid note content."""

    def test_build_with_frontmatter_and_body(self):
        """Builds a complete note with YAML frontmatter and body."""
        from vault_cli.core.frontmatter import build_note

        metadata = {"title": "Test Note", "status": "active"}
        body = "# Test Note\n\nBody content."
        result = build_note(metadata, body)

        assert result.startswith("---\n")
        assert "title: Test Note" in result or "title: 'Test Note'" in result
        assert "status: active" in result
        assert "---\n" in result
        assert "# Test Note" in result
        assert "Body content." in result

    def test_build_with_empty_frontmatter(self):
        """Building with empty frontmatter produces body only (no YAML block)."""
        from vault_cli.core.frontmatter import build_note

        body = "# Just Body\n\nNo frontmatter."
        result = build_note({}, body)

        assert not result.startswith("---")
        assert result == body or result.strip() == body.strip()

    def test_build_with_list_values(self):
        """Frontmatter lists are serialized correctly."""
        from vault_cli.core.frontmatter import build_note

        metadata = {"tags": ["ai", "agent", "python"]}
        body = "Body."
        result = build_note(metadata, body)

        assert "---" in result
        assert "tags" in result
        # Tags should be in the output, either as inline [ai, agent, python]
        # or block-style with dashes
        assert "ai" in result
        assert "agent" in result
        assert "python" in result

    def test_build_with_wikilink_values(self):
        """Frontmatter wikilink values are preserved."""
        from vault_cli.core.frontmatter import build_note

        metadata = {"categories": ["[[People]]", "[[Projects]]"]}
        body = "Body."
        result = build_note(metadata, body)

        assert "[[People]]" in result
        assert "[[Projects]]" in result

    def test_roundtrip_parse_build(self):
        """Parsing and rebuilding a note preserves key information."""
        from vault_cli.core.frontmatter import parse_frontmatter, build_note

        original = (
            "---\n"
            "title: Roundtrip\n"
            "status: active\n"
            "---\n"
            "\n"
            "# Roundtrip\n"
            "\n"
            "Body content.\n"
        )
        metadata, body = parse_frontmatter(original)
        rebuilt = build_note(metadata, body)

        # Re-parse the rebuilt content
        meta2, body2 = parse_frontmatter(rebuilt)
        assert meta2["title"] == metadata["title"]
        assert meta2["status"] == metadata["status"]
        assert "Body content." in body2

    def test_build_with_none_frontmatter(self):
        """Building with None frontmatter produces body only."""
        from vault_cli.core.frontmatter import build_note

        body = "Just body."
        result = build_note(None, body)
        assert not result.startswith("---")


class TestSetProperty:
    """set_property() adds or updates a property in note content."""

    def test_set_new_property_on_existing_frontmatter(self):
        """Adds a new property to existing frontmatter."""
        from vault_cli.core.frontmatter import set_property

        content = "---\ntitle: Test\n---\n\nBody.\n"
        result = set_property(content, "status", "active")

        from vault_cli.core.frontmatter import parse_frontmatter

        metadata, body = parse_frontmatter(result)
        assert metadata["status"] == "active"
        assert metadata["title"] == "Test"

    def test_update_existing_property(self):
        """Updates an existing property value."""
        from vault_cli.core.frontmatter import set_property

        content = "---\ntitle: Test\nstatus: draft\n---\n\nBody.\n"
        result = set_property(content, "status", "active")

        from vault_cli.core.frontmatter import parse_frontmatter

        metadata, _ = parse_frontmatter(result)
        assert metadata["status"] == "active"

    def test_set_property_on_note_without_frontmatter(self):
        """Creates frontmatter when setting a property on a note without one."""
        from vault_cli.core.frontmatter import set_property

        content = "# No Frontmatter\n\nJust body.\n"
        result = set_property(content, "status", "active")

        from vault_cli.core.frontmatter import parse_frontmatter

        metadata, body = parse_frontmatter(result)
        assert metadata["status"] == "active"
        assert "# No Frontmatter" in body

    def test_set_list_property(self):
        """Can set a property with a list value."""
        from vault_cli.core.frontmatter import set_property

        content = "---\ntitle: Test\n---\n\nBody.\n"
        result = set_property(content, "tags", ["ai", "python"])

        from vault_cli.core.frontmatter import parse_frontmatter

        metadata, _ = parse_frontmatter(result)
        assert isinstance(metadata["tags"], list)
        assert "ai" in metadata["tags"]


class TestRemoveProperty:
    """remove_property() removes a property from note frontmatter."""

    def test_remove_existing_property(self):
        """Removes a property that exists in frontmatter."""
        from vault_cli.core.frontmatter import remove_property

        content = "---\ntitle: Test\nstatus: active\ntags:\n  - ai\n---\n\nBody.\n"
        result = remove_property(content, "status")

        from vault_cli.core.frontmatter import parse_frontmatter

        metadata, body = parse_frontmatter(result)
        assert "status" not in metadata
        assert metadata["title"] == "Test"
        assert "Body." in body

    def test_remove_nonexistent_property(self):
        """Removing a property that doesn't exist is a no-op."""
        from vault_cli.core.frontmatter import remove_property

        content = "---\ntitle: Test\n---\n\nBody.\n"
        result = remove_property(content, "nonexistent")

        from vault_cli.core.frontmatter import parse_frontmatter

        metadata, _ = parse_frontmatter(result)
        assert metadata["title"] == "Test"
        assert "nonexistent" not in metadata

    def test_remove_from_note_without_frontmatter(self):
        """Removing a property from a note without frontmatter is a no-op."""
        from vault_cli.core.frontmatter import remove_property

        content = "# No Frontmatter\n\nJust body.\n"
        result = remove_property(content, "status")
        assert result == content
