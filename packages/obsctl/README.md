# obsidian-vault-cli

[![PyPI](https://img.shields.io/pypi/v/obsidian-vault-cli)](https://pypi.org/project/obsidian-vault-cli/)
[![Python 3.10+](https://img.shields.io/badge/python-3.10+-3776AB.svg)](https://pypi.org/project/obsidian-vault-cli/)
[![MIT License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/HarKro753/obsctl/blob/main/LICENSE)

A Python CLI that gives agents (and humans) full access to an Obsidian vault synced via [Self-hosted LiveSync](https://github.com/vrtmrz/obsidian-livesync) (CouchDB) — **without requiring Obsidian to be installed or running**.

Part of the [obsctl](https://github.com/HarKro753/obsctl) monorepo.

## Install

```bash
pip install obsidian-vault-cli
```

## Configure

```bash
vault config set vault.host obsidian.yourhost.com
vault config set vault.port 443
vault config set vault.protocol https
vault config set vault.database obsidian
vault config set vault.username admin
vault config set vault.password yourpassword

vault ping   # verify connection
```

Or use environment variables: `VAULT_HOST`, `VAULT_PORT`, `VAULT_DATABASE`, `VAULT_USERNAME`, `VAULT_PASSWORD`, `VAULT_PROTOCOL`.

## Quick start

```bash
# Read
vault read --file "north star"
vault read --path "References/Person.md" --json

# Create
vault create --name "New Idea" --folder "References" --content "# Idea"

# Write (requires --force to overwrite)
vault write --path "References/Person.md" --content "..." --force
vault write --path "References/Person.md" --content "..." --diff   # preview only

# Search
vault search --query "agent loop" --context
vault search --query "TODO" --limit 10

# Graph traversal
vault backlinks --file "closedclaw"
vault links --file "closedclaw"
vault unresolved
vault orphans

# Tags & properties
vault tags --counts --sort count
vault properties --file "north star"
vault property:set --name "status" --value "active" --file "north star" --yes

# Delete (requires --yes in scripts)
vault delete --file "old note" --yes
```

## Safety flags

All mutating commands support these flags — designed for safe agent operation:

| Flag | Behaviour |
|------|-----------|
| `--force` | Skip existence guard, overwrite unconditionally |
| `--yes` | Accept all vault rule warnings automatically (for scripts/agents) |
| `--strict` | Treat vault rule violations as hard errors (exit code 2) |
| `--dry-run` | Print what would happen, do nothing |
| `--diff` | Show unified diff of content change, do nothing |

- `vault write` **refuses to silently overwrite** existing notes without `--force`
- `vault create` **aborts if the note already exists**
- `vault delete` **prompts for confirmation** (defaults to No)
- `vault property:set` **shows current → new values** before writing

## Vault rule guardrails

`vault create`, `vault write`, and `vault move` enforce vault design rules as interactive guardrails. Violations produce a warning with the rule name, specific violation, and a reference to the vault design rule.

| Rule | Trigger |
|------|---------|
| **Folder placement** | Target folder does not exist in the vault (dynamic check) |
| **Properties system** | Note has no `categories` property in frontmatter |
| **Placement rules** | `[[References]]` category at root, or non-References category in `References/` |

```bash
# Interactive (default) — prompts y/N
vault create --name "My Idea" --folder "Random Stuff" --content "..."

# Agent mode — accept warnings automatically
vault create --name "My Idea" --folder "Random Stuff" --content "..." --yes

# CI/strict mode — hard fail on violations (exit code 2)
vault create --name "My Idea" --folder "Random Stuff" --content "..." --strict
```

Exit codes: `0` = success, `1` = error, `2` = rule violation rejected.

`--force` does NOT bypass rule checks — it only controls overwrite behaviour. `--yes` and `--strict` control rule behaviour.

## All commands

| Command | Description |
|---------|-------------|
| `vault read` | Read note content |
| `vault create` | Create new note |
| `vault write` | Write/overwrite note |
| `vault append` | Append to note |
| `vault prepend` | Prepend to note |
| `vault delete` | Soft-delete note |
| `vault move` | Move note to new path |
| `vault rename` | Rename note (same folder) |
| `vault files` | List files |
| `vault folders` | List folders |
| `vault search` | Full-text search |
| `vault backlinks` | Incoming wikilinks |
| `vault links` | Outgoing wikilinks |
| `vault unresolved` | Dead wikilinks |
| `vault orphans` | Notes with no incoming links |
| `vault tags` | List all tags |
| `vault tag` | Notes with specific tag |
| `vault properties` | Note frontmatter |
| `vault property:read` | Read one property |
| `vault property:set` | Set a property |
| `vault property:remove` | Remove a property |
| `vault templates` | List templates |
| `vault template:read` | Read template |
| `vault ping` | Test connectivity |
| `vault config show` | Show config |
| `vault config set` | Update config |

## Programmatic usage

```python
from vault_cli.core.client import VaultClient

client = VaultClient(
    host="obsidian.yourhost.com",
    port=443,
    database="obsidian",
    username="admin",
    password="yourpassword",
    protocol="https"
)

note = client.read_note("north star.md")
client.write_note("References/New.md", "# New note")
results = client.search_notes("agent loop")
```

## License

MIT
