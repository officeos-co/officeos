"""Tests for vault_cli.core.config — Configuration loading.

Tests cover file loading, environment variable overrides, defaults, and error handling.
"""

import json
import os
import tempfile

import pytest
from unittest.mock import patch, MagicMock


class TestConfigDefaults:
    """Config loader provides sensible defaults when no config file exists."""

    def test_missing_config_uses_defaults(self):
        """When no config file exists, default values are used."""
        from vault_cli.core.config import load_config

        with patch("vault_cli.core.config.os.path.exists", return_value=False):
            config = load_config()

        assert config["vault"]["host"] == "localhost"
        assert config["vault"]["port"] == 5984
        assert config["vault"]["database"] == "obsidian"
        assert config["vault"]["protocol"] == "http"

    def test_default_templates_folder(self):
        """Default templates folder is 'Templates'."""
        from vault_cli.core.config import load_config

        with patch("vault_cli.core.config.os.path.exists", return_value=False):
            config = load_config()

        assert config.get("templates_folder") == "Templates"

    def test_default_output_format(self):
        """Default output format is 'text'."""
        from vault_cli.core.config import load_config

        with patch("vault_cli.core.config.os.path.exists", return_value=False):
            config = load_config()

        assert config.get("output_format") == "text"


class TestConfigFromFile:
    """Config loader reads from JSON config files."""

    def test_load_from_explicit_path(self):
        """Loads config from an explicitly provided path."""
        from vault_cli.core.config import load_config

        config_data = {
            "vault": {
                "host": "obsidian.example.com",
                "port": 443,
                "database": "myvault",
                "username": "user",
                "password": "pass",
                "protocol": "https",
            },
            "templates_folder": "MyTemplates",
        }

        with tempfile.NamedTemporaryFile(mode="w", suffix=".json", delete=False) as f:
            json.dump(config_data, f)
            f.flush()
            temp_path = f.name

        try:
            config = load_config(path=temp_path)
            assert config["vault"]["host"] == "obsidian.example.com"
            assert config["vault"]["port"] == 443
            assert config["vault"]["database"] == "myvault"
            assert config["vault"]["username"] == "user"
            assert config["vault"]["password"] == "pass"
            assert config["vault"]["protocol"] == "https"
            assert config["templates_folder"] == "MyTemplates"
        finally:
            os.unlink(temp_path)

    def test_load_from_default_path(self):
        """Loads config from ~/.vault-cli/config.json if it exists."""
        from vault_cli.core.config import load_config

        config_data = {
            "vault": {
                "host": "default-host.example.com",
                "port": 5984,
                "database": "obsidian",
            }
        }

        with tempfile.NamedTemporaryFile(mode="w", suffix=".json", delete=False) as f:
            json.dump(config_data, f)
            f.flush()
            temp_path = f.name

        try:
            config = load_config(path=temp_path)
            assert config["vault"]["host"] == "default-host.example.com"
        finally:
            os.unlink(temp_path)

    def test_invalid_json_raises_error(self):
        """Invalid JSON in config file raises an error."""
        from vault_cli.core.config import load_config

        with tempfile.NamedTemporaryFile(mode="w", suffix=".json", delete=False) as f:
            f.write("{not valid json!!!")
            f.flush()
            temp_path = f.name

        try:
            with pytest.raises(Exception):
                load_config(path=temp_path)
        finally:
            os.unlink(temp_path)

    def test_partial_config_merged_with_defaults(self):
        """A config file with only some values gets defaults for the rest."""
        from vault_cli.core.config import load_config

        config_data = {
            "vault": {
                "host": "custom.example.com",
            }
        }

        with tempfile.NamedTemporaryFile(mode="w", suffix=".json", delete=False) as f:
            json.dump(config_data, f)
            f.flush()
            temp_path = f.name

        try:
            config = load_config(path=temp_path)
            assert config["vault"]["host"] == "custom.example.com"
            # Other values should fall back to defaults
            assert config["vault"]["port"] == 5984
            assert config["vault"]["database"] == "obsidian"
        finally:
            os.unlink(temp_path)


