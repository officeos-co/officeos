# Notion

This skill gives you access to search and read Notion pages and databases.

## Commands

- `notion search --query "meeting notes" --page_size 5` — Search for pages matching a query.
- `notion read_page --page_id "a1b2c3d4-..."` — Fetch a page's content as plain text.

## Workflow

1. Always start with `notion search` to find pages. Never guess page IDs.
2. Use the `id` from search results with `notion read_page` to get content.
3. If a search returns no results, try alternative keywords — the content may exist under a different title.
4. When answering questions, cite which page the information came from.

## Limitations

- Only pages **shared with the integration** are visible. If search returns nothing, the pages may exist but not be shared.
- Page IDs are opaque UUIDs. Never fabricate them — always discover via search.
- `read_page` returns top-level blocks only. Deeply nested content (toggles, synced blocks) may be truncated.
- Notion API is rate-limited. Avoid rapid repeated queries in a loop.
- This skill is **read-only**. You cannot create, update, or delete pages.
