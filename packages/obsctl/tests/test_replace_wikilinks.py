"""Tests for replace_wikilinks() — wikilink rewriting engine.

These tests define the contract for replacing [[OldName]] with [[NewName]]
across note content, handling all wikilink variants while respecting code blocks.
"""

import pytest

from vault_cli.core.wikilinks import replace_wikilinks


class TestReplaceSimple:
    """Basic wikilink replacement."""

    def test_simple_replacement(self):
        """[[Old Name]] -> [[New Name]]"""
        text = "See [[Old Name]] for details."
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == "See [[New Name]] for details."

    def test_no_match_returns_unchanged(self):
        """Text without the target wikilink is returned unchanged."""
        text = "See [[Other Note]] for details."
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == text

    def test_multiple_occurrences(self):
        """All occurrences of [[Old Name]] are replaced."""
        text = "First [[Old Name]] then [[Old Name]] again."
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == "First [[New Name]] then [[New Name]] again."
        assert "Old Name" not in result

    def test_multiple_lines(self):
        """Replaces across multiple lines."""
        text = "Line 1 [[Old Name]]\nLine 2 [[Old Name]]\nLine 3."
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == "Line 1 [[New Name]]\nLine 2 [[New Name]]\nLine 3."

    def test_returns_count(self):
        """replace_wikilinks returns (new_text, count)."""
        text = "[[Old Name]] and [[Old Name]] and [[Other]]."
        result, count = replace_wikilinks(
            text, "Old Name", "New Name", return_count=True
        )
        assert count == 2
        assert result == "[[New Name]] and [[New Name]] and [[Other]]."

    def test_zero_count_when_no_match(self):
        """Count is 0 when no replacements made."""
        text = "No links here."
        result, count = replace_wikilinks(
            text, "Old Name", "New Name", return_count=True
        )
        assert count == 0
        assert result == text


class TestReplaceWithDisplayText:
    """Wikilinks with display text: [[Old Name|Display]]."""

    def test_preserves_display_text(self):
        """[[Old Name|My Display]] -> [[New Name|My Display]]"""
        text = "See [[Old Name|my alias]] for more."
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == "See [[New Name|my alias]] for more."

    def test_display_text_with_spaces(self):
        """Display text with spaces is preserved."""
        text = "[[Old Name|The Old Display Text]]"
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == "[[New Name|The Old Display Text]]"


class TestReplaceWithHeadings:
    """Wikilinks with heading anchors: [[Old Name#Heading]]."""

    def test_preserves_heading(self):
        """[[Old Name#Section]] -> [[New Name#Section]]"""
        text = "See [[Old Name#Details]] for info."
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == "See [[New Name#Details]] for info."

    def test_preserves_heading_and_display(self):
        """[[Old Name#Section|Display]] -> [[New Name#Section|Display]]"""
        text = "[[Old Name#Core Pattern|the pattern]]"
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == "[[New Name#Core Pattern|the pattern]]"


class TestReplaceCodeBlockExclusion:
    """Wikilinks inside code blocks must NOT be replaced."""

    def test_fenced_code_block_excluded(self):
        """Wikilinks in fenced code blocks are untouched."""
        text = (
            "Normal [[Old Name]] here.\n"
            "\n"
            "```python\n"
            'link = "[[Old Name]]"\n'
            "```\n"
            "\n"
            "After [[Old Name]] too.\n"
        )
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert "Normal [[New Name]] here." in result
        assert "After [[New Name]] too." in result
        # Inside the code block, the link should NOT be changed
        assert '[[Old Name]]"' in result

    def test_inline_code_excluded(self):
        """Wikilinks in inline code `...` are untouched."""
        text = "Use `[[Old Name]]` syntax. See [[Old Name]]."
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert "`[[Old Name]]`" in result
        assert "See [[New Name]]." in result

    def test_multiple_code_blocks(self):
        """Multiple code blocks are all excluded."""
        text = (
            "[[Old Name]] start.\n"
            "```\n[[Old Name]]\n```\n"
            "Middle [[Old Name]].\n"
            "```\n[[Old Name]]\n```\n"
            "End [[Old Name]].\n"
        )
        result = replace_wikilinks(text, "Old Name", "New Name")
        # Count: 3 outside code blocks should be replaced
        assert result.count("[[New Name]]") == 3
        # 2 inside code blocks should remain
        lines = result.split("\n")
        in_code = False
        old_in_code = 0
        for line in lines:
            if line.startswith("```"):
                in_code = not in_code
            elif in_code and "[[Old Name]]" in line:
                old_in_code += 1
        assert old_in_code == 2


