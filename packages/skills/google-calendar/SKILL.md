# Google Calendar

Create, read, update, and manage calendars, events, attendees, recurring events, and availability via the Google Calendar API v3.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Calendars

### List calendars

```
google-calendar list_calendars
```

Returns: list of `id`, `summary`, `description`, `time_zone`, `primary`, `access_role`.

### Get calendar

```
google-calendar get_calendar --calendar_id "primary"
```

| Argument      | Type   | Required | Default   | Description        |
|---------------|--------|----------|-----------|--------------------|
| `calendar_id` | string | no       | `primary` | Calendar ID        |

Returns: `id`, `summary`, `description`, `time_zone`, `location`.

### Create calendar

```
google-calendar create_calendar --summary "Team Standups" --time_zone "America/New_York"
```

| Argument    | Type   | Required | Description               |
|-------------|--------|----------|---------------------------|
| `summary`   | string | yes      | Calendar name             |
| `time_zone` | string | no       | IANA time zone identifier |

Returns: `id`, `summary`.

### Delete calendar

```
google-calendar delete_calendar --calendar_id "abc123@group.calendar.google.com"
```

| Argument      | Type   | Required | Description             |
|---------------|--------|----------|-------------------------|
| `calendar_id` | string | yes      | Calendar ID to delete   |

Returns: confirmation status.

## Events

### List events

```
google-calendar list_events --calendar_id "primary" --time_min "2026-04-14T00:00:00Z" --time_max "2026-04-21T00:00:00Z" --max_results 20
```

| Argument      | Type   | Required | Default   | Description                            |
|---------------|--------|----------|-----------|----------------------------------------|
| `calendar_id` | string | no       | `primary` | Calendar ID                            |
| `time_min`    | string | no       |           | Start of time range (RFC 3339)         |
| `time_max`    | string | no       |           | End of time range (RFC 3339)           |
| `query`       | string | no       |           | Free-text search across event fields   |
| `max_results` | int    | no       | 10        | Events to return (1-2500)              |

Returns: list of `id`, `summary`, `start`, `end`, `location`, `description`, `attendees`, `status`, `html_link`, `recurring_event_id`.

### Get event

```
google-calendar get_event --calendar_id "primary" --event_id "abc123def456"
```

| Argument      | Type   | Required | Default   | Description     |
|---------------|--------|----------|-----------|-----------------|
| `calendar_id` | string | no       | `primary` | Calendar ID     |
| `event_id`    | string | yes      |           | Event ID        |

Returns: full event details including `summary`, `description`, `start`, `end`, `location`, `attendees`, `recurrence`, `reminders`, `organizer`, `creator`, `html_link`.

### Create event

```
google-calendar create_event --calendar_id "primary" --summary "Sprint Planning" --start "2026-04-15T09:00:00" --end "2026-04-15T10:00:00" --location "Room 42" --attendees '["alice@example.com","bob@example.com"]' --description "Review sprint backlog"
```

| Argument      | Type     | Required | Default   | Description                                    |
|---------------|----------|----------|-----------|------------------------------------------------|
| `calendar_id` | string   | no       | `primary` | Calendar ID                                    |
| `summary`     | string   | yes      |           | Event title                                    |
| `start`       | string   | yes      |           | Start time (RFC 3339 or date for all-day)      |
| `end`         | string   | yes      |           | End time (RFC 3339 or date for all-day)        |
| `location`    | string   | no       |           | Event location                                 |
| `description` | string   | no       |           | Event description                              |
| `attendees`   | string[] | no       |           | List of attendee email addresses               |
| `recurrence`  | string[] | no       |           | RRULE strings (e.g. `["RRULE:FREQ=WEEKLY"]`)   |
| `time_zone`   | string   | no       |           | IANA time zone for start/end                   |

Returns: `id`, `html_link`, `summary`, `start`, `end`.

### Update event

```
google-calendar update_event --calendar_id "primary" --event_id "abc123def456" --summary "Updated Sprint Planning" --location "Room 101"
```

| Argument      | Type     | Required | Default   | Description                           |
|---------------|----------|----------|-----------|---------------------------------------|
| `calendar_id` | string   | no       | `primary` | Calendar ID                           |
| `event_id`    | string   | yes      |           | Event ID to update                    |
| `summary`     | string   | no       |           | Updated title                         |
| `start`       | string   | no       |           | Updated start time                    |
| `end`         | string   | no       |           | Updated end time                      |
| `location`    | string   | no       |           | Updated location                      |
| `description` | string   | no       |           | Updated description                   |
| `attendees`   | string[] | no       |           | Updated attendee list (replaces all)  |

Returns: `id`, `html_link`, `summary`, `start`, `end`, `updated`.

### Delete event

```
google-calendar delete_event --calendar_id "primary" --event_id "abc123def456"
```

| Argument      | Type   | Required | Default   | Description           |
|---------------|--------|----------|-----------|-----------------------|
| `calendar_id` | string | no       | `primary` | Calendar ID           |
| `event_id`    | string | yes      |           | Event ID to delete    |

Returns: confirmation status.

### Move event

```
google-calendar move_event --calendar_id "primary" --event_id "abc123def456" --destination_calendar_id "work@group.calendar.google.com"
```