class TestConfigEnvOverrides:
    """Environment variables override config file values."""

    def test_vault_host_env_override(self):
        """VAULT_HOST env var overrides config file host."""
        from vault_cli.core.config import load_config

        with (
            patch("vault_cli.core.config.os.path.exists", return_value=False),
            patch.dict(os.environ, {"VAULT_HOST": "env-host.example.com"}, clear=False),
        ):
            config = load_config()
            assert config["vault"]["host"] == "env-host.example.com"

    def test_vault_port_env_override(self):
        """VAULT_PORT env var overrides config file port."""
        from vault_cli.core.config import load_config

        with (
            patch("vault_cli.core.config.os.path.exists", return_value=False),
            patch.dict(os.environ, {"VAULT_PORT": "443"}, clear=False),
        ):
            config = load_config()
            assert config["vault"]["port"] == 443

    def test_vault_database_env_override(self):
        """VAULT_DATABASE env var overrides config file database."""
        from vault_cli.core.config import load_config

        with (
            patch("vault_cli.core.config.os.path.exists", return_value=False),
            patch.dict(os.environ, {"VAULT_DATABASE": "custom_db"}, clear=False),
        ):
            config = load_config()
            assert config["vault"]["database"] == "custom_db"

    def test_vault_username_env_override(self):
        """VAULT_USERNAME env var overrides config file username."""
        from vault_cli.core.config import load_config

        with (
            patch("vault_cli.core.config.os.path.exists", return_value=False),
            patch.dict(os.environ, {"VAULT_USERNAME": "env_user"}, clear=False),
        ):
            config = load_config()
            assert config["vault"]["username"] == "env_user"

    def test_vault_password_env_override(self):
        """VAULT_PASSWORD env var overrides config file password."""
        from vault_cli.core.config import load_config

        with (
            patch("vault_cli.core.config.os.path.exists", return_value=False),
            patch.dict(os.environ, {"VAULT_PASSWORD": "env_secret"}, clear=False),
        ):
            config = load_config()
            assert config["vault"]["password"] == "env_secret"

    def test_vault_protocol_env_override(self):
        """VAULT_PROTOCOL env var overrides config file protocol."""
        from vault_cli.core.config import load_config

        with (
            patch("vault_cli.core.config.os.path.exists", return_value=False),
            patch.dict(os.environ, {"VAULT_PROTOCOL": "https"}, clear=False),
        ):
            config = load_config()
            assert config["vault"]["protocol"] == "https"

    def test_env_overrides_file_values(self):
        """Env vars take precedence over config file values."""
        from vault_cli.core.config import load_config

        config_data = {
            "vault": {
                "host": "file-host.example.com",
                "port": 5984,
            }
        }

        with tempfile.NamedTemporaryFile(mode="w", suffix=".json", delete=False) as f:
            json.dump(config_data, f)
            f.flush()
            temp_path = f.name

        try:
            with patch.dict(
                os.environ, {"VAULT_HOST": "env-host.example.com"}, clear=False
            ):
                config = load_config(path=temp_path)
                assert config["vault"]["host"] == "env-host.example.com"
                assert config["vault"]["port"] == 5984  # From file
        finally:
            os.unlink(temp_path)


class TestConfigMerge:
    """Global and local config files are merged correctly."""

    def test_local_overrides_global(self):
        """Local config (.vault-cli.json) overrides global (~/.vault-cli/config.json)."""
        from vault_cli.core.config import load_config

        global_config = {
            "vault": {
                "host": "global-host.example.com",
                "port": 5984,
                "database": "obsidian",
            },
            "templates_folder": "GlobalTemplates",
        }
        local_config = {
            "vault": {
                "host": "local-host.example.com",
            },
            "templates_folder": "LocalTemplates",
        }

        with (
            tempfile.NamedTemporaryFile(mode="w", suffix=".json", delete=False) as gf,
            tempfile.NamedTemporaryFile(mode="w", suffix=".json", delete=False) as lf,
        ):
            json.dump(global_config, gf)
            gf.flush()
            json.dump(local_config, lf)
            lf.flush()
            global_path = gf.name
            local_path = lf.name

        try:
            config = load_config(global_path=global_path, local_path=local_path)
            # Local overrides global for host
            assert config["vault"]["host"] == "local-host.example.com"
            # Local overrides global for templates_folder
            assert config["templates_folder"] == "LocalTemplates"
            # Global provides fallback for port
            assert config["vault"]["port"] == 5984
        finally:
            os.unlink(global_path)
            os.unlink(local_path)
