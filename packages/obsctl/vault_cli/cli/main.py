"""Click CLI entry point for obsidian-vault-cli."""

import click

from vault_cli import __version__

# Import command modules
from vault_cli.cli.crud import (
    read,
    create,
    write_cmd,
    append,
    prepend,
    delete_cmd,
)
from vault_cli.cli.relocate import move, rename
from vault_cli.cli.files import files, folders
from vault_cli.cli.search import search
from vault_cli.cli.graph import backlinks, links, unresolved, orphans
from vault_cli.cli.tags import tags, tag
from vault_cli.cli.properties import (
    properties,
    property_read,
    property_set,
    property_remove,
)
from vault_cli.cli.templates import templates, template_read
from vault_cli.cli.config_cmd import ping, config_group
from vault_cli.cli.recover import recover


@click.group()
@click.version_option(__version__)
def cli():
    """Obsidian vault CLI via CouchDB/LiveSync."""
    pass


# CRUD
cli.add_command(read)
cli.add_command(create)
cli.add_command(write_cmd)
cli.add_command(append)
cli.add_command(prepend)
cli.add_command(delete_cmd)
cli.add_command(move)
cli.add_command(rename)

# Files
cli.add_command(files)
cli.add_command(folders)

# Search
cli.add_command(search)

# Graph
cli.add_command(backlinks)
cli.add_command(links)
cli.add_command(unresolved)
cli.add_command(orphans)

# Tags
cli.add_command(tags)
cli.add_command(tag)

# Properties
cli.add_command(properties)
cli.add_command(property_read)
cli.add_command(property_set)
cli.add_command(property_remove)

# Templates
cli.add_command(templates)
cli.add_command(template_read)

# Config & connectivity
cli.add_command(ping)
cli.add_command(config_group)

# Recovery
cli.add_command(recover)
