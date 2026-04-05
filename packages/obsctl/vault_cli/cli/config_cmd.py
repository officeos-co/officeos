"""Config and connectivity commands: ping, config show/set."""

import click

from vault_cli.cli.helpers import get_client, get_config, output


@click.command()
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def ping(json_mode):
    """Test CouchDB connectivity."""
    client = get_client()

    try:
        result = client.ping()
        if json_mode:
            output(result, json_mode=True)
        else:
            click.echo("Connected to CouchDB.")
    except ConnectionError as e:
        if json_mode:
            output({"ok": False, "error": str(e)}, json_mode=True)
        else:
            click.echo(f"Connection failed: {e}", err=True)
        raise SystemExit(1)
    except PermissionError as e:
        if json_mode:
            output({"ok": False, "error": str(e)}, json_mode=True)
        else:
            click.echo(f"Auth failed: {e}", err=True)
        raise SystemExit(1)
    except LookupError as e:
        if json_mode:
            output({"ok": False, "error": str(e)}, json_mode=True)
        else:
            click.echo(f"Database not found: {e}", err=True)
        raise SystemExit(1)


@click.group("config")
def config_group():
    """Manage configuration."""
    pass


@config_group.command("show")
@click.option("--json", "json_mode", is_flag=True, help="Output as JSON")
def config_show(json_mode):
    """Show current configuration."""
    config = get_config()

    # Mask password for display
    display_config = _deep_copy_mask(config)

    if json_mode:
        output(display_config, json_mode=True)
    else:
        _print_config(display_config)


@config_group.command("set")
@click.argument("key")
@click.argument("value")
def config_set(key, value):
    """Set a configuration value (e.g. vault.host myserver.com)."""
    from vault_cli.core.config import load_config, save_config, GLOBAL_CONFIG_PATH
    import os
    import json

    # Load existing config file (not merged with defaults)
    config_path = GLOBAL_CONFIG_PATH
    if os.path.exists(config_path):
        with open(config_path) as f:
            file_config = json.load(f)
    else:
        file_config = {}

    # Parse dotted key
    parts = key.split(".")
    target = file_config
    for part in parts[:-1]:
        if part not in target:
            target[part] = {}
        target = target[part]

    # Type coercion for known numeric fields
    final_key = parts[-1]
    if final_key == "port":
        value = int(value)

    target[final_key] = value
    save_config(file_config, config_path)
    click.echo(f"Set {key}={value}")


def _deep_copy_mask(config):
    """Deep copy config, masking password fields."""
    import copy

    result = copy.deepcopy(config)
    if "vault" in result and "password" in result["vault"]:
        pw = result["vault"]["password"]
        if pw:
            result["vault"]["password"] = "****" + pw[-4:] if len(pw) > 4 else "****"
    return result


def _print_config(config, prefix=""):
    """Recursively print config in a readable format."""
    for key, value in config.items():
        full_key = f"{prefix}{key}" if not prefix else f"{prefix}.{key}"
        if isinstance(value, dict):
            _print_config(value, full_key)
        else:
            click.echo(f"{full_key}: {value}")
