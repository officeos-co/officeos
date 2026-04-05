"""Move and rename commands with backlink-aware wikilink rewriting."""

import os

import click

from vault_cli.cli.helpers import get_client, output, resolve_file, enforce_guardrails
from vault_cli.core.backlinks import update_backlinks


@click.command()
@click.option("--file", "file_name", required=True, help="Source note name")
@click.option("--to", "to_path", required=True, help="Destination path")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
@click.option("--dry-run", is_flag=True, help="Show what would happen without moving")
@click.option(
    "--no-backlinks", is_flag=True, help="Skip backlink rewriting (legacy behaviour)"
)
@click.option("--yes", is_flag=True, help="Accept vault rule warnings automatically")
@click.option("--strict", is_flag=True, help="Treat rule violations as hard errors")
def move(file_name, to_path, json_mode, dry_run, no_backlinks, yes, strict):
    """Move a note to a new path. Rewrites backlinks if the filename changes."""
    client = get_client()
    path = resolve_file(client, file_name)

    # Read the note content for frontmatter checks
    note = client.read_note(path)
    note_content = note["content"] if note else ""

    # Vault rule guardrails on the destination path
    enforce_guardrails(client, to_path, note_content, yes=yes, strict=strict)

    # Detect if the filename (not just folder) changed
    old_name = _basename_no_ext(path)
    new_name = _basename_no_ext(to_path)
    name_changed = old_name.lower() != new_name.lower()

    if dry_run:
        click.echo(f"Would move: {path} -> {to_path}")
        if name_changed and not no_backlinks:
            bl = update_backlinks(client, old_name, new_name, dry_run=True)
            _echo_backlink_report(bl, dry_run=True)
        return

    result = client.move_note(path, to_path)

    bl_result = None
    if name_changed and not no_backlinks:
        bl_result = update_backlinks(client, old_name, new_name)

    if json_mode:
        data = {"ok": True, "moved": f"{path} -> {to_path}"}
        if bl_result:
            data["backlinks"] = bl_result
        output(data, json_mode=True)
    else:
        click.echo(f"Moved: {path} -> {to_path}")
        if bl_result and bl_result["total_links"] > 0:
            _echo_backlink_report(bl_result)
        elif name_changed and not no_backlinks:
            click.echo("No backlink updates needed.")


@click.command()
@click.option("--file", "file_name", required=True, help="Note name")
@click.option("--name", "new_name", required=True, help="New name")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
@click.option("--dry-run", is_flag=True, help="Show what would happen without renaming")
@click.option(
    "--no-backlinks", is_flag=True, help="Skip backlink rewriting (legacy behaviour)"
)
def rename(file_name, new_name, json_mode, dry_run, no_backlinks):
    """Rename a note (keeps same folder). Rewrites backlinks across the vault."""
    client = get_client()
    path = resolve_file(client, file_name)

    # Keep the same folder, change the filename
    if "/" in path:
        folder = path.rsplit("/", 1)[0]
        new_path = f"{folder}/{new_name}"
    else:
        new_path = new_name

    if not new_path.endswith(".md"):
        new_path += ".md"

    old_name = _basename_no_ext(path)

    if dry_run:
        click.echo(f"Would rename: {path} -> {new_path}")
        if not no_backlinks:
            bl = update_backlinks(client, old_name, new_name, dry_run=True)
            _echo_backlink_report(bl, dry_run=True)
        return

    result = client.move_note(path, new_path)

    bl_result = None
    if not no_backlinks:
        bl_result = update_backlinks(client, old_name, new_name)

    if json_mode:
        data = {"ok": True, "renamed": f"{path} -> {new_path}"}
        if bl_result:
            data["backlinks"] = bl_result
        output(data, json_mode=True)
    else:
        click.echo(f"Renamed: {path} -> {new_path}")
        if bl_result:
            _echo_backlink_report(bl_result)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _basename_no_ext(path):
    """Extract basename without .md extension from a vault path."""
    basename = os.path.basename(path)
    if basename.lower().endswith(".md"):
        return basename[:-3]
    return basename


def _echo_backlink_report(bl_result, dry_run=False):
    """Print a human-readable backlink update report."""
    total = bl_result["total_links"]
    notes = bl_result["total_notes"]
    details = bl_result.get("details", [])

    if total == 0:
        if dry_run:
            click.echo("No backlinks to update.")
        return

    prefix = "Would update" if dry_run else "Updated"
    link_word = "backlink" if total == 1 else "backlinks"
    note_word = "note" if notes == 1 else "notes"
    click.echo(f"{prefix} {total} {link_word} in {notes} {note_word}:")
    for detail in details:
        count = detail["count"]
        link_label = "link" if count == 1 else "links"
        click.echo(f"  - {detail['path']} ({count} {link_label})")
