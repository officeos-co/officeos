# obsidian-vault-cli — Product Spec v2.0

**Status:** Draft
**Date:** 2026-03-03
**Authors:** Harro Krog

---

## Vision

A Python CLI that gives agents (and humans) full Obsidian CLI-equivalent access to an Obsidian vault synced via Self-hosted LiveSync (CouchDB) — **without requiring Obsidian to be installed or running**.

The Obsidian CLI (v1.12+) requires a running Obsidian desktop app with a Catalyst license. This tool replaces it entirely by talking directly to the CouchDB database that LiveSync uses for sync.

**No AI. No curation. No inbox processing.** This is a pure vault access tool: read, write, search, traverse the knowledge graph, manage properties and tags. The kind of foundation an agent needs to operate on a vault as a knowledge graph.

---

## Problem

When an AI agent (OpenClaw, Claude Code, etc.) needs to interact with an Obsidian vault synced via LiveSync, it currently has to:

1. **Read notes** via raw `curl` calls with URL-encoded paths and HTTP basic auth
2. **Write notes** by generating temporary `.mjs` scripts, importing a Node.js VaultClient class as an ESM module, running them with `node`, then verifying via separate `curl` calls
3. **Search** — not possible (only path matching via `_all_docs`)
4. **Backlinks / links / tags / properties** — not possible at all

This tool replaces all of that with simple CLI commands:

```bash
vault read file="north star"
vault create name="New Idea" content="..." folder="References"
vault search query="agent loop" --context
vault backlinks file="closedclaw"
vault tags --counts --sort=count
vault property:set name="status" value="active" file="self-hosting"
vault links file="elevenstoic"
vault templates
```

---

## Package Identity

- **Language:** Python 3.10+
- **CLI name:** `vault` (via `pyproject.toml` entry point)
- **Package:** `obsidian-vault-cli`
- **Repository:** `github.com/HarKro753/obsidian-curator-real` (to be renamed)
- **License:** MIT

---

## Architecture

Two layers:

```
┌──────────────────────────────────────────────────┐
│  CLI                                              │  vault read / write / search / backlinks / tags
│                                                   │  Mirrors Obsidian CLI syntax (key=value params)
├──────────────────────────────────────────────────┤
│  VaultClient (Python)                             │  CouchDB CRUD, LiveSync document format,
│                                                   │  in-memory graph index, frontmatter parsing
└──────────────────────────────────────────────────┘
         │
         ▼
   CouchDB / LiveSync
```

### VaultClient (Core Library)

The programmatic API. Handles all CouchDB interaction and LiveSync document format details.

```python
from vault_cli import VaultClient

client = VaultClient(
    host="obsidian.harrokrog.com",
    port=443,
    database="obsidian",
    username="admin",
    password="changeme",
    protocol="https"
)

# CRUD
note = client.read_note("north star.md")
client.write_note("References/New Person.md", content)
client.delete_note("old-note.md")
client.move_note("draft.md", "References/final.md")

# Search
results = client.search_content("agent loop")
results = client.search_content("agent loop", path="References")

# Graph
backlinks = client.get_backlinks("closedclaw.md")
links = client.get_links("closedclaw.md")
orphans = client.get_orphans()
unresolved = client.get_unresolved()

# Tags & Properties
tags = client.get_all_tags()
props = client.get_properties("north star.md")
client.set_property("north star.md", "status", "active")
```

### CLI

Wraps the VaultClient for terminal use. Mirrors Obsidian CLI syntax where possible.

```bash
vault read file="north star"
vault create name="New Idea" content="# Idea" folder="References"
vault write path="References/Person.md" content="---\n..."
vault append file="north star" content="\n## New section"
vault prepend file="north star" content="**Updated 2026-03-03**"
vault delete file="old note"
vault move file="draft" to="References/final.md"
vault rename file="draft" name="Final Version"

vault files
vault files folder="References"
vault files ext=md
vault files --total
vault folders

vault search query="agent loop"
vault search query="TODO" path="References" limit=5
vault search query="agent" --context
vault search query="agent" --total

vault backlinks file="closedclaw"
vault backlinks file="closedclaw" --counts
vault links file="closedclaw"
vault unresolved
vault orphans

vault tags
vault tags --counts
vault tags --sort=count
vault tag name="ai" --verbose

vault properties file="north star"
vault property:read name="status" file="north star"
vault property:set name="status" value="active" file="north star"
vault property:set name="tags" value="ai" type=list file="north star"
vault property:remove name="status" file="north star"

vault templates
vault template:read name="people template"

vault ping                    # test CouchDB connectivity
vault config show             # show current config
vault config set vault.host obsidian.harrokrog.com
```

