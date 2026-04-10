# Notion Skill

Search and read Notion pages and databases.

## Actions

- **search** — Search the connected Notion workspace for pages matching a query.
- **read_page** — Fetch a Notion page's top-level block children as plain text.

## Credentials

- `api_key` — Internal Integration Token. Create one at https://www.notion.so/my-integrations and share the pages you want the agent to access with it.

## Limitations

- Read-only. Cannot create or update pages.
- Only pages shared with the integration are visible.
- Subject to Notion API rate limits (3 requests/second).
