# PagerDuty

Manage incidents, services, users, on-call schedules, escalation policies, and more via the PagerDuty REST API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Incidents

### List incidents

```
pagerduty list_incidents --statuses '["triggered","acknowledged"]' --service_ids '["P1ABC23"]' --limit 25
```

| Argument      | Type     | Required | Default | Description                                    |
|---------------|----------|----------|---------|------------------------------------------------|
| `statuses`    | string[] | no       |         | Filter by status: `triggered`, `acknowledged`, `resolved` |
| `service_ids` | string[] | no       |         | Filter by service IDs                          |
| `team_ids`    | string[] | no       |         | Filter by team IDs                             |
| `urgency`     | string   | no       |         | `high` or `low`                                |
| `limit`       | int      | no       | 25      | Max results (1-100)                            |
| `offset`      | int      | no       | 0       | Pagination offset                              |
| `since`       | string   | no       |         | Filter to incidents since ISO 8601 timestamp   |
| `until`       | string   | no       |         | Filter to incidents until ISO 8601 timestamp   |

Returns: list of `id`, `title`, `status`, `urgency`, `service`, `assigned_to`, `created_at`, `html_url`.

### Get incident

```
pagerduty get_incident --incident_id "P1ABC23"
```

| Argument      | Type   | Required | Description |
|---------------|--------|----------|-------------|
| `incident_id` | string | yes      | Incident ID |

Returns: `id`, `title`, `status`, `urgency`, `service`, `escalation_policy`, `assigned_to`, `acknowledgers`, `last_status_change_at`, `created_at`, `html_url`, `description`, `incident_key`.

### Create incident

```
pagerduty create_incident --title "Database connection pool exhausted" --service_id "P1ABC23" --urgency high --body "Error rate spike on prod-db-01 at 14:32 UTC"
```

| Argument      | Type   | Required | Description                           |
|---------------|--------|----------|---------------------------------------|
| `title`       | string | yes      | Incident title                        |
| `service_id`  | string | yes      | Service ID to create incident for     |
| `urgency`     | string | no       | `high` or `low`                       |
| `body`        | string | no       | Incident details/description          |
| `incident_key`| string | no       | Dedup key                             |
| `escalation_policy_id` | string | no | Override escalation policy      |

Returns: `id`, `title`, `status`, `html_url`.

### Acknowledge incident

```
pagerduty acknowledge_incident --incident_id "P1ABC23"
```

| Argument      | Type   | Required | Description |
|---------------|--------|----------|-------------|
| `incident_id` | string | yes      | Incident ID |

Returns: `id`, `status`.

### Resolve incident

```
pagerduty resolve_incident --incident_id "P1ABC23"
```

| Argument      | Type   | Required | Description |
|---------------|--------|----------|-------------|
| `incident_id` | string | yes      | Incident ID |

Returns: `id`, `status`, `resolved_at`.

### Add note to incident

```
pagerduty add_note --incident_id "P1ABC23" --content "Investigated - root cause is memory leak in worker pool"
```

| Argument      | Type   | Required | Description     |
|---------------|--------|----------|-----------------|
| `incident_id` | string | yes      | Incident ID     |
| `content`     | string | yes      | Note text       |

Returns: `id`, `content`, `created_at`, `user`.

### List incident alerts

```
pagerduty list_alerts --incident_id "P1ABC23"
```

| Argument      | Type   | Required | Description |
|---------------|--------|----------|-------------|
| `incident_id` | string | yes      | Incident ID |

Returns: list of `id`, `status`, `severity`, `summary`, `source`, `created_at`.

## Services

### List services

```
pagerduty list_services --team_ids '["T1ABC23"]' --query "api" --limit 50
```

| Argument   | Type     | Required | Description                  |
|------------|----------|----------|------------------------------|
| `team_ids` | string[] | no       | Filter by team IDs           |
| `query`    | string   | no       | Search query (name match)    |
| `limit`    | int      | no       | Max results                  |

Returns: list of `id`, `name`, `status`, `description`, `escalation_policy`, `teams`, `html_url`.

### Get service

```
pagerduty get_service --service_id "P1ABC23"
```

| Argument     | Type   | Required | Description |
|--------------|--------|----------|-------------|
| `service_id` | string | yes      | Service ID  |