---

## Configuration

Config file at `~/.vault-cli/config.json` (global) or `.vault-cli.json` (project-local). Local overrides global.

```json
{
  "vault": {
    "host": "localhost",
    "port": 5984,
    "database": "obsidian",
    "username": "",
    "password": "",
    "protocol": "http"
  },
  "templates_folder": "Templates",
  "output_format": "text"
}
```

That's it. No AI config, no structure presets, no tidy rules. Just the CouchDB connection and a couple of preferences.

### Environment Variable Override

Every config value can be set via env var:

```bash
export VAULT_HOST=obsidian.harrokrog.com
export VAULT_PORT=443
export VAULT_DATABASE=obsidian
export VAULT_USERNAME=admin
export VAULT_PASSWORD=changeme
export VAULT_PROTOCOL=https
```

Env vars take precedence over config file.

---

## LiveSync Document Format

LiveSync stores notes in CouchDB as two document types:

### Metadata Document

`_id` = note path (lowercased). Contains note metadata and references to content chunks.

```json
{
  "_id": "references/some person.md",
  "_rev": "1-abc123",
  "path": "References/Some Person.md",
  "children": ["h:a1b2c3d4e5f6"],
  "ctime": 1709500000000,
  "mtime": 1709500000000,
  "size": 1234,
  "type": "plain",
  "eden": {}
}
```

### Leaf/Chunk Document

`_id` = `h:` + first 12 chars of SHA-256 hash of content. Contains the actual note content. Content-addressed and shared across notes.

```json
{
  "_id": "h:a1b2c3d4e5f6",
  "type": "leaf",
  "data": "---\ncategories:\n  - \"[[People]]\"\n---\n\n# Some Person\n..."
}
```

Large notes are split into multiple chunks (50KB each). The metadata document's `children` array lists them in order.

### Deletion

Soft-delete: set `deleted: true`, clear `children`, clear `data`. Do NOT use CouchDB `db.destroy()`.

### ⚠️ Known CouchDB/LiveSync Footgun

If a parent doc has `deleted: true` and a write comes in without removing that flag, LiveSync treats it as deleted. Always check for `deleted: true` on any doc before writing to it and strip the flag explicitly.

---

## Safety & Error Handling (v0.2.0)

Agent-operated tools must fail loudly and prevent data loss by default. These rules apply to all destructive commands.

### Write safety (`vault write`)

`vault write` **will not silently overwrite** an existing note without explicit opt-in.

**Default behaviour (no flags):**
```
$ vault write --path "References/Dijkstra Algorithm.md" --content "..."
Note already exists (2,341 chars, last modified 2026-03-03).
Use --force to overwrite, or --diff to preview changes.
Aborted.
```

**`--force` flag:** Skip the guard, overwrite unconditionally. Use in scripts.

**`--diff` flag:** Print a unified diff of what would change, then exit. No write.

**`deleted: true` detection:** Before writing, if the existing doc has `deleted: true`, print:
```
Warning: note exists but is marked deleted in CouchDB.
Writing will restore it. Continue? [y/N]
```

### Create safety (`vault create`)

If a note with the same path already exists:
```
Note already exists: References/Dijkstra Algorithm.md
Use `vault write --path "..." --force` to overwrite.
Aborted.
```

### Delete safety (`vault delete`)

Delete requires explicit confirmation.

**Interactive (TTY):** Prompts `Delete "note name"? [y/N]` — defaults to No.

**Non-interactive (piped/scripted):** Requires `--yes` flag explicitly.

```bash
vault delete --file "old note" --yes    # scripted, no prompt
vault delete --file "old note"          # prompts in TTY, errors in scripts
```

### Property safety (`vault property:set`)

Before writing, reads the current value and shows what will change:
```
$ vault property:set --name tags --value math --file "Mathe I"
Current: tags = ['evergreen', 'engineering']
New:     tags = ['evergreen', 'engineering', 'math']
Apply? [Y/n]
```

