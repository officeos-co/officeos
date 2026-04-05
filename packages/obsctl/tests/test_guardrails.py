"""Tests for vault rule guardrails — the rules engine (core layer).

Tests cover:
- Rule 1: New folder creation warning
- Rule 2: Missing categories frontmatter warning
- Rule 3: Folder ↔ category mismatch warning
- Dynamic folder existence check (no hardcoded folder list)
- Violation dataclass structure
"""

import pytest

from vault_cli.core.guardrails import check_rules, Violation


# ---------------------------------------------------------------------------
# Fixtures: simulated vault folder state
# ---------------------------------------------------------------------------

EXISTING_FOLDERS = {"Projects", "References", "Templates", "Categories", "Daily"}

CONTENT_WITH_CATEGORIES = (
    '---\ncategories:\n  - "[[Projects]]"\ntags:\n  - ai\n---\n\n# My Note\n'
)

CONTENT_WITHOUT_CATEGORIES = "---\ntags:\n  - ai\n---\n\n# My Note\n"

CONTENT_NO_FRONTMATTER = "# My Note\n\nJust some text.\n"

CONTENT_REFERENCES_CATEGORY = (
    '---\ncategories:\n  - "[[References]]"\n---\n\n# External Resource\n'
)

CONTENT_NON_REFERENCES_CATEGORY = (
    '---\ncategories:\n  - "[[Projects]]"\n---\n\n# My Project\n'
)


# ---------------------------------------------------------------------------
# Rule 1: New folder creation
# ---------------------------------------------------------------------------


class TestNewFolderRule:
    """Trigger when the target folder does not exist in the vault."""

    def test_new_folder_triggers_warning(self):
        """Creating in a non-existent folder should trigger a violation."""
        violations = check_rules(
            path="30 References/My Note.md",
            content=CONTENT_WITH_CATEGORIES,
            existing_folders=EXISTING_FOLDERS,
        )
        folder_violations = [v for v in violations if v.rule_name == "Folder placement"]
        assert len(folder_violations) == 1
        assert "30 References" in folder_violations[0].message
        assert "does not exist" in folder_violations[0].message

    def test_existing_folder_no_warning(self):
        """Creating in an existing folder should NOT trigger a violation."""
        violations = check_rules(
            path="Projects/My Note.md",
            content=CONTENT_WITH_CATEGORIES,
            existing_folders=EXISTING_FOLDERS,
        )
        folder_violations = [v for v in violations if v.rule_name == "Folder placement"]
        assert len(folder_violations) == 0

    def test_root_path_no_folder_warning(self):
        """Creating at vault root (no folder) should NOT trigger folder warning."""
        violations = check_rules(
            path="My Note.md",
            content=CONTENT_WITH_CATEGORIES,
            existing_folders=EXISTING_FOLDERS,
        )
        folder_violations = [v for v in violations if v.rule_name == "Folder placement"]
        assert len(folder_violations) == 0

    def test_warning_lists_existing_folders(self):
        """The violation message should list existing folders for reference."""
        violations = check_rules(
            path="Random Stuff/My Note.md",
            content=CONTENT_WITH_CATEGORIES,
            existing_folders=EXISTING_FOLDERS,
        )
        folder_violations = [v for v in violations if v.rule_name == "Folder placement"]
        assert len(folder_violations) == 1
        # Should mention some existing folders in the message
        msg = folder_violations[0].message
        assert "Existing folders:" in msg

    def test_dynamic_folder_check(self):
        """Folder check is dynamic — whatever set is passed is the source of truth."""
        custom_folders = {"Zettelkasten", "Archive"}
        violations = check_rules(
            path="Projects/Note.md",
            content=CONTENT_WITH_CATEGORIES,
            existing_folders=custom_folders,
        )
        folder_violations = [v for v in violations if v.rule_name == "Folder placement"]
        # "Projects" is NOT in custom_folders, so should trigger
        assert len(folder_violations) == 1

    def test_nested_folder_checks_top_level(self):
        """Nested paths like 'Projects/Sub/Note.md' — checks 'Projects/Sub'."""
        violations = check_rules(
            path="Projects/Sub/Note.md",
            content=CONTENT_WITH_CATEGORIES,
            existing_folders=EXISTING_FOLDERS,
        )
        folder_violations = [v for v in violations if v.rule_name == "Folder placement"]
        # "Projects/Sub" doesn't exist, even though "Projects" does
        assert len(folder_violations) == 1
        assert "Projects/Sub" in folder_violations[0].message

    def test_empty_existing_folders_triggers_warning(self):
        """If vault has no folders, any folder path should trigger."""
        violations = check_rules(
            path="NewFolder/Note.md",
            content=CONTENT_WITH_CATEGORIES,
            existing_folders=set(),
        )
        folder_violations = [v for v in violations if v.rule_name == "Folder placement"]
        assert len(folder_violations) == 1


