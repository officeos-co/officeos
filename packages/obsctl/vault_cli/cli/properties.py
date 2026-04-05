"""Property commands: properties, property:read, property:set, property:remove."""

import click

from vault_cli.cli.helpers import get_client, output, resolve_file
from vault_cli.core.frontmatter import parse_frontmatter, set_property, remove_property


@click.command()
@click.option("--file", "file_name", required=True, help="Note name")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def properties(file_name, json_mode):
    """Show all properties (frontmatter) of a note."""
    client = get_client()
    path = resolve_file(client, file_name)

    note = client.read_note(path)
    if note is None:
        click.echo(f"Note not found: {path}", err=True)
        raise SystemExit(1)

    metadata, _ = parse_frontmatter(note["content"])

    if json_mode:
        output({"file": path, "properties": metadata}, json_mode=True)
    else:
        if not metadata:
            click.echo(f"No properties in: {path}")
        else:
            for key, value in metadata.items():
                click.echo(f"{key}: {value}")


@click.command("property:read")
@click.option("--name", required=True, help="Property name")
@click.option("--file", "file_name", required=True, help="Note name")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def property_read(name, file_name, json_mode):
    """Read a single property from a note."""
    client = get_client()
    path = resolve_file(client, file_name)

    note = client.read_note(path)
    if note is None:
        click.echo(f"Note not found: {path}", err=True)
        raise SystemExit(1)

    metadata, _ = parse_frontmatter(note["content"])
    value = metadata.get(name)

    if json_mode:
        output({"file": path, "property": name, "value": value}, json_mode=True)
    else:
        if value is None:
            click.echo(f"Property '{name}' not found in: {path}")
        else:
            click.echo(str(value))


@click.command("property:set")
@click.option("--name", required=True, help="Property name")
@click.option("--value", required=True, help="Property value")
@click.option("--file", "file_name", required=True, help="Note name")
@click.option(
    "--type",
    "value_type",
    default="text",
    type=click.Choice(["text", "list", "number", "checkbox"]),
    help="Value type",
)
@click.option("--yes", is_flag=True, help="Skip confirmation prompt")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
@click.option("--dry-run", is_flag=True, help="Show what would change without writing")
def property_set(name, value, file_name, value_type, yes, json_mode, dry_run):
    """Set a property on a note."""
    client = get_client()
    path = resolve_file(client, file_name)

    note = client.read_note(path)
    if note is None:
        click.echo(f"Note not found: {path}", err=True)
        raise SystemExit(1)

    # Type coercion
    if value_type == "number":
        try:
            typed_value = float(value) if "." in value else int(value)
        except ValueError:
            click.echo(f"Invalid number: {value}", err=True)
            raise SystemExit(1)
    elif value_type == "checkbox":
        typed_value = value.lower() in ("true", "yes", "1")
    elif value_type == "list":
        # For list type, read existing and append
        metadata, _ = parse_frontmatter(note["content"])
        existing = metadata.get(name, [])
        if isinstance(existing, list):
            if value not in existing:
                existing.append(value)
            typed_value = existing
        else:
            typed_value = [value]
    else:
        typed_value = value

    # Read current value for display
    metadata, _ = parse_frontmatter(note["content"])
    current_value = metadata.get(name)

    if dry_run:
        click.echo(f"Would set {name}:")
        click.echo(f"  Current: {name} = {current_value!r}")
        click.echo(f"  New:     {name} = {typed_value!r}")
        return

    # Show preview and ask for confirmation unless --yes
    if not yes:
        click.echo(f"Current: {name} = {current_value!r}")
        click.echo(f"New:     {name} = {typed_value!r}")
        confirmed = click.confirm("Apply?", default=True)
        if not confirmed:
            click.echo("Aborted.")
            return

    new_content = set_property(note["content"], name, typed_value)
    result = client.write_note(path, new_content)

    if json_mode:
        output(
            {"file": path, "property": name, "value": typed_value, **result},
            json_mode=True,
        )
    else:
        click.echo(f"Set {name}={typed_value} on: {path}")


@click.command("property:remove")
@click.option("--name", required=True, help="Property name")
@click.option("--file", "file_name", required=True, help="Note name")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
@click.option("--dry-run", is_flag=True, help="Show what would change without writing")
def property_remove(name, file_name, json_mode, dry_run):
    """Remove a property from a note."""
    client = get_client()
    path = resolve_file(client, file_name)

    note = client.read_note(path)
    if note is None:
        click.echo(f"Note not found: {path}", err=True)
        raise SystemExit(1)

    if dry_run:
        metadata, _ = parse_frontmatter(note["content"])
        current_value = metadata.get(name)
        click.echo(
            f"Would remove property '{name}' (current value: {current_value!r}) from: {path}"
        )
        return

    new_content = remove_property(note["content"], name)
    result = client.write_note(path, new_content)

    if json_mode:
        output({"file": path, "removed": name, **result}, json_mode=True)
    else:
        click.echo(f"Removed '{name}' from: {path}")