| Argument                   | Type   | Required | Default   | Description                     |
|----------------------------|--------|----------|-----------|---------------------------------|
| `calendar_id`              | string | no       | `primary` | Source calendar ID              |
| `event_id`                 | string | yes      |           | Event ID to move                |
| `destination_calendar_id`  | string | yes      |           | Target calendar ID              |

Returns: `id`, `html_link`, `calendar_id`.

## Quick Add

### Quick add event

```
google-calendar quick_add --calendar_id "primary" --text "Lunch with Alice tomorrow at noon"
```

| Argument      | Type   | Required | Default   | Description                                |
|---------------|--------|----------|-----------|--------------------------------------------|
| `calendar_id` | string | no       | `primary` | Calendar ID                                |
| `text`        | string | yes      |           | Natural language event description         |

Returns: `id`, `html_link`, `summary`, `start`, `end`.

## Attendees

### Add attendee

```
google-calendar add_attendee --calendar_id "primary" --event_id "abc123def456" --email "charlie@example.com"
```

| Argument      | Type   | Required | Default   | Description               |
|---------------|--------|----------|-----------|---------------------------|
| `calendar_id` | string | no       | `primary` | Calendar ID               |
| `event_id`    | string | yes      |           | Event ID                  |
| `email`       | string | yes      |           | Attendee email to add     |

Returns: `id`, `attendees`.

### Remove attendee

```
google-calendar remove_attendee --calendar_id "primary" --event_id "abc123def456" --email "charlie@example.com"
```

| Argument      | Type   | Required | Default   | Description                 |
|---------------|--------|----------|-----------|-----------------------------|
| `calendar_id` | string | no       | `primary` | Calendar ID                 |
| `event_id`    | string | yes      |           | Event ID                    |
| `email`       | string | yes      |           | Attendee email to remove    |

Returns: `id`, `attendees`.

### Respond to event

```
google-calendar respond --calendar_id "primary" --event_id "abc123def456" --status "accepted"
```

| Argument      | Type   | Required | Default   | Description                                    |
|---------------|--------|----------|-----------|------------------------------------------------|
| `calendar_id` | string | no       | `primary` | Calendar ID                                    |
| `event_id`    | string | yes      |           | Event ID                                       |
| `status`      | string | yes      |           | `accepted`, `declined`, or `tentative`         |

Returns: `id`, `status`, `attendees`.

## Recurring Events

### List instances

```
google-calendar list_instances --calendar_id "primary" --event_id "abc123def456" --time_min "2026-04-01T00:00:00Z" --time_max "2026-05-01T00:00:00Z"
```

| Argument      | Type   | Required | Default   | Description                          |
|---------------|--------|----------|-----------|--------------------------------------|
| `calendar_id` | string | no       | `primary` | Calendar ID                          |
| `event_id`    | string | yes      |           | Recurring event ID                   |
| `time_min`    | string | no       |           | Start of range (RFC 3339)            |
| `time_max`    | string | no       |           | End of range (RFC 3339)              |
| `max_results` | int    | no       | 25        | Instances to return                  |

Returns: list of individual event instances with `id`, `summary`, `start`, `end`, `recurring_event_id`.

## Free/Busy

### Check availability

```
google-calendar check_availability --time_min "2026-04-15T08:00:00Z" --time_max "2026-04-15T18:00:00Z" --calendars '["primary","alice@example.com"]'
```

| Argument    | Type     | Required | Description                              |
|-------------|----------|----------|------------------------------------------|
| `time_min`  | string   | yes      | Start of range (RFC 3339)                |
| `time_max`  | string   | yes      | End of range (RFC 3339)                  |
| `calendars` | string[] | yes      | List of calendar IDs to check            |

Returns: per-calendar list of `busy` intervals with `start` and `end`.

## Settings

### Get settings

```
google-calendar get_settings
```

Returns: list of `id`, `value` for calendar settings (e.g. `timezone`, `locale`, `dateFieldOrder`, `format24HourTime`).

## Workflow

1. **Start with `google-calendar list_calendars`** to discover available calendars.
2. Use `list_events` with time range to see upcoming events.
3. Use `quick_add` for fast event creation from natural language.
4. Use `create_event` for precise control over time, location, attendees, and recurrence.
5. Check availability with `check_availability` before scheduling meetings.
6. Manage attendees with `add_attendee` and `remove_attendee`.
7. For recurring events, use `list_instances` to see individual occurrences.

## Safety notes

- Event IDs are opaque strings. **Never fabricate them** -- always discover via `list_events` or `get_event`.
- All times should be in RFC 3339 format (e.g. `2026-04-15T09:00:00Z` or `2026-04-15T09:00:00-04:00`). For all-day events use date format `2026-04-15`.
- **`delete_event` is permanent.** Confirm with the user before deleting.
- `delete_calendar` removes the entire calendar and all its events. Use with extreme caution.
- `update_event` with `attendees` replaces the full attendee list. Use `add_attendee`/`remove_attendee` to modify individual attendees.
- Recurrence rules follow the RFC 5545 RRULE format (e.g. `RRULE:FREQ=WEEKLY;BYDAY=MO,WE,FR`).
- Only calendars accessible to the authenticated account are visible.
- Calendar API has per-user rate limits. Avoid rapid loops of create/update operations.
