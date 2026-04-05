"""Backlink-aware rename/move orchestration.

Scans the entire vault for notes that contain [[old_name]] wikilinks and
rewrites them to [[new_name]], respecting code blocks and all wikilink variants.
"""

from vault_cli.core.wikilinks import replace_wikilinks


def update_backlinks(client, old_name, new_name, dry_run=False):
    """Find and rewrite all wikilinks targeting old_name across the vault.

    Reads every note, checks for [[old_name]] references (case-insensitive),
    and rewrites them to [[new_name]]. Handles all variants:
    [[name]], [[name|display]], [[name#heading]], [[name#heading|display]],
    and frontmatter values containing "[[name]]".

    Args:
        client: VaultClient instance with list_notes/read_note/write_note.
        old_name: Current note name (without .md extension).
        new_name: New note name (without .md extension).
        dry_run: If True, report changes without writing.

    Returns:
        dict with:
            total_links: Total number of wikilink occurrences rewritten.
            total_notes: Number of notes that were modified.
            details: List of {path, count} dicts per modified note.
    """
    all_notes = client.list_notes()
    details = []
    total_links = 0

    for note_info in all_notes:
        path = note_info["path"]
        note = client.read_note(path)
        if note is None:
            continue

        content = note["content"]
        new_content, count = replace_wikilinks(
            content, old_name, new_name, return_count=True
        )

        if count > 0:
            total_links += count
            details.append({"path": path, "count": count})
            if not dry_run:
                client.write_note(path, new_content)

    return {
        "total_links": total_links,
        "total_notes": len(details),
        "details": details,
    }