Add `--yes` to skip the prompt in scripts.

### Error messages

All HTTP errors include:
- The operation attempted (`write`, `delete`, `property:set`)
- The note path
- The CouchDB error body (not just the status code)
- A human hint where applicable

**Examples:**

```
Error writing "References/Mathe I.md":
  CouchDB 409 Conflict — document update conflict.
  Hint: The doc was modified externally. Re-read and retry.

Error connecting to CouchDB at https://obsidian.harrokrog.com:
  Connection refused. Is the server running?
  Check: vault ping

Error reading "References/Unknown.md":
  Note not found. Use `vault files` to browse, or `vault search query="Unknown"` to find it.
```

### `--dry-run` global flag

Any mutating command accepts `--dry-run`: prints what would happen without doing it. Safe for agents to validate before committing.

```bash
vault write --path "X.md" --content "..." --dry-run
# → Would write 1,234 chars to X.md (currently 980 chars)

vault delete --file "old note" --dry-run
# → Would soft-delete: old note.md
```

### Vault Rule Guardrails (v0.2.2)

`vault create`, `vault write`, and `vault move` enforce vault design rules as interactive guardrails before any mutation. The folder existence check is dynamic — queries the vault, no hardcoded folder list.

**Three rules:**

| Rule | Trigger |
|------|---------|
| **Folder placement** | Target folder does not exist in the vault |
| **Properties system** | Note has no `categories` property in frontmatter |
| **Placement rules** | `[[References]]` category at root, or non-References category in `References/` |

**Three modes:**

| Flag | Behaviour |
|------|-----------|
| _(default)_ | Interactive prompt with rule explanation, `y/N` (defaults No) |
| `--yes` | Accept all warnings, log them, proceed |
| `--strict` | Hard fail on any violation (exit code 2, no prompt) |

**Output format (parseable by agents):**
```
⚠ Rule: Folder placement
  Folder "30 References" does not exist in the vault.
  Existing folders: Categories, Daily, Projects, References, Templates
  
  Creating a new folder is usually a mistake — notes go in existing infrastructure folders.
  See: vault design rules → "Folders are for infrastructure, not organization"
```

**Exit codes:** 0 = success, 1 = error, 2 = rule violation rejected.

**`--force` vs `--yes`:** `--force` means "overwrite existing content". `--yes` means "I know I'm breaking a vault rule". These are separate concerns and cannot substitute for each other.

**Implementation:** `vault_cli/core/guardrails.py` provides `check_rules(path, content, existing_folders)` which returns a list of `Violation` objects. `vault_cli/cli/helpers.py` provides `enforce_guardrails(client, path, content, yes=, strict=)` which handles the prompt/abort/accept logic.

---

## Feature Breakdown

### Tier 1: Core CRUD (must have)

| Command | Description | Maps to Obsidian CLI |
|---------|-------------|---------------------|
| `vault read file="X"` | Read note content | `obsidian read file="X"` |
| `vault read path="X"` | Read by exact path | `obsidian read path="X"` |
| `vault create name="X" content="Y"` | Create new note | `obsidian create name="X" content="Y"` |
| `vault create name="X" template="Y"` | Create from template | `obsidian create name="X" template="Y"` |
| `vault write path="X" content="Y"` | Write/overwrite note | — |
| `vault append file="X" content="Y"` | Append to note | `obsidian append` |
| `vault prepend file="X" content="Y"` | Prepend to note | `obsidian prepend` |
| `vault delete file="X"` | Soft-delete | `obsidian delete` |
| `vault move file="X" to="Y"` | Move/rename path | `obsidian move` |
| `vault rename file="X" name="Y"` | Rename (keep folder) | `obsidian rename` |
| `vault files` | List all files | `obsidian files` |
| `vault files folder="X"` | List in folder | `obsidian files folder="X"` |
| `vault files --total` | Count files | `obsidian files total` |
| `vault folders` | List all folders | `obsidian folders` |
| `vault search query="X"` | Content search | `obsidian search` |
| `vault search query="X" --context` | Search with line context | `obsidian search:context` |

### Tier 2: Graph Traversal (essential for agents)

