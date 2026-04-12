# Obsidian

Manage an Obsidian vault backed by CouchDB (Self-hosted LiveSync) via the `obsctl` CLI.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Commands

### Read a note

```
obsidian read_note --path "Daily/2026-04-12.md"
```

| Argument | Type   | Required | Default | Description       |
|----------|--------|----------|---------|-------------------|
| `path`   | string | yes      |         | Note path in vault |

Returns the note's full markdown content.

### Create a note

```
obsidian create_note --path "Projects/new-idea.md" --content "# New Idea"
```

| Argument      | Type   | Required | Default | Description                        |
|---------------|--------|----------|---------|------------------------------------|
| `path`        | string | yes      |         | Path for the new note              |
| `content`     | string | yes      |         | Markdown content                   |
| `frontmatter` | string | no       |         | YAML frontmatter (without fences)  |

### Write (overwrite) a note

```
obsidian write_note --path "Projects/new-idea.md" --content "# Updated content"
```

| Argument | Type   | Required | Default | Description          |
|----------|--------|----------|---------|----------------------|
| `path`   | string | yes      |         | Note path            |
| `content`| string | yes      |         | New markdown content |

### Append to a note

```
obsidian append_note --path "Daily/2026-04-12.md" --content "\n- New item"
```

| Argument | Type   | Required | Default | Description        |
|----------|--------|----------|---------|--------------------|
| `path`   | string | yes      |         | Note path          |
| `content`| string | yes      |         | Content to append  |

### Prepend to a note

```
obsidian prepend_note --path "Daily/2026-04-12.md" --content "# Header\n"
```

| Argument | Type   | Required | Default | Description         |
|----------|--------|----------|---------|---------------------|
| `path`   | string | yes      |         | Note path           |
| `content`| string | yes      |         | Content to prepend  |

### Delete a note

```
obsidian delete_note --path "Archive/old-note.md" --force true
```

| Argument | Type    | Required | Default | Description               |
|----------|---------|----------|---------|---------------------------|
| `path`   | string  | yes      |         | Note path                 |
| `force`  | boolean | no       | false   | Skip confirmation prompt  |

### List files

```
obsidian list_files --folder "Daily" --pattern "*.md"
```

| Argument  | Type   | Required | Default | Description                |
|-----------|--------|----------|---------|----------------------------|
| `folder`  | string | no       |         | Folder to list (root if omitted) |
| `pattern` | string | no       |         | Glob pattern filter        |

### List folders

```
obsidian list_folders
```

No arguments. Returns all folder paths in the vault.

### Move a note

```
obsidian move_note --from "Inbox/note.md" --to "Projects/note.md"
```

| Argument | Type   | Required | Default | Description      |
|----------|--------|----------|---------|------------------|
| `from`   | string | yes      |         | Current path     |
| `to`     | string | yes      |         | Destination path |

### Rename a note

```
obsidian rename_note --path "Projects/old-name.md" --new_name "new-name.md"
```

| Argument   | Type   | Required | Default | Description    |
|------------|--------|----------|---------|----------------|
| `path`     | string | yes      |         | Current path   |
| `new_name` | string | yes      |         | New file name  |

### Get backlinks

```
obsidian get_backlinks --path "Projects/idea.md"
```

| Argument | Type   | Required | Default | Description |
|----------|--------|----------|---------|-------------|
| `path`   | string | yes      |         | Note path   |

Returns list of notes that link to this note.

### Get outgoing links

```
obsidian get_links --path "Projects/idea.md"
```

| Argument | Type   | Required | Default | Description |
|----------|--------|----------|---------|-------------|
| `path`   | string | yes      |         | Note path   |

Returns list of notes linked from this note.

### Find orphan notes

```
obsidian find_orphans
```

No arguments. Returns notes with no incoming or outgoing links.

### Find unresolved links

```
obsidian find_unresolved
```

No arguments. Returns broken/unresolved `[[wikilinks]]`.

### Search

