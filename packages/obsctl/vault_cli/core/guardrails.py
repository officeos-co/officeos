"""Vault rule guardrails — check notes against vault design conventions.

Rules:
1. Folder placement — warn when target folder does not exist in vault
2. Properties system — warn when note has no `categories` in frontmatter
3. Placement rules  — warn on folder ↔ category mismatch (References)

Each rule returns a Violation with rule_name, message, and reference.
"""

from dataclasses import dataclass

from vault_cli.core.frontmatter import parse_frontmatter


@dataclass
class Violation:
    """A single rule violation detected by check_rules."""

    rule_name: str
    message: str
    reference: str


def check_rules(
    path: str,
    content: str,
    existing_folders: set[str],
) -> list[Violation]:
    """Run all vault rules against a note path and content.

    Args:
        path: Target vault path (e.g. "References/My Note.md").
        content: Full note content including frontmatter.
        existing_folders: Set of folder paths that currently exist in the vault.

    Returns:
        List of Violation objects (empty if no rules are broken).
    """
    violations: list[Violation] = []

    # Determine the target folder (None if root)
    target_folder = path.rsplit("/", 1)[0] if "/" in path else None

    # Parse frontmatter once for rules 2 and 3
    metadata, _ = parse_frontmatter(content)

    # Rule 1: New folder creation
    _check_folder_placement(target_folder, existing_folders, violations)

    # Rule 2: Missing categories
    _check_missing_categories(path, metadata, violations)

    # Rule 3: Folder ↔ category mismatch
    _check_category_mismatch(path, target_folder, metadata, violations)

    return violations


# ---------------------------------------------------------------------------
# Individual rule checks
# ---------------------------------------------------------------------------


def _check_folder_placement(
    target_folder: str | None,
    existing_folders: set[str],
    violations: list[Violation],
) -> None:
    """Rule 1: Warn when target folder does not exist in the vault."""
    if target_folder is None:
        return  # Root path, no folder to check

    if target_folder in existing_folders:
        return  # Folder exists, all good

    # Build a sorted list of existing folders for the message
    folder_list = ", ".join(sorted(existing_folders)) if existing_folders else "(none)"

    violations.append(
        Violation(
            rule_name="Folder placement",
            message=(
                f'Folder "{target_folder}" does not exist in the vault.\n'
                f"  Existing folders: {folder_list}\n"
                f"  \n"
                f"  Creating a new folder is usually a mistake — "
                f"notes go in existing infrastructure folders."
            ),
            reference=(
                'vault design rules → "Folders are for infrastructure, not organization"'
            ),
        )
    )


def _check_missing_categories(
    path: str,
    metadata: dict,
    violations: list[Violation],
) -> None:
    """Rule 2: Warn when note content has no `categories` property."""
    if "categories" in metadata:
        return  # Property exists (even if empty list)

    # Extract note name for the message
    name = path.rsplit("/", 1)[-1] if "/" in path else path
    if name.endswith(".md"):
        name = name[:-3]

    violations.append(
        Violation(
            rule_name="Properties system",
            message=(
                f'Note "{name}" has no `categories` property in frontmatter.\n'
                f"  Every note should have categories for classification."
            ),
            reference='vault design rules → "Properties are for meaning"',
        )
    )


def _check_category_mismatch(
    path: str,
    target_folder: str | None,
    metadata: dict,
    violations: list[Violation],
) -> None:
    """Rule 3: Warn on folder ↔ category mismatch for References."""
    categories = metadata.get("categories")
    if not categories or not isinstance(categories, list):
        return  # No categories to check

    # Extract category names from wikilink format: '[[References]]' → 'References'
    cat_names = set()
    for cat in categories:
        if isinstance(cat, str):
            # Strip wikilink brackets: "[[References]]" → "References"
            stripped = cat.strip()
            if stripped.startswith("[[") and stripped.endswith("]]"):
                cat_names.add(stripped[2:-2])
            else:
                cat_names.add(stripped)

    has_references_cat = "References" in cat_names
    in_references_folder = (
        target_folder is not None and target_folder.split("/")[0] == "References"
    )

    if has_references_cat and not in_references_folder:
        # References category but NOT in References/ folder
        violations.append(
            Violation(
                rule_name="Placement rules",
                message=(
                    'Note is categorized as "[[References]]" but is being '
                    "placed at root.\n"
                    "  Reference notes (external world) belong in References/."
                ),
                reference=(
                    'vault design rules → "Root = your world, '
                    'References = external world"'
                ),
            )
        )
    elif not has_references_cat and in_references_folder:
        # In References/ folder but no References category
        violations.append(
            Violation(
                rule_name="Placement rules",
                message=(
                    "Note is in References/ but categories do not include "
                    '"[[References]]".\n'
                    "  Notes in References/ should be categorized as References."
                ),
                reference=(
                    'vault design rules → "Root = your world, '
                    'References = external world"'
                ),
            )
        )


def get_existing_folders(client) -> set[str]:
    """Dynamically derive existing folders from the vault's current notes.

    Args:
        client: VaultClient instance.

    Returns:
        Set of folder paths found in the vault.
    """
    folders: set[str] = set()
    for note in client.list_notes():
        path = note["path"]
        if "/" in path:
            folders.add(path.rsplit("/", 1)[0])
    return folders


def format_violation(violation: Violation) -> str:
    """Format a violation for CLI output (parseable by agents).

    Output format:
        ⚠ Rule: <rule_name>
          <message>
          See: <reference>
    """
    lines = [f"⚠ Rule: {violation.rule_name}"]
    for line in violation.message.split("\n"):
        lines.append(f"  {line}")
    lines.append(f"  See: {violation.reference}")
    return "\n".join(lines)
