"""Search commands."""

import click

from vault_cli.cli.helpers import get_client, output


def _load_index(client):
    """Load all notes and build the in-memory index."""
    from vault_cli.core.index import VaultIndex

    notes_meta = client.list_notes()
    notes = []
    for meta in notes_meta:
        note = client.read_note(meta["path"])
        if note:
            notes.append((note["path"], note["content"]))

    index = VaultIndex()
    index.build(notes)
    return index


@click.command()
@click.option("--query", required=True, help="Search query")
@click.option("--path", "search_path", default=None, help="Restrict to folder")
@click.option("--limit", default=None, type=int, help="Max results")
@click.option("--total", is_flag=True, help="Show count only")
@click.option("--context", is_flag=True, help="Show surrounding lines")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def search(query, search_path, limit, total, context, json_mode):
    """Search note content."""
    client = get_client()
    index = _load_index(client)

    results = index.search_content(query, context=context)

    # Filter by path
    if search_path:
        path_lower = search_path.lower().rstrip("/")
        results = [r for r in results if r["path"].lower().startswith(path_lower + "/")]

    # Total mode
    if total:
        if json_mode:
            output({"total": len(results)}, json_mode=True)
        else:
            click.echo(str(len(results)))
        return

    # Limit
    if limit:
        results = results[:limit]

    if json_mode:
        output(results, json_mode=True)
    else:
        for r in results:
            if context and "context" in r:
                click.echo(f"\n{r['path']}:{r['line_number']}")
                for ctx_line in r["context"]:
                    click.echo(f"  {ctx_line}")
            else:
                click.echo(f"{r['path']}:{r['line_number']}: {r['line'].strip()}")
