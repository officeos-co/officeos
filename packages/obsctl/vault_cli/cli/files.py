"""File and folder listing commands."""

import click

from vault_cli.cli.helpers import get_client, output


@click.command()
@click.option("--folder", default=None, help="Filter by folder")
@click.option("--ext", default=None, help="Filter by extension (e.g. md)")
@click.option("--total", is_flag=True, help="Show count only")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def files(folder, ext, total, json_mode):
    """List all files in the vault."""
    client = get_client()
    notes = client.list_notes()

    # Filter by folder
    if folder:
        folder_lower = folder.lower().rstrip("/")
        notes = [n for n in notes if n["path"].lower().startswith(folder_lower + "/")]

    # Filter by extension
    if ext:
        ext_dot = ext if ext.startswith(".") else "." + ext
        notes = [n for n in notes if n["path"].lower().endswith(ext_dot.lower())]

    if total:
        if json_mode:
            output({"total": len(notes)}, json_mode=True)
        else:
            click.echo(str(len(notes)))
    else:
        paths = [n["path"] for n in notes]
        if json_mode:
            output(paths, json_mode=True)
        else:
            for p in sorted(paths):
                click.echo(p)


@click.command()
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def folders(json_mode):
    """List all folders in the vault."""
    client = get_client()
    notes = client.list_notes()

    folder_set = set()
    for note in notes:
        path = note["path"]
        if "/" in path:
            folder = path.rsplit("/", 1)[0]
            folder_set.add(folder)

    folder_list = sorted(folder_set)

    if json_mode:
        output(folder_list, json_mode=True)
    else:
        for f in folder_list:
            click.echo(f)
