# Notion

This skill gives you access to search and read Notion pages and databases.

## Tools

### `notion.search`

Search the connected Notion workspace for pages matching a query.

**Parameters (required marked with *):**
- `query`* (string): Free-text search query. Matches page titles and content.
- `page_size` (integer, optional): 1–100. Default 10.

**Example call:**
```json
{ "query": "meeting notes", "page_size": 5 }
```

**Returns:** Array of pages with `id`, `title`, `url`, `last_edited_time`. Use the `id` to read the full page content.

---

### `notion.read_page`

Fetch a Notion page's top-level block children as plain text.

**Parameters (required marked with *):**
- `page_id`* (string): The UUID of the page to read. Get this from `notion.search` results.

**Example call:**
```json
{ "page_id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890" }
```

**Returns:** The page content as plain text, extracted from top-level blocks (paragraphs, headings, lists, etc.).

## Workflow

1. Always start with `notion.search` to find pages. Never guess page IDs.
2. Use the `id` from search results with `notion.read_page` to get content.
3. If a search returns no results, try alternative keywords — the content may exist under a different title.
4. When answering questions, cite which page the information came from.

## Limitations

- Only pages **shared with the integration** are visible. If search returns nothing, the pages may exist but not be shared.
- Page IDs are opaque UUIDs. Never fabricate them — always discover via search.
- `notion.read_page` returns top-level blocks only. Deeply nested content (toggles, synced blocks) may be truncated.
- Notion API is rate-limited. Avoid rapid repeated queries in a loop.
- This skill is **read-only**. You cannot create, update, or delete pages.
