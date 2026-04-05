"""Template commands: templates, template:read."""

import click

from vault_cli.cli.helpers import get_client, get_config, output


@click.command()
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def templates(json_mode):
    """List available templates."""
    client = get_client()
    config = get_config()
    templates_folder = config.get("templates_folder", "Templates")

    notes = client.list_notes()
    template_notes = [
        n["path"]
        for n in notes
        if n["path"].lower().startswith(templates_folder.lower() + "/")
        and n["path"].lower().endswith(".md")
    ]

    if json_mode:
        output(template_notes, json_mode=True)
    else:
        if not template_notes:
            click.echo(f"No templates found in {templates_folder}/")
        else:
            for t in sorted(template_notes):
                # Show just the template name without the folder
                name = t.split("/", 1)[1] if "/" in t else t
                click.echo(name)


@click.command("template:read")
@click.option("--name", required=True, help="Template name")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def template_read(name, json_mode):
    """Read a template's content."""
    client = get_client()
    config = get_config()
    templates_folder = config.get("templates_folder", "Templates")

    template_name = name if name.endswith(".md") else name + ".md"
    path = f"{templates_folder}/{template_name}"

    note = client.read_note(path)
    if note is None:
        click.echo(f"Template not found: {path}", err=True)
        raise SystemExit(1)

    if json_mode:
        output({"name": name, "path": path, "content": note["content"]}, json_mode=True)
    else:
        click.echo(note["content"])
