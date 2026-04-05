"""Configuration loading for obsidian-vault-cli."""

import json
import os
import copy


DEFAULTS = {
    "vault": {
        "host": "localhost",
        "port": 5984,
        "database": "obsidian",
        "username": "",
        "password": "",
        "protocol": "http",
    },
    "templates_folder": "Templates",
    "output_format": "text",
}

# Default config file paths
GLOBAL_CONFIG_PATH = os.path.expanduser("~/.vault-cli/config.json")
LOCAL_CONFIG_PATH = ".vault-cli.json"


def _deep_merge(base, override):
    """Deep merge override into base. Returns a new dict."""
    result = copy.deepcopy(base)
    for key, value in override.items():
        if key in result and isinstance(result[key], dict) and isinstance(value, dict):
            result[key] = _deep_merge(result[key], value)
        else:
            result[key] = copy.deepcopy(value)
    return result


def _load_json_file(path):
    """Load and parse a JSON file. Raises on invalid JSON."""
    with open(path, "r") as f:
        return json.load(f)


def load_config(path=None, global_path=None, local_path=None):
    """Load configuration from files and environment variables.

    Priority (highest to lowest):
    1. Environment variables
    2. Explicit path (if provided)
    3. Local config (.vault-cli.json)
    4. Global config (~/.vault-cli/config.json)
    5. Defaults

    Args:
        path: explicit config file path
        global_path: override for global config path (for testing)
        local_path: override for local config path (for testing)
    """
    config = copy.deepcopy(DEFAULTS)

    # Load global config
    gp = global_path or GLOBAL_CONFIG_PATH
    if os.path.exists(gp):
        file_config = _load_json_file(gp)
        config = _deep_merge(config, file_config)

    # Load local config (overrides global)
    lp = local_path or LOCAL_CONFIG_PATH
    if os.path.exists(lp):
        file_config = _load_json_file(lp)
        config = _deep_merge(config, file_config)

    # Load explicit path (overrides both)
    if path:
        file_config = _load_json_file(path)
        config = _deep_merge(config, file_config)

    # Environment variable overrides (highest priority)
    env_map = {
        "VAULT_HOST": ("vault", "host"),
        "VAULT_PORT": ("vault", "port"),
        "VAULT_DATABASE": ("vault", "database"),
        "VAULT_USERNAME": ("vault", "username"),
        "VAULT_PASSWORD": ("vault", "password"),
        "VAULT_PROTOCOL": ("vault", "protocol"),
    }

    for env_var, (section, key) in env_map.items():
        value = os.environ.get(env_var)
        if value is not None:
            if key == "port":
                value = int(value)
            config[section][key] = value

    return config


def save_config(config, path=None):
    """Save configuration to a JSON file.

    Args:
        config: configuration dict
        path: file path (defaults to global config path)
    """
    target_path = path or GLOBAL_CONFIG_PATH
    os.makedirs(os.path.dirname(target_path), exist_ok=True)
    with open(target_path, "w") as f:
        json.dump(config, f, indent=2)
