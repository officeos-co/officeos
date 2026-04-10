# Google Workspace Skill

Search Google Drive and list upcoming Calendar events via a service-account key.

## Actions

- **drive_search** — Search Google Drive for files whose name contains the query.
- **calendar_upcoming** — List the next N events on the configured calendar.

## Credentials

- `service_account_json` — Full contents of a GCP service account key file. Enable Drive and Calendar APIs in your GCP project and share relevant Drive folders / Calendar with the service account email.
- `calendar_id` (optional) — Defaults to "primary". Use the calendar's email address for shared calendars.

## Limitations

- Read-only. Cannot create, update, or delete files or events.
- Requires domain-wide delegation or explicit sharing for files/calendars.
- Full-text search is name-only for Drive (no content search).