| Command | Description | Maps to Obsidian CLI |
|---------|-------------|---------------------|
| `vault backlinks file="X"` | Incoming `[[wikilinks]]` | `obsidian backlinks` |
| `vault backlinks file="X" --counts` | With counts | `obsidian backlinks counts` |
| `vault links file="X"` | Outgoing `[[wikilinks]]` | `obsidian links` |
| `vault unresolved` | Links pointing to non-existent notes | `obsidian unresolved` |
| `vault orphans` | Notes with zero incoming links | `obsidian orphans` |

### Tier 3: Tags & Properties

| Command | Description | Maps to Obsidian CLI |
|---------|-------------|---------------------|
| `vault tags` | List all tags | `obsidian tags` |
| `vault tags --counts` | With counts | `obsidian tags counts` |
| `vault tags --sort=count` | Sorted by frequency | `obsidian tags sort=count` |
| `vault tag name="X"` | Notes with tag | `obsidian tag name="X"` |
| `vault tag name="X" --verbose` | With file list | `obsidian tag name="X" verbose` |
| `vault properties file="X"` | All properties of note | `obsidian properties file="X"` |
| `vault property:read name="X" file="Y"` | Read one property | `obsidian property:read` |
| `vault property:set name="X" value="Y" file="Z"` | Set property | `obsidian property:set` |
| `vault property:remove name="X" file="Y"` | Remove property | `obsidian property:remove` |

### Tier 4: Templates

| Command | Description | Maps to Obsidian CLI |
|---------|-------------|---------------------|
| `vault templates` | List available templates | `obsidian templates` |
| `vault template:read name="X"` | Read template content | `obsidian template:read` |

### Tier 5: Utility

| Command | Description |
|---------|-------------|
| `vault ping` | Test CouchDB connectivity |
| `vault config show` | Show resolved config |
| `vault config set <key> <value>` | Update config |
| `vault --version` | Show version |
| `vault --help` | Show help |

---

## Implementation Details

### In-Memory Index (for graph operations)

Graph commands (`backlinks`, `links`, `unresolved`, `orphans`, `tags`) need to scan all notes. Strategy:

1. Load all notes from CouchDB in a single `_all_docs?include_docs=true` call
2. Fetch content chunks for each note
3. Build in-memory index: path -> content, wikilink graph, tag map
4. Run the requested operation
5. Discard index when process exits

No persistent cache. No background sync. Every command invocation is stateless.

**Performance target:** < 5 seconds for a vault with 500 notes. Acceptable — agent commands are not interactive.

**Optimization:** For CRUD commands (read, write, create, delete, move), skip the index entirely. Only graph/tag/search commands trigger the full vault load.

### Wikilink Parsing

Extract `[[wikilinks]]` from both frontmatter and body text using regex:

```python
import re
WIKILINK_RE = re.compile(r'\[\[([^\]|]+?)(?:\|[^\]]+?)?\]\]')
```

This captures `[[Note Name]]` and `[[Note Name|Display]]`, extracting just the note name.

### Frontmatter Parsing

Use `python-frontmatter` library for reliable YAML parsing. Do not roll custom YAML parsing.

```python
import frontmatter

post = frontmatter.loads(content)
props = dict(post.metadata)  # frontmatter as dict
body = post.content           # body without frontmatter
```

### File Resolution

The `file=` parameter resolves like an Obsidian wikilink — name only, case-insensitive, no extension required:

1. Try exact path match (case-insensitive)
2. Try `{name}.md` (case-insensitive)
3. Try basename match across all folders (case-insensitive)
4. Return first match, or error if ambiguous

The `path=` parameter is an exact path from vault root.

### Output Formats

- **Default (text):** Human-readable, one item per line
- **`--json`:** Structured JSON output (for agents parsing output programmatically)

---

## Technical Decisions

1. **Python 3.10+** — available on every VM and server. Simple HTTP requests to CouchDB. Rich ecosystem for markdown/YAML parsing.
2. **`requests`** library for CouchDB HTTP — simple, reliable, no CouchDB-specific ORM needed
3. **`python-frontmatter`** for YAML frontmatter — battle-tested, handles edge cases
4. **`click`** for CLI — clean decorator syntax, automatic `--help`, subcommand support
5. **No AI dependencies** — no torch, no transformers, no API keys. Pure vault access.
6. **Minimal dependencies:** `requests`, `python-frontmatter`, `click`, `PyYAML`
7. **Stateless** — no daemon, no background process, no persistent cache. Each command is a fresh invocation.
8. **`pip install`** — standard Python packaging via `pyproject.toml`. No npm, no Node.js.