class TestReplaceCaseInsensitive:
    """Case-insensitive matching, case-preserving replacement."""

    def test_case_insensitive_match(self):
        """[[old name]] matches 'Old Name' and is replaced with New Name."""
        text = "See [[old name]] for details."
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == "See [[New Name]] for details."

    def test_mixed_case_match(self):
        """[[OLD NAME]] and [[Old name]] both match."""
        text = "See [[OLD NAME]] and [[Old name]]."
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == "See [[New Name]] and [[New Name]]."

    def test_preserves_new_name_casing(self):
        """The new name's exact casing is always used."""
        text = "[[old note]]"
        result = replace_wikilinks(text, "old note", "My NEW Note")
        assert result == "[[My NEW Note]]"


class TestReplaceFrontmatter:
    """Wikilinks in frontmatter property values should be updated."""

    def test_frontmatter_wikilink(self):
        """Frontmatter value '[[Old Name]]' is updated."""
        text = '---\ncategories:\n  - "[[Old Name]]"\n---\n\nBody with [[Old Name]].\n'
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert '[[New Name]]"' in result
        assert "Body with [[New Name]]." in result

    def test_frontmatter_multiple_values(self):
        """Multiple frontmatter wikilinks are all updated."""
        text = (
            "---\n"
            "categories:\n"
            '  - "[[Old Name]]"\n'
            '  - "[[Other]]"\n'
            "author:\n"
            '  - "[[Old Name]]"\n'
            "---\n"
            "\n"
            "Content.\n"
        )
        result = replace_wikilinks(text, "Old Name", "New Name")
        # Both frontmatter references should be updated
        assert result.count("[[New Name]]") == 2
        assert "[[Other]]" in result


class TestReplaceEmbeds:
    """Embedded wikilinks: ![[Old Name]] and ![[Old Name.ext]]."""

    def test_embed_replaced(self):
        """![[Old Name]] -> ![[New Name]]"""
        text = "Embed: ![[Old Name]]"
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == "Embed: ![[New Name]]"

    def test_embed_with_extension(self):
        """![[Old Name.png]] is NOT replaced when renaming 'Old Name' (different target)."""
        text = "Image: ![[Old Name.png]] and note: [[Old Name]]"
        # Extension-bearing embeds are different targets, only [[Old Name]] should match
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert "[[New Name]]" in result


class TestReplaceSelfLinks:
    """Self-referencing links within the note being renamed."""

    def test_self_reference_updated(self):
        """If the note links to itself, those links are updated too."""
        text = "# Old Name\n\nThis note [[Old Name]] references itself."
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert "[[New Name]]" in result


class TestReplaceEdgeCases:
    """Edge cases and boundary conditions."""

    def test_empty_text(self):
        """Empty string input returns empty string."""
        result = replace_wikilinks("", "Old", "New")
        assert result == ""

    def test_no_wikilinks(self):
        """Text with no wikilinks at all."""
        text = "Just plain text with no links."
        result = replace_wikilinks(text, "Old", "New")
        assert result == text

    def test_adjacent_wikilinks(self):
        """Two wikilinks right next to each other."""
        text = "[[Old Name]][[Other]]"
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == "[[New Name]][[Other]]"

    def test_wikilink_at_start_of_line(self):
        """Wikilink at the very start of text."""
        text = "[[Old Name]] starts the line."
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == "[[New Name]] starts the line."

    def test_wikilink_at_end_of_line(self):
        """Wikilink at the very end of text."""
        text = "Ends with [[Old Name]]"
        result = replace_wikilinks(text, "Old Name", "New Name")
        assert result == "Ends with [[New Name]]"

    def test_special_regex_chars_in_name(self):
        """Names with regex special characters are handled safely."""
        text = "See [[Note (2024)]] here."
        result = replace_wikilinks(text, "Note (2024)", "Note (2025)")
        assert result == "See [[Note (2025)]] here."

    def test_does_not_match_partial_name(self):
        """[[Old]] should NOT match [[Old Name]] or [[Old Name Extra]]."""
        text = "[[Old Name]] and [[Old]] and [[Old Name Extra]]."
        result = replace_wikilinks(text, "Old", "New")
        assert "[[Old Name]]" in result  # unchanged
        assert "[[New]]" in result
        assert "[[Old Name Extra]]" in result  # unchanged
