# Notion

Search and read Notion pages and databases.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Commands

### Search pages

```
notion search --query "meeting notes" --page_size 5
```

| Argument    | Type   | Required | Default | Description               |
|-------------|--------|----------|---------|---------------------------|
| `query`     | string | yes      |         | Free-text search query    |
| `page_size` | int    | no       | 10      | Results to return (1–100) |

Returns array of pages: `id`, `title`, `url`, `object_type`.

### Read a page

```
notion read_page --page_id "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

| Argument  | Type   | Required | Description                             |
|-----------|--------|----------|-----------------------------------------|
| `page_id` | string | yes      | Page UUID from `notion search` results  |

Returns `page_id` and `text` — top-level block children as plain text.

## Workflow

1. **Always start with `notion search`** to find pages. Never guess page IDs.
2. Use the `id` from search results with `notion read_page` to get content.
3. If search returns no results, try alternative keywords — content may exist under a different title or be nested inside a parent page.
4. When answering questions, **cite which page** the information came from (include title and URL).
5. For broad discovery, search with short generic terms first, then refine.

## Safety notes

- Page IDs are opaque UUIDs. **Never fabricate them** — always discover via search.
- Notion API is rate-limited (3 req/sec). Avoid rapid repeated queries in a loop.
- `read_page` returns top-level blocks only. Deeply nested content (toggles, synced blocks, child pages) may be truncated.
- This skill is **read-only**. You cannot create, update, or delete pages.
- Only pages shared with the configured integration are visible. If content can't be found, it may need to be shared.