Returns: `id`, `name`, `status`, `description`, `escalation_policy`, `teams`, `alert_creation`, `incident_urgency_rule`, `html_url`.

## Users

### List users

```
pagerduty list_users --query "john" --team_ids '["T1ABC23"]'
```

| Argument   | Type     | Required | Description              |
|------------|----------|----------|--------------------------|
| `query`    | string   | no       | Search by name or email  |
| `team_ids` | string[] | no       | Filter by team IDs       |
| `limit`    | int      | no       | Max results              |

Returns: list of `id`, `name`, `email`, `role`, `teams`, `html_url`.

### Get user

```
pagerduty get_user --user_id "P1ABC23"
```

| Argument  | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `user_id` | string | yes      | User ID     |

Returns: `id`, `name`, `email`, `role`, `teams`, `contact_methods`, `notification_rules`, `html_url`.

## On-Calls

### List on-calls

```
pagerduty list_oncalls --schedule_ids '["P1ABC23"]' --user_ids '["U1ABC23"]' --earliest true
```

| Argument        | Type     | Required | Description                                      |
|-----------------|----------|----------|--------------------------------------------------|
| `schedule_ids`  | string[] | no       | Filter by schedule IDs                           |
| `user_ids`      | string[] | no       | Filter by user IDs                               |
| `escalation_policy_ids` | string[] | no | Filter by escalation policy IDs              |
| `earliest`      | boolean  | no       | Return only the earliest on-call per layer       |
| `since`         | string   | no       | Start of on-call window (ISO 8601)               |
| `until`         | string   | no       | End of on-call window (ISO 8601)                 |

Returns: list of `escalation_policy`, `escalation_level`, `schedule`, `user`, `start`, `end`.

## Schedules

### List schedules

```
pagerduty list_schedules --query "primary"
```

| Argument | Type   | Required | Description             |
|----------|--------|----------|-------------------------|
| `query`  | string | no       | Filter by schedule name |
| `limit`  | int    | no       | Max results             |

Returns: list of `id`, `name`, `time_zone`, `description`, `escalation_policies`, `users`, `html_url`.

### Get schedule

```
pagerduty get_schedule --schedule_id "P1ABC23" --since "2024-01-15T00:00:00Z" --until "2024-01-22T00:00:00Z"
```

| Argument      | Type   | Required | Description                         |
|---------------|--------|----------|-------------------------------------|
| `schedule_id` | string | yes      | Schedule ID                         |
| `since`       | string | no       | Start of rendered schedule window   |
| `until`       | string | no       | End of rendered schedule window     |

Returns: `id`, `name`, `time_zone`, `final_schedule` (with rendered entries showing user + start/end).

## Escalation Policies

### List escalation policies

```
pagerduty list_escalation_policies --query "engineering" --user_ids '["P1ABC23"]'
```

| Argument   | Type     | Required | Description                    |
|------------|----------|----------|--------------------------------|
| `query`    | string   | no       | Filter by policy name          |
| `user_ids` | string[] | no       | Filter policies with these users |
| `team_ids` | string[] | no       | Filter by team IDs             |

Returns: list of `id`, `name`, `description`, `num_loops`, `escalation_rules`, `services`, `teams`, `html_url`.

### Get escalation policy

```
pagerduty get_escalation_policy --policy_id "P1ABC23"
```

| Argument    | Type   | Required | Description         |
|-------------|--------|----------|---------------------|
| `policy_id` | string | yes      | Escalation policy ID|

Returns: `id`, `name`, `description`, `num_loops`, `escalation_rules`, `services`, `teams`.

## Workflow

1. **Check active incidents** with `list_incidents` filtered by `triggered` or `acknowledged` status.
2. **Acknowledge** a triggered incident with `acknowledge_incident` when you start investigating.
3. **Add notes** with `add_note` to document investigation findings.
4. **Find who is on-call** with `list_oncalls` before escalating.
5. **Resolve** with `resolve_incident` once the problem is fixed.
6. **Review services** with `list_services` to understand the dependency map.

## Safety notes

- `create_incident` immediately notifies on-call responders. Verify service ID and urgency before calling.
- Resolving an incident is irreversible through the API — a new incident must be created if the issue recurs.
- API rate limit is 960 requests/minute per token.
- Always include the `From` header user in production — this skill sets it to `api@officeos.co`.
