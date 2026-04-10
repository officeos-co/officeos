# Google Workspace

This skill gives you access to search Google Drive and read Google Calendar events.

## Tools

### `google.drive_search`

Search Google Drive for files whose name contains the query.

**Parameters (required marked with *):**
- `query`* (string): Search text. Matches file names using Google Drive's full-text search.
- `page_size` (integer, optional): 1–100. Default 10.

**Example call:**
```json
{ "query": "Q1 report", "page_size": 5 }
```

**Returns:** Array of files with `id`, `name`, `mimeType`, `webViewLink`, `modifiedTime`. The `webViewLink` opens the file in a browser.

---

### `google.calendar_upcoming`

List the next N events on the configured calendar.

**Parameters:**
- `max_results` (integer, optional): 1–50. Default 10.

**Example call:**
```json
{ "max_results": 5 }
```

**Returns:** Array of events with `summary` (title), `start`, `end`, `location`, `attendees`, `htmlLink`.

## Workflow

For file/document questions:
1. Use `google.drive_search` with descriptive keywords.
2. Present results with file names and direct links.

For schedule/meeting questions:
1. Use `google.calendar_upcoming` with an appropriate `max_results`.
2. Summarize events by date, time, and attendees.

## Limitations

- Uses a service account. Only files and calendars **shared with or delegated to** the service account are visible.
- Drive search uses Google's full-text search. Simple keywords work best — avoid complex query syntax.
- Calendar returns events from the configured calendar only (defaults to primary).
- This skill is **read-only**. You cannot create, modify, or delete files or events.