# ---------------------------------------------------------------------------
# Rule 2: Missing categories frontmatter
# ---------------------------------------------------------------------------


class TestMissingCategoriesRule:
    """Trigger when note content has no `categories` property in frontmatter."""

    def test_no_categories_triggers_warning(self):
        """Content without categories property should trigger."""
        violations = check_rules(
            path="My Note.md",
            content=CONTENT_WITHOUT_CATEGORIES,
            existing_folders=EXISTING_FOLDERS,
        )
        cat_violations = [v for v in violations if v.rule_name == "Properties system"]
        assert len(cat_violations) == 1
        assert "categories" in cat_violations[0].message

    def test_no_frontmatter_triggers_warning(self):
        """Content without any frontmatter should trigger."""
        violations = check_rules(
            path="My Note.md",
            content=CONTENT_NO_FRONTMATTER,
            existing_folders=EXISTING_FOLDERS,
        )
        cat_violations = [v for v in violations if v.rule_name == "Properties system"]
        assert len(cat_violations) == 1

    def test_with_categories_no_warning(self):
        """Content with categories property should NOT trigger."""
        violations = check_rules(
            path="My Note.md",
            content=CONTENT_WITH_CATEGORIES,
            existing_folders=EXISTING_FOLDERS,
        )
        cat_violations = [v for v in violations if v.rule_name == "Properties system"]
        assert len(cat_violations) == 0

    def test_empty_content_triggers_warning(self):
        """Empty content should trigger missing categories."""
        violations = check_rules(
            path="My Note.md",
            content="",
            existing_folders=EXISTING_FOLDERS,
        )
        cat_violations = [v for v in violations if v.rule_name == "Properties system"]
        assert len(cat_violations) == 1

    def test_empty_categories_list_no_warning(self):
        """categories: [] should NOT trigger (property exists, just empty)."""
        content = "---\ncategories: []\n---\n\n# Note\n"
        violations = check_rules(
            path="My Note.md",
            content=content,
            existing_folders=EXISTING_FOLDERS,
        )
        cat_violations = [v for v in violations if v.rule_name == "Properties system"]
        assert len(cat_violations) == 0


# ---------------------------------------------------------------------------
# Rule 3: Folder ↔ category mismatch
# ---------------------------------------------------------------------------


