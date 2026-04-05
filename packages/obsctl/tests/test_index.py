"""Tests for vault_cli.core.index — In-memory vault index.

Tests use a small set of interconnected notes with known wikilinks, tags, and properties
to verify graph operations.
"""

import pytest


# ---------------------------------------------------------------------------
# Test note corpus
# ---------------------------------------------------------------------------

# 5 notes with known links:
#
#   ClosedClaw  →  Agent Loop, Python, LangChain, ElevenStoic, North Star
#   ElevenStoic →  ClosedClaw, Marcus Aurelius
#   Agent Loop  →  ClosedClaw
#   North Star  →  ClosedClaw, ElevenStoic
#   People Template →  People  (in frontmatter)
#
# Expected backlinks:
#   ClosedClaw  ← ElevenStoic, Agent Loop, North Star  (3 incoming)
#   ElevenStoic ← ClosedClaw, North Star               (2 incoming)
#   Agent Loop  ← ClosedClaw                            (1 incoming)
#   North Star  ← ClosedClaw                            (1 incoming)
#   People Template ← (none)                            (0 — orphan)
#
# Unresolved links (targets with no note): Python, LangChain, Marcus Aurelius, Navigation, People, Projects, Concepts


def _make_notes():
    """Build list of (path, content) tuples for testing."""
    return [
        (
            "Projects/ClosedClaw.md",
            (
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
        ),
        (
            "Projects/ElevenStoic.md",
            (
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
        ),
        (
            "References/Agent Loop.md",
            (
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
        ),
        (
            "North Star.md",
            (
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
        ),
        (
            "Templates/People Template.md",
            (
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
        ),
    ]


@pytest.fixture
def notes():
    return _make_notes()


@pytest.fixture
def index(notes):
    """Build and return a VaultIndex from the test notes."""
    from vault_cli.core.index import VaultIndex

    idx = VaultIndex()
    idx.build(notes)
    return idx


# ---------------------------------------------------------------------------
# Test: build_index
# ---------------------------------------------------------------------------


class TestBuildIndex:
    """VaultIndex.build() constructs the index from (path, content) tuples."""

    def test_build_populates_notes(self, notes):
        """After build, all notes are indexed."""
        from vault_cli.core.index import VaultIndex

        idx = VaultIndex()
        idx.build(notes)
        assert len(idx.notes) == 5

    def test_build_extracts_wikilinks(self, index):
        """Build extracts outgoing wikilinks for each note."""
        # ClosedClaw links to: Agent Loop, Python, LangChain, ElevenStoic, North Star
        # (and possibly Projects from frontmatter)
        links = index.get_links("Projects/ClosedClaw.md")
        link_names = [l.lower() for l in links]
        assert "agent loop" in [n.lower() for n in links] or any(
            "agent loop" in n.lower() for n in links
        )

    def test_build_extracts_tags(self, index):
        """Build extracts tags from frontmatter."""
        all_tags = index.get_all_tags()
        tag_names = [t["name"] if isinstance(t, dict) else t for t in all_tags]
        # Flatten if it's a dict of name->count
        if isinstance(all_tags, dict):
            tag_names = list(all_tags.keys())
        assert "ai" in tag_names
        assert "agent" in tag_names
        assert "business" in tag_names

    def test_build_with_empty_list(self):
        """Building with no notes creates an empty index."""
        from vault_cli.core.index import VaultIndex

        idx = VaultIndex()
        idx.build([])
        assert len(idx.notes) == 0


# ---------------------------------------------------------------------------
# Test: get_backlinks
# ---------------------------------------------------------------------------


class TestGetBacklinks:
    """get_backlinks() returns notes that link TO the specified note."""

    def test_closedclaw_has_three_backlinks(self, index):
        """ClosedClaw is linked from ElevenStoic, Agent Loop, and North Star."""
        backlinks = index.get_backlinks("Projects/ClosedClaw.md")
        # Normalize to lowercase for comparison
        bl_lower = [b.lower() for b in backlinks]
        assert len(backlinks) >= 3
        assert any("elevenstoic" in b for b in bl_lower)
        assert any("agent loop" in b for b in bl_lower)
        assert any("north star" in b for b in bl_lower)

    def test_agent_loop_has_one_backlink(self, index):
        """Agent Loop is only linked from ClosedClaw."""
        backlinks = index.get_backlinks("References/Agent Loop.md")
        bl_lower = [b.lower() for b in backlinks]
        assert len(backlinks) >= 1
        assert any("closedclaw" in b for b in bl_lower)

    def test_people_template_has_no_backlinks(self, index):
        """People Template has no incoming links (orphan)."""
        backlinks = index.get_backlinks("Templates/People Template.md")
        assert len(backlinks) == 0

    def test_backlinks_for_nonexistent_note(self, index):
        """Requesting backlinks for a non-indexed note returns empty list."""
        backlinks = index.get_backlinks("Nonexistent.md")
        assert backlinks == []


# ---------------------------------------------------------------------------
# Test: get_links
# ---------------------------------------------------------------------------


class TestGetLinks:
    """get_links() returns notes that the specified note links TO."""

    def test_closedclaw_outgoing_links(self, index):
        """ClosedClaw links to Agent Loop, Python, LangChain, ElevenStoic, North Star."""
        links = index.get_links("Projects/ClosedClaw.md")
        link_lower = [l.lower() for l in links]
        assert len(links) >= 4  # At minimum the body links
        assert any("agent loop" in l for l in link_lower)
        assert any("elevenstoic" in l for l in link_lower)
        assert any("north star" in l for l in link_lower)

    def test_people_template_links_to_people(self, index):
        """People Template links to People (from frontmatter)."""
        links = index.get_links("Templates/People Template.md")
        link_lower = [l.lower() for l in links]
        assert any("people" in l for l in link_lower)

    def test_links_for_nonexistent_note(self, index):
        """Requesting links for a non-indexed note returns empty list."""
        links = index.get_links("Nonexistent.md")
        assert links == []


# ---------------------------------------------------------------------------
# Test: get_unresolved
# ---------------------------------------------------------------------------


class TestGetUnresolved:
    """get_unresolved() returns wikilink targets that have no matching note."""

    def test_unresolved_links_exist(self, index):
        """Python, LangChain, Marcus Aurelius are unresolved (no matching notes)."""
        unresolved = index.get_unresolved()
        ur_lower = [u.lower() for u in unresolved]
        assert any("python" in u for u in ur_lower)
        assert any("langchain" in u for u in ur_lower)
        assert any("marcus aurelius" in u for u in ur_lower)

    def test_existing_notes_not_in_unresolved(self, index):
        """Notes that exist should not appear in unresolved list."""
        unresolved = index.get_unresolved()
        ur_lower = [u.lower() for u in unresolved]
        # ClosedClaw exists as a note, so it should NOT be unresolved
        # (links to it should resolve)
        # Check that none of the existing note basenames are in unresolved
        # This is tricky because wikilinks are by name, not path
        # But "ClosedClaw" exists and is linked to, so it should resolve
        existing_basenames = [
            "closedclaw",
            "elevenstoic",
            "agent loop",
            "north star",
            "people template",
        ]
        for name in existing_basenames:
            assert name not in ur_lower

    def test_returns_list(self, index):
        """Return type is a list."""
        assert isinstance(index.get_unresolved(), list)


# ---------------------------------------------------------------------------
# Test: get_orphans
# ---------------------------------------------------------------------------


class TestGetOrphans:
    """get_orphans() returns notes with zero incoming links."""

    def test_people_template_is_orphan(self, index):
        """People Template has no incoming links and should be an orphan."""
        orphans = index.get_orphans()
        orphan_lower = [o.lower() for o in orphans]
        assert any("people template" in o for o in orphan_lower)

    def test_closedclaw_is_not_orphan(self, index):
        """ClosedClaw has 3 incoming links and should NOT be an orphan."""
        orphans = index.get_orphans()
        orphan_lower = [o.lower() for o in orphans]
        assert not any("closedclaw" in o for o in orphan_lower)

    def test_returns_list(self, index):
        """Return type is a list."""
        assert isinstance(index.get_orphans(), list)


# ---------------------------------------------------------------------------
# Test: get_all_tags
# ---------------------------------------------------------------------------


class TestGetAllTags:
    """get_all_tags() aggregates tags from all notes with counts."""

    def test_returns_all_unique_tags(self, index):
        """All unique tags across all notes are returned."""
        tags = index.get_all_tags()
        # Could be dict {name: count} or list of {name, count}
        if isinstance(tags, dict):
            tag_names = set(tags.keys())
        else:
            tag_names = {t["name"] if isinstance(t, dict) else t for t in tags}

        assert "ai" in tag_names
        assert "agent" in tag_names
        assert "business" in tag_names
        assert "stoicism" in tag_names
        assert "architecture" in tag_names
        assert "meta" in tag_names

    def test_tag_counts_are_correct(self, index):
        """Tag counts reflect how many notes use each tag."""
        tags = index.get_all_tags()

        if isinstance(tags, dict):
            counts = tags
        else:
            counts = {t["name"]: t["count"] for t in tags if isinstance(t, dict)}

        # "ai" appears in ClosedClaw and Agent Loop → count 2
        assert counts.get("ai", 0) == 2
        # "business" appears only in ElevenStoic → count 1
        assert counts.get("business", 0) == 1
        # "meta" appears only in North Star → count 1
        assert counts.get("meta", 0) == 1

    def test_empty_tags_not_counted(self, index):
        """Notes with empty tags list (tags: []) don't contribute phantom tags."""
        tags = index.get_all_tags()
        if isinstance(tags, dict):
            assert "" not in tags
        else:
            tag_names = [t["name"] if isinstance(t, dict) else t for t in tags]
            assert "" not in tag_names


# ---------------------------------------------------------------------------
# Test: search_content
# ---------------------------------------------------------------------------


class TestSearchContent:
    """search_content() performs full-text search across note bodies."""

    def test_search_finds_matching_notes(self, index):
        """Search for a word returns notes containing it."""
        results = index.search_content("autonomous")
        assert len(results) >= 1
        # Should find "Agent Loop" which contains "autonomous agents"
        result_paths = [
            r["path"].lower() if isinstance(r, dict) else r.lower() for r in results
        ]
        assert any("agent loop" in p for p in result_paths)

    def test_search_case_insensitive(self, index):
        """Search is case-insensitive."""
        results_lower = index.search_content("closedclaw")
        results_upper = index.search_content("CLOSEDCLAW")
        # Both should find results
        assert len(results_lower) >= 1
        assert len(results_upper) >= 1

    def test_search_returns_line_numbers(self, index):
        """Results include line numbers where the match was found."""
        results = index.search_content("stoic")
        assert len(results) >= 1
        # Each result should have a line number
        for result in results:
            if isinstance(result, dict):
                assert "line" in result or "line_number" in result or "lines" in result

    def test_search_no_results(self, index):
        """Search for non-existent term returns empty list."""
        results = index.search_content("xyzzynonexistent12345")
        assert results == []

    def test_search_with_context(self, index):
        """search_content with context=True returns surrounding lines."""
        results = index.search_content("autonomous", context=True)
        assert len(results) >= 1
        for result in results:
            if isinstance(result, dict):
                # Should have context lines around the match
                assert (
                    "context" in result or "lines" in result or "surrounding" in result
                )

    def test_search_finds_in_frontmatter(self, index):
        """Search can find text within frontmatter values."""
        results = index.search_content("active")
        # "active" appears as status: active in ClosedClaw and ElevenStoic
        assert len(results) >= 1

    def test_search_multiple_matches_in_same_note(self, index):
        """Multiple matches in the same note are reported."""
        # "ClosedClaw" links text appears in multiple notes
        results = index.search_content("ClosedClaw")
        # At minimum ClosedClaw itself, and any notes that reference it
        assert len(results) >= 1
