"""Recover command: restore soft-deleted notes from CouchDB."""

import click

from vault_cli.cli.helpers import get_client, output


@click.command()
@click.option("--folder", default=None, help="Only recover notes in this folder (e.g. References)")
@click.option("--dry-run", is_flag=True, help="List what would be recovered without restoring")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def recover(folder, dry_run, json_mode):
    """Recover all soft-deleted notes (or those in --folder)."""
    client = get_client()

    deleted = client.list_deleted(folder=folder)

    if not deleted:
        click.echo("No deleted notes found.")
        return

    if dry_run:
        click.echo(f"Found {len(deleted)} deleted note(s):")
        for note in deleted:
            click.echo(f"  {note['path']}")
        return

    recovered = 0
    failed = 0
    results = []

    for note in deleted:
        try:
            result = client.recover_note(note["id"])
            if result and result.get("recovered"):
                recovered += 1
                results.append(result["path"])
                if not json_mode:
                    click.echo(f"Recovered: {result['path']}")
            elif result and result.get("skipped"):
                if not json_mode:
                    click.echo(f"Skipped (not deleted): {result['path']}")
        except Exception as e:
            failed += 1
            if not json_mode:
                click.echo(f"Failed: {note['path']} — {e}", err=True)

    if json_mode:
        output({"recovered": recovered, "failed": failed, "notes": results}, json_mode=True)
    else:
        click.echo(f"\nDone: {recovered} recovered, {failed} failed")