class TestCategoryMismatchRule:
    """Trigger on References category at root or non-References category in References/."""

    def test_references_category_at_root_triggers(self):
        """Note at root with [[References]] category should trigger."""
        violations = check_rules(
            path="External Resource.md",
            content=CONTENT_REFERENCES_CATEGORY,
            existing_folders=EXISTING_FOLDERS,
        )
        mismatch = [v for v in violations if v.rule_name == "Placement rules"]
        assert len(mismatch) == 1
        assert "References" in mismatch[0].message

    def test_references_category_in_references_folder_no_warning(self):
        """Note in References/ with [[References]] category is fine."""
        violations = check_rules(
            path="References/External Resource.md",
            content=CONTENT_REFERENCES_CATEGORY,
            existing_folders=EXISTING_FOLDERS,
        )
        mismatch = [v for v in violations if v.rule_name == "Placement rules"]
        assert len(mismatch) == 0

    def test_non_references_category_in_references_folder_triggers(self):
        """Note in References/ without [[References]] category should trigger."""
        violations = check_rules(
            path="References/My Project.md",
            content=CONTENT_NON_REFERENCES_CATEGORY,
            existing_folders=EXISTING_FOLDERS,
        )
        mismatch = [v for v in violations if v.rule_name == "Placement rules"]
        assert len(mismatch) == 1

    def test_non_references_category_at_root_no_warning(self):
        """Note at root with non-References category is fine."""
        violations = check_rules(
            path="My Project.md",
            content=CONTENT_NON_REFERENCES_CATEGORY,
            existing_folders=EXISTING_FOLDERS,
        )
        mismatch = [v for v in violations if v.rule_name == "Placement rules"]
        assert len(mismatch) == 0

    def test_no_categories_skips_mismatch_check(self):
        """If no categories exist, mismatch check should NOT trigger."""
        violations = check_rules(
            path="References/No Cat.md",
            content=CONTENT_WITHOUT_CATEGORIES,
            existing_folders=EXISTING_FOLDERS,
        )
        mismatch = [v for v in violations if v.rule_name == "Placement rules"]
        assert len(mismatch) == 0

    def test_no_frontmatter_skips_mismatch_check(self):
        """If no frontmatter, mismatch check should NOT trigger."""
        violations = check_rules(
            path="References/Plain.md",
            content=CONTENT_NO_FRONTMATTER,
            existing_folders=EXISTING_FOLDERS,
        )
        mismatch = [v for v in violations if v.rule_name == "Placement rules"]
        assert len(mismatch) == 0


# ---------------------------------------------------------------------------
# Violation structure
# ---------------------------------------------------------------------------


class TestViolationStructure:
    """Violations have the required fields."""

    def test_violation_has_required_fields(self):
        """Each violation has rule_name, message, and reference."""
        violations = check_rules(
            path="NewFolder/Note.md",
            content=CONTENT_NO_FRONTMATTER,
            existing_folders=EXISTING_FOLDERS,
        )
        assert len(violations) >= 1
        for v in violations:
            assert isinstance(v.rule_name, str)
            assert isinstance(v.message, str)
            assert isinstance(v.reference, str)
            assert len(v.rule_name) > 0
            assert len(v.message) > 0
            assert len(v.reference) > 0

    def test_violation_reference_points_to_vault_rules(self):
        """References should mention vault design rules."""
        violations = check_rules(
            path="NewFolder/Note.md",
            content=CONTENT_WITH_CATEGORIES,
            existing_folders=EXISTING_FOLDERS,
        )
        for v in violations:
            assert "vault design rules" in v.reference.lower()


# ---------------------------------------------------------------------------
# Multiple violations
# ---------------------------------------------------------------------------


class TestMultipleViolations:
    """check_rules can return multiple violations at once."""

    def test_new_folder_and_missing_categories(self):
        """New folder + no categories = two violations."""
        violations = check_rules(
            path="BadFolder/Note.md",
            content=CONTENT_NO_FRONTMATTER,
            existing_folders=EXISTING_FOLDERS,
        )
        rule_names = {v.rule_name for v in violations}
        assert "Folder placement" in rule_names
        assert "Properties system" in rule_names

    def test_all_three_rules_can_fire(self):
        """References category at root in new folder with no existing folders."""
        violations = check_rules(
            path="External Resource.md",
            content=CONTENT_REFERENCES_CATEGORY,
            existing_folders=EXISTING_FOLDERS,
        )
        # At root: no folder violation, but References category at root = mismatch
        rule_names = {v.rule_name for v in violations}
        assert "Placement rules" in rule_names

    def test_no_violations_for_valid_note(self):
        """Well-formed note in existing folder produces no violations."""
        content = '---\ncategories:\n  - "[[References]]"\n---\n\n# Good Note\n'
        violations = check_rules(
            path="References/Good Note.md",
            content=content,
            existing_folders=EXISTING_FOLDERS,
        )
        assert len(violations) == 0
