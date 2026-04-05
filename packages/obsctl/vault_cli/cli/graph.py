"""Graph traversal commands: backlinks, links, unresolved, orphans."""

import click

from vault_cli.cli.helpers import get_client, output
from vault_cli.cli.search import _load_index


@click.command()
@click.option("--file", "file_name", required=True, help="Note name")
@click.option("--counts", is_flag=True, help="Show count only")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def backlinks(file_name, counts, json_mode):
    """Show notes that link TO this note (incoming links)."""
    client = get_client()
    index = _load_index(client)

    # Resolve to a path for backlink lookup
    from vault_cli.cli.helpers import resolve_file

    path = resolve_file(client, file_name)

    results = index.get_backlinks(path)

    if counts:
        if json_mode:
            output({"file": path, "count": len(results)}, json_mode=True)
        else:
            click.echo(str(len(results)))
    elif json_mode:
        output({"file": path, "backlinks": results}, json_mode=True)
    else:
        if not results:
            click.echo(f"No backlinks for: {path}")
        else:
            for r in sorted(results):
                click.echo(r)


@click.command()
@click.option("--file", "file_name", required=True, help="Note name")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def links(file_name, json_mode):
    """Show outgoing links FROM this note."""
    client = get_client()
    index = _load_index(client)

    from vault_cli.cli.helpers import resolve_file

    path = resolve_file(client, file_name)

    results = index.get_links(path)

    if json_mode:
        output({"file": path, "links": results}, json_mode=True)
    else:
        if not results:
            click.echo(f"No outgoing links in: {path}")
        else:
            for r in results:
                click.echo(r)


@click.command()
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def unresolved(json_mode):
    """Show wikilinks pointing to notes that don't exist."""
    client = get_client()
    index = _load_index(client)

    results = index.get_unresolved()

    if json_mode:
        output({"unresolved": results, "count": len(results)}, json_mode=True)
    else:
        if not results:
            click.echo("No unresolved links.")
        else:
            click.echo(f"{len(results)} unresolved links:")
            for r in results:
                click.echo(f"  [[{r}]]")


@click.command()
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def orphans(json_mode):
    """Show notes with zero incoming links."""
    client = get_client()
    index = _load_index(client)

    results = index.get_orphans()

    if json_mode:
        output({"orphans": results, "count": len(results)}, json_mode=True)
    else:
        if not results:
            click.echo("No orphan notes.")
        else:
            click.echo(f"{len(results)} orphan notes:")
            for r in sorted(results):
                click.echo(f"  {r}")
