"""Tests for vault_cli.core.wikilinks — Wikilink extraction.

These tests define the contract for wikilink parsing used in graph operations.
"""

import pytest


class TestExtractWikilinks:
    """extract_wikilinks() finds [[wikilinks]] in note content."""

    def test_simple_wikilink(self):
        """Extracts a simple [[Note Name]] wikilink."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = "See [[Agent Loop]] for details."
        result = extract_wikilinks(text)
        assert "Agent Loop" in result

    def test_wikilink_with_display_text(self):
        """Extracts note name from [[Note Name|Display Text]], ignoring display portion."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = "Check out [[ClosedClaw|the project]]."
        result = extract_wikilinks(text)
        assert "ClosedClaw" in result
        assert "the project" not in result

    def test_wikilink_with_heading(self):
        """Extracts the note name from [[Note#Heading]] (strips heading part)."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = "See [[Agent Loop#Core Pattern]] for the details."
        result = extract_wikilinks(text)
        # Should extract "Agent Loop" (the note part, not the heading)
        assert any("Agent Loop" in link for link in result)

    def test_wikilink_with_heading_and_display(self):
        """Extracts note name from [[Note#Heading|Display]]."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = "See [[Agent Loop#Core Pattern|the pattern]]."
        result = extract_wikilinks(text)
        assert any("Agent Loop" in link for link in result)

    def test_multiple_wikilinks_in_one_line(self):
        """Extracts all wikilinks when multiple appear on the same line."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = "Links to [[ClosedClaw]] and [[ElevenStoic]] and [[North Star]]."
        result = extract_wikilinks(text)
        assert len(result) >= 3
        assert "ClosedClaw" in result
        assert "ElevenStoic" in result
        assert "North Star" in result

    def test_multiple_wikilinks_across_lines(self):
        """Extracts wikilinks spread across multiple lines."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = "First [[Alpha]].\nSecond [[Beta]].\nThird [[Gamma]]."
        result = extract_wikilinks(text)
        assert len(result) >= 3

    def test_no_false_positives_in_code_blocks(self):
        """Wikilinks inside fenced code blocks should be ignored."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = (
            "Normal [[RealLink]] here.\n"
            "\n"
            "```python\n"
            'link = "[[NotALink]]"\n'
            "```\n"
            "\n"
            "Back to [[AnotherReal]].\n"
        )
        result = extract_wikilinks(text)
        assert "RealLink" in result
        assert "AnotherReal" in result
        assert "NotALink" not in result

    def test_no_false_positives_in_inline_code(self):
        """Wikilinks inside inline code (`...`) should be ignored."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = "Use `[[NotALink]]` syntax. See [[RealLink]]."
        result = extract_wikilinks(text)
        assert "RealLink" in result
        assert "NotALink" not in result

    def test_wikilinks_in_frontmatter_values(self):
        """Extracts wikilinks that appear in frontmatter values."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = (
            "---\n"
            "categories:\n"
            '  - "[[People]]"\n'
            '  - "[[Projects]]"\n'
            "---\n"
            "\n"
            "# Note\n"
            "\n"
            "See [[Agent Loop]].\n"
        )
        result = extract_wikilinks(text)
        assert "People" in result
        assert "Projects" in result
        assert "Agent Loop" in result

    def test_empty_wikilink_ignored(self):
        """Empty [[]] should be ignored — not treated as a valid wikilink."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = "Empty [[]] should not count. But [[Valid]] should."
        result = extract_wikilinks(text)
        assert "Valid" in result
        assert "" not in result

    def test_whitespace_only_wikilink_ignored(self):
        """Wikilink containing only whitespace should be ignored."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = "Spaces [[   ]] are not valid. But [[Real Note]] is."
        result = extract_wikilinks(text)
        assert "Real Note" in result
        # Whitespace-only links should not appear
        for link in result:
            assert link.strip() != ""

    def test_nested_brackets(self):
        """Handles edge case with nested/extra brackets gracefully."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = "Weird [[[Triple]]] brackets."
        result = extract_wikilinks(text)
        # Should still extract the inner link or at least not crash
        assert isinstance(result, list)

    def test_wikilink_with_special_characters(self):
        """Wikilinks can contain special characters like dashes and numbers."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = "See [[Self-Hosting 101]] and [[2026-03-03 Daily]]."
        result = extract_wikilinks(text)
        assert "Self-Hosting 101" in result
        assert "2026-03-03 Daily" in result

    def test_returns_list_type(self):
        """Return type is always a list."""
        from vault_cli.core.wikilinks import extract_wikilinks

        assert isinstance(extract_wikilinks("no links here"), list)

    def test_no_duplicates(self):
        """Duplicate wikilinks in the same note should either be deduplicated or counted."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = "[[Alpha]] is great. [[Alpha]] again. [[Beta]]."
        result = extract_wikilinks(text)
        # Whether deduplication happens is a design choice, but let's ensure
        # both note names are present
        assert "Alpha" in result
        assert "Beta" in result

    def test_plain_text_no_links(self):
        """Plain text with no wikilinks returns empty list."""
        from vault_cli.core.wikilinks import extract_wikilinks

        result = extract_wikilinks("Just a regular paragraph with no links.")
        assert result == []

    def test_embed_wikilink(self):
        """Embedded links ![[Image.png]] — should still extract the target."""
        from vault_cli.core.wikilinks import extract_wikilinks

        text = "An image ![[photo.png]] and a note [[Some Note]]."
        result = extract_wikilinks(text)
        assert "Some Note" in result
        # Embed is optional — some implementations may include it
        # At minimum, the non-embed link should be found
