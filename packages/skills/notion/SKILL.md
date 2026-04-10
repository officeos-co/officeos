# Notion

Search, read, create, and manage Notion pages, blocks, databases, and to-do items.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Pages

### Search pages

```
notion search --query "meeting notes" --page_size 5
```

| Argument    | Type   | Required | Default | Description               |
|-------------|--------|----------|---------|---------------------------|
| `query`     | string | yes      |         | Free-text search query    |
| `page_size` | int    | no       | 10      | Results to return (1–100) |

Returns: `id`, `title`, `url`, `object_type`.

### Read page content

```
notion read_page --page_id "a1b2c3d4-..."
```

Returns the page's top-level block children as plain text.

### Create a child page

```
notion create_page --parent_id "a1b2c3d4-..." --title "Weekly Standup Notes"
```

| Argument    | Type   | Required | Description              |
|-------------|--------|----------|--------------------------|
| `parent_id` | string | yes      | Parent page UUID         |
| `title`     | string | yes      | Title for the new page   |

Returns: `id`, `url`, `title`.

## Blocks

### List blocks

```
notion list_blocks --page_id "a1b2c3d4-..." --type to_do
```

| Argument  | Type   | Required | Description                                           |
|-----------|--------|----------|-------------------------------------------------------|
| `page_id` | string | yes      | Page UUID                                             |
| `type`    | string | no       | Filter: `paragraph`, `heading_1`, `heading_2`, `heading_3`, `bulleted_list_item`, `numbered_list_item`, `to_do`, `toggle`, `quote`, `callout`, `divider`, `code` |

Returns: `id`, `type`, `content`, `checked` (for to-dos).

### Add a block

```
notion add_block --page_id "a1b2c3d4-..." --content "Hello world" --type paragraph
```

| Argument  | Type   | Required | Default     | Description         |
|-----------|--------|----------|-------------|---------------------|
| `page_id` | string | yes      |             | Page to append to   |
| `content` | string | no       | `""`        | Text content        |
| `type`    | string | no       | `paragraph` | Block type (see list_blocks) |

### Update a block

```
notion update_block --block_id "b1c2d3e4-..." --content "Updated text"
```

### Delete a block

```
notion delete_block --block_id "b1c2d3e4-..."
```

## To-Do Items

### Add a to-do

```
notion add_todo --page_id "a1b2c3d4-..." --content "Buy groceries"
```

| Argument  | Type    | Required | Default | Description            |
|-----------|---------|----------|---------|------------------------|
| `page_id` | string  | yes      |         | Page to add to-do to   |
| `content` | string  | yes      |         | To-do item text        |
| `checked` | boolean | no       | false   | Initial checked state  |

### Mark a to-do done/undone

```
notion update_todo --block_id "b1c2d3e4-..." --checked true
```

## Databases

### Query a database

```
notion query_database --database_id "d1e2f3a4-..." --page_size 10
```

| Argument      | Type   | Required | Default | Description                          |
|---------------|--------|----------|---------|--------------------------------------|
| `database_id` | string | yes      |         | Database UUID                        |
| `filter_json` | string | no       |         | JSON filter (Notion API syntax)      |
| `sort_json`   | string | no       |         | JSON sort array (Notion API syntax)  |
| `page_size`   | int    | no       | 10      | Results to return (1–100)            |

Returns: `id`, `url`, `properties` (serialized JSON of all property values).

## Workflow

1. **Always start with `notion search`** to find pages. Never guess page IDs.
2. Use `id` from search results with `read_page`, `list_blocks`, or `create_page`.
3. To manage content: `list_blocks` → identify block IDs → `update_block` or `delete_block`.
4. For to-do lists: `add_todo` to create items, `list_blocks --type to_do` to view them, `update_todo` to check/uncheck.
5. For databases: use `query_database` with filter/sort JSON. Inspect returned properties to understand the schema.
6. Prefer **appending** (`add_block`) over destructive operations.
7. When answering questions, **cite which page** the information came from.

## Safety notes

- Page and block IDs are opaque UUIDs. **Never fabricate them** — always discover via search or list_blocks.
- Notion API is rate-limited (3 req/sec). Avoid rapid repeated queries in a loop.
- `read_page` and `list_blocks` return top-level blocks only. Nested content may be truncated.
- Only pages shared with the configured integration are visible.
- `delete_block` is destructive and cannot be undone. Confirm with the user before deleting.
