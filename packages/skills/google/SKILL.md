# Google Workspace

Search Google Drive files and list upcoming Google Calendar events.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Commands

### Search Drive

```
google drive_search --query "Q1 report" --page_size 5
```

| Argument    | Type   | Required | Default | Description               |
| ----------- | ------ | -------- | ------- | ------------------------- |
| `query`     | string | yes      |         | Search text (file names)  |
| `page_size` | int    | no       | 10      | Results to return (1–100) |

Returns array of files: `id`, `name`, `mime_type`, `web_view_link`, `modified_time`.
The `web_view_link` opens the file in a browser.

### Upcoming calendar events

```
google calendar_upcoming --max_results 5
```

| Argument      | Type | Required | Default | Description             |
| ------------- | ---- | -------- | ------- | ----------------------- |
| `max_results` | int  | no       | 10      | Events to return (1–50) |

Returns array of events: `id`, `summary`, `start`, `end`, `html_link`.

## Workflow

For file/document questions:

1. Use `google drive_search` with descriptive keywords.
2. Present results with file names and direct links (`web_view_link`).
3. If results are empty, try shorter or alternative keywords.

For schedule/meeting questions:

1. Use `google calendar_upcoming` to get the next events.
2. Summarize events by date, time, and title.
3. Include the `html_link` so the user can open events directly.

## Safety notes

- Drive search matches **file names only** — no content search.
- Calendar returns events from the configured calendar only (defaults to primary).
- Only files and calendars shared with the configured service account are visible.
- This skill is **read-only**. You cannot create, modify, or delete files or events.
