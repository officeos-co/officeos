"""Tag commands: tags, tag."""

import click

from vault_cli.cli.helpers import get_client, output
from vault_cli.cli.search import _load_index


@click.command()
@click.option("--counts", is_flag=True, help="Show counts per tag")
@click.option(
    "--sort",
    "sort_by",
    default=None,
    type=click.Choice(["count", "name"]),
    help="Sort order",
)
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def tags(counts, sort_by, json_mode):
    """List all tags in the vault."""
    client = get_client()
    index = _load_index(client)

    all_tags = index.get_all_tags()

    if sort_by == "count":
        sorted_tags = sorted(all_tags.items(), key=lambda x: x[1], reverse=True)
    elif sort_by == "name":
        sorted_tags = sorted(all_tags.items(), key=lambda x: x[0])
    else:
        sorted_tags = sorted(all_tags.items(), key=lambda x: x[0])

    if json_mode:
        output(dict(sorted_tags), json_mode=True)
    elif counts:
        for tag_name, count in sorted_tags:
            click.echo(f"{tag_name}: {count}")
    else:
        for tag_name, _ in sorted_tags:
            click.echo(tag_name)


@click.command()
@click.option("--name", required=True, help="Tag name")
@click.option("--verbose", is_flag=True, help="Show file list")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def tag(name, verbose, json_mode):
    """Show notes with a specific tag."""
    client = get_client()
    index = _load_index(client)

    from vault_cli.core.frontmatter import parse_frontmatter

    matching = []
    for path, content in index.notes.items():
        metadata, _ = parse_frontmatter(content)
        note_tags = metadata.get("tags", [])
        if isinstance(note_tags, list) and name in note_tags:
            matching.append(path)

    if json_mode:
        output({"tag": name, "notes": matching, "count": len(matching)}, json_mode=True)
    elif verbose:
        click.echo(f"Tag: {name} ({len(matching)} notes)")
        for path in sorted(matching):
            click.echo(f"  {path}")
    else:
        click.echo(f"{len(matching)} notes with tag '{name}'")
        for path in sorted(matching):
            click.echo(path)
