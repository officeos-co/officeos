# Google Workspace

This skill gives you access to search Google Drive and read Google Calendar events.

## Commands

- `google drive_search --query "Q1 report" --page_size 5` — Search Drive for files by name.
- `google calendar_upcoming --max_results 5` — List the next N calendar events.

## Workflow

For file/document questions:
1. Use `google drive_search` with descriptive keywords.
2. Present results with file names and direct links.

For schedule/meeting questions:
1. Use `google calendar_upcoming` with an appropriate `max_results`.
2. Summarize events by date, time, and attendees.

## Limitations

- Uses a service account. Only files and calendars **shared with the service account** are visible.
- Drive search matches file names only — no content search.
- Calendar returns events from the configured calendar only (defaults to primary).
- This skill is **read-only**. You cannot create, modify, or delete files or events.