```
obsidian search --query "meeting notes" --limit 20
```

| Argument | Type   | Required | Default | Description             |
|----------|--------|----------|---------|-------------------------|
| `query`  | string | yes      |         | Full-text search query  |
| `limit`  | int    | no       | 10      | Max results             |

### List tags

```
obsidian list_tags
```

No arguments. Returns all tags used across the vault.

### Find notes by tag

```
obsidian find_by_tag --tag "project"
```

| Argument | Type   | Required | Default | Description              |
|----------|--------|----------|---------|--------------------------|
| `tag`    | string | yes      |         | Tag name (without `#`)   |

### Find notes by category

```
obsidian find_by_category --category "project" --limit 20
```

| Argument   | Type   | Required | Default | Description                                     |
|------------|--------|----------|---------|-------------------------------------------------|
| `category` | string | yes      |         | Category value to match (e.g. `project`)        |
| `limit`    | int    | no       | 50      | Max results to return                           |

Returns an array of `{ path, title, category, tags }` for notes whose `category` frontmatter property matches exactly.

### Query notes by property

```
obsidian query_by_property --property "status" --value "active" --operator "eq" --limit 30
```

| Argument   | Type                          | Required | Default | Description                                                                       |
|------------|-------------------------------|----------|---------|-----------------------------------------------------------------------------------|
| `property` | string                        | yes      |         | Frontmatter property name (e.g. `status`, `priority`)                            |
| `value`    | string                        | no       |         | Value to match (not required when `operator` is `exists`)                        |
| `operator` | `eq` \| `contains` \| `exists` | no      | `eq`    | `eq` — exact match; `contains` — substring/array member; `exists` — any value    |
| `limit`    | int                           | no       | 50      | Max results to return                                                             |

Returns an array of `{ path, title, value, tags }` for matching notes.

### Get frontmatter properties

```
obsidian get_properties --path "Projects/idea.md"
```

| Argument | Type   | Required | Default | Description |
|----------|--------|----------|---------|-------------|
| `path`   | string | yes      |         | Note path   |

Returns key-value pairs from the note's YAML frontmatter.

### Set a frontmatter property

```
obsidian set_property --path "Projects/idea.md" --key "status" --value "active"
```

| Argument | Type   | Required | Default | Description    |
|----------|--------|----------|---------|----------------|
| `path`   | string | yes      |         | Note path      |
| `key`    | string | yes      |         | Property name  |
| `value`  | string | yes      |         | Property value |

### Apply a template

```
obsidian apply_template --path "Daily/2026-04-12.md" --template "daily-note"
```

| Argument   | Type   | Required | Default | Description    |
|------------|--------|----------|---------|----------------|
| `path`     | string | yes      |         | Note path      |
| `template` | string | yes      |         | Template name  |

### List templates

```
obsidian list_templates
```

No arguments. Returns available template names.

## Workflow

1. Use `obsidian list_files` or `obsidian search` to discover notes.
2. Use `obsidian read_note` to view content, `obsidian get_properties` for metadata.
3. Use `obsidian get_backlinks` / `obsidian get_links` to explore connections.
4. Use `obsidian create_note`, `obsidian write_note`, `obsidian append_note` to modify vault content.
5. Use `obsidian find_orphans` and `obsidian find_unresolved` for vault maintenance.
6. Use `obsidian list_tags` and `obsidian find_by_tag` to navigate by topic.
7. Use `obsidian find_by_category` to find all notes of a given category, or `obsidian query_by_property` for flexible bases-style filtering on any frontmatter property.

## Safety notes

- The `delete_note` action is destructive. Use `--force true` to skip confirmation.
- The `write_note` action overwrites the entire note. Use `append_note` or `prepend_note` for partial edits.
- All operations go through the `obsctl` CLI which talks directly to CouchDB. Changes sync to all connected Obsidian clients.
- Credentials (CouchDB URL, user, password, vault name) are injected by the runtime and never exposed to the agent.