---

## Repository Structure

```
obsidian-vault-cli/
├── vault_cli/
│   ├── __init__.py
│   ├── __main__.py          # python -m vault_cli
│   ├── client.py            # VaultClient — CouchDB CRUD + LiveSync format
│   ├── index.py             # In-memory vault index (graph, tags, search)
│   ├── frontmatter.py       # Frontmatter parsing/writing helpers
│   ├── wikilinks.py         # Wikilink extraction and resolution
│   ├── config.py            # Config loader (file + env vars)
│   ├── cli/
│   │   ├── __init__.py
│   │   ├── main.py          # Click CLI entry point + command groups
│   │   ├── crud.py          # read, create, write, append, prepend, delete, move, rename
│   │   ├── files.py         # files, folders
│   │   ├── search.py        # search, search:context
│   │   ├── graph.py         # backlinks, links, unresolved, orphans
│   │   ├── tags.py          # tags, tag
│   │   ├── properties.py    # properties, property:read/set/remove
│   │   ├── templates.py     # templates, template:read
│   │   └── config_cmd.py    # config show/set, ping
│   └── output.py            # Output formatting (text, JSON)
├── tests/
│   ├── test_client.py
│   ├── test_index.py
│   ├── test_wikilinks.py
│   ├── test_frontmatter.py
│   └── test_cli.py
├── pyproject.toml
├── requirements.txt
├── LICENSE
├── README.md
└── SPEC.md                  # This file
```

---

## What This Project Is NOT

- **Not an AI curation tool.** No inbox processing, no auto-tagging, no AI-powered filing.
- **Not a sync tool.** Does not sync files to/from disk. Use LiveSync for that.
- **Not a replacement for Obsidian.** Does not render markdown, manage plugins, or provide a UI.
- **Not a CouchDB admin tool.** Does not manage databases, users, or replication.

---

## Implementation Plan

### Phase 1: Core (VaultClient + CRUD CLI) ✅

1. `vault_cli/client.py` — VaultClient class
2. `vault_cli/config.py` — Config loader
3. `vault_cli/cli/crud.py` — `read`, `create`, `write`, `append`, `prepend`, `delete`, `move`, `rename`
4. `vault_cli/cli/files.py` — `files`, `folders`
5. `vault ping` + `vault config show/set`

### Phase 2: Search + Graph ✅

1. `vault_cli/index.py` — In-memory vault index
2. `vault_cli/wikilinks.py` — Wikilink regex extraction
3. `vault_cli/cli/search.py` — `search`, `search --context`
4. `vault_cli/cli/graph.py` — `backlinks`, `links`, `unresolved`, `orphans`

### Phase 3: Tags, Properties, Templates ✅

1. `vault_cli/frontmatter.py` — Frontmatter helpers
2. `vault_cli/cli/tags.py` — `tags`, `tag`
3. `vault_cli/cli/properties.py` — `properties`, `property:read/set/remove`
4. `vault_cli/cli/templates.py` — `templates`, `template:read`

### Phase 4: Safety & Error Handling (v0.2.0) ✅

1. `write --force` guard — refuse silent overwrites by default
2. `create` existence check
3. `delete --yes` / interactive confirmation
4. `property:set` read-before-write + diff preview
5. `--dry-run` global flag
6. `deleted: true` detection on write
7. Wrapped HTTP error messages with context + hints
8. Bump version to `0.2.0`

### Phase 5: Vault Rule Guardrails (v0.2.2) ✅

1. `vault_cli/core/guardrails.py` — rule engine with `check_rules()` and `Violation` dataclass
2. Three rules: folder placement, missing categories, folder ↔ category mismatch
3. `--yes` flag on `create`, `write`, `move` — accept warnings automatically
4. `--strict` flag — treat violations as hard errors (exit code 2)
5. Interactive mode (default) — prompt with `y/N`
6. Dynamic folder existence check (queries vault, no hardcoded list)
7. `--force` does NOT bypass rules (separate concern)
