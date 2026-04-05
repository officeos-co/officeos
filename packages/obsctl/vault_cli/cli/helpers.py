"""Shared CLI helpers — client instantiation, output formatting, guardrails."""

import json
import sys

import click

from vault_cli.core.config import load_config
from vault_cli.core.client import VaultClient
from vault_cli.core.guardrails import (
    check_rules,
    format_violation,
    get_existing_folders,
    Violation,
)


def get_client():
    """Load config and return a VaultClient instance."""
    try:
        config = load_config()
    except Exception as e:
        click.echo(f"Config error: {e}", err=True)
        sys.exit(1)

    v = config["vault"]
    return VaultClient(
        host=v["host"],
        port=v["port"],
        database=v["database"],
        username=v["username"],
        password=v["password"],
        protocol=v["protocol"],
    )


def get_config():
    """Load and return config dict."""
    try:
        return load_config()
    except Exception as e:
        click.echo(f"Config error: {e}", err=True)
        sys.exit(1)


def output(data, json_mode=False):
    """Print data to stdout. JSON mode: dumps as JSON. Text mode: plain."""
    if json_mode:
        click.echo(json.dumps(data, indent=2, default=str))
    elif isinstance(data, str):
        click.echo(data)
    elif isinstance(data, list):
        for item in data:
            click.echo(item)
    elif isinstance(data, dict):
        for key, value in data.items():
            click.echo(f"{key}: {value}")
    else:
        click.echo(str(data))


def resolve_file(client, file_name):
    """Resolve a file= parameter to a full vault path.

    Tries:
    1. Exact path match (case-insensitive)
    2. name.md match (case-insensitive)
    3. Basename match across all folders (case-insensitive)

    Returns the path string or exits with error.
    """
    notes = client.list_notes()
    name_lower = file_name.lower()

    # 1. Exact path match
    for note in notes:
        if note["path"].lower() == name_lower:
            return note["path"]

    # 2. name.md match
    name_md = name_lower if name_lower.endswith(".md") else name_lower + ".md"
    for note in notes:
        if note["path"].lower() == name_md:
            return note["path"]

    # 3. Basename match (without extension)
    for note in notes:
        path = note["path"]
        basename = path.rsplit("/", 1)[-1] if "/" in path else path
        basename_no_ext = basename.rsplit(".", 1)[0] if "." in basename else basename
        if basename_no_ext.lower() == name_lower:
            return note["path"]
        if basename.lower() == name_md:
            return note["path"]

    click.echo(f"Note not found: {file_name}", err=True)
    sys.exit(1)


def enforce_guardrails(client, path, content, *, yes=False, strict=False):
    """Run vault rules and enforce them based on mode flags.

    Modes:
        default (yes=False, strict=False): Interactive prompt, y/N
        --yes   (yes=True):  Accept all, log accepted warnings
        --strict (strict=True): Hard fail on any violation (exit 2)

    Args:
        client: VaultClient instance for dynamic folder lookup.
        path: Target note path.
        content: Note content (for frontmatter checks).
        yes: Accept all warnings automatically.
        strict: Treat violations as hard errors.

    Raises:
        SystemExit(2): On rejected/strict rule violation.
    """
    existing_folders = get_existing_folders(client)
    violations = check_rules(path, content, existing_folders)

    if not violations:
        return  # No issues

    # Print all violations to stderr so they don't corrupt structured output
    for v in violations:
        click.echo(format_violation(v), err=True)

    if strict:
        click.echo("\nError: Rule violation in strict mode. Aborting.", err=True)
        raise SystemExit(2)

    if yes:
        click.echo("(accepted via --yes)", err=True)
        return  # Proceed

    # Interactive mode: prompt
    click.echo("")
    confirmed = click.confirm("Proceed anyway?", default=False)
    if not confirmed:
        raise SystemExit(2)
