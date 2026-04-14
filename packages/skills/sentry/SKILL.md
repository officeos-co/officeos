# Sentry

Monitor errors, manage releases, track performance, and configure alerts via the Sentry API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Projects

### List projects

```
sentry list_projects --organization my-org
```

| Argument       | Type   | Required | Description                 |
|----------------|--------|----------|-----------------------------|
| `organization` | string | yes      | Organization slug           |

Returns: list with `id`, `slug`, `name`, `platform`, `status`, `date_created`.

### Get project

```
sentry get_project --organization my-org --project backend
```

| Argument       | Type   | Required | Description       |
|----------------|--------|----------|-------------------|
| `organization` | string | yes      | Organization slug |
| `project`      | string | yes      | Project slug      |

Returns: `id`, `slug`, `name`, `platform`, `status`, `dsn`, `team`, `date_created`, `features`.

### Create project

```
sentry create_project --organization my-org --team platform --name "Backend API" --platform node
```

| Argument       | Type   | Required | Description                                |
|----------------|--------|----------|--------------------------------------------|
| `organization` | string | yes      | Organization slug                          |
| `team`         | string | yes      | Team slug                                  |
| `name`         | string | yes      | Project name                               |
| `platform`     | string | no       | Platform (e.g. `node`, `python`, `csharp`) |

Returns: `id`, `slug`, `name`, `dsn`.

## Issues

### List issues

```
sentry list_issues --organization my-org --project backend --query "is:unresolved level:error" --sort date --per_page 25
```

| Argument       | Type   | Required | Default        | Description                                 |
|----------------|--------|----------|----------------|---------------------------------------------|
| `organization` | string | yes      |                | Organization slug                           |
| `project`      | string | yes      |                | Project slug                                |
| `query`        | string | no       | `is:unresolved`| Search query (Sentry search syntax)         |
| `sort`         | string | no       | `date`         | `date`, `new`, `priority`, `freq`, `users`  |
| `per_page`     | int    | no       | 25             | Results per page (1-100)                    |
| `cursor`       | string | no       |                | Pagination cursor                           |

Returns: list with `id`, `title`, `culprit`, `level`, `status`, `count`, `user_count`, `first_seen`, `last_seen`, `permalink`.

### Get issue

```
sentry get_issue --issue_id 12345
```

| Argument   | Type   | Required | Description |
|------------|--------|----------|-------------|
| `issue_id` | string | yes      | Issue ID    |

Returns: `id`, `title`, `culprit`, `level`, `status`, `count`, `user_count`, `first_seen`, `last_seen`, `metadata`, `tags`, `assigned_to`, `permalink`.

### Resolve issue

```
sentry resolve_issue --issue_id 12345 --status resolved
```

| Argument   | Type   | Required | Default    | Description                                      |
|------------|--------|----------|------------|--------------------------------------------------|
| `issue_id` | string | yes      |            | Issue ID                                         |
| `status`   | string | no       | `resolved` | `resolved`, `resolvedInNextRelease`              |

Returns: `id`, `status`.

### Ignore issue

```
sentry ignore_issue --issue_id 12345 --ignore_count 100 --ignore_window 60
```

| Argument        | Type   | Required | Default | Description                                 |
|-----------------|--------|----------|---------|---------------------------------------------|
| `issue_id`      | string | yes      |         | Issue ID                                    |
| `ignore_count`  | int    | no       |         | Ignore until seen this many more times      |
| `ignore_window` | int    | no       |         | Time window in minutes for ignore_count     |
| `ignore_until`  | string | no       |         | ISO 8601 timestamp to ignore until          |

Returns: `id`, `status`.

### Assign issue

```
sentry assign_issue --issue_id 12345 --assignee user:jane@example.com
```

| Argument   | Type   | Required | Description                                         |
|------------|--------|----------|-----------------------------------------------------|
| `issue_id` | string | yes      | Issue ID                                            |
| `assignee` | string | yes      | `user:<email>`, `team:<slug>`, or empty to unassign |

Returns: `id`, `assigned_to`.

### Delete issue

```
sentry delete_issue --issue_id 12345
```

| Argument   | Type   | Required | Description |
|------------|--------|----------|-------------|
| `issue_id` | string | yes      | Issue ID    |

Returns: confirmation with `id`.

## Events

### List events

```
sentry list_events --organization my-org --project backend --per_page 10
```

| Argument       | Type   | Required | Default | Description           |
|----------------|--------|----------|---------|-----------------------|
| `organization` | string | yes      |         | Organization slug     |
| `project`      | string | yes      |         | Project slug          |
| `per_page`     | int    | no       | 25      | Results per page      |
| `cursor`       | string | no       |         | Pagination cursor     |

Returns: list with `event_id`, `title`, `message`, `level`, `platform`, `timestamp`, `tags`.

### Get event

```
sentry get_event --organization my-org --project backend --event_id abc123
```

| Argument       | Type   | Required | Description       |
|----------------|--------|----------|-------------------|
| `organization` | string | yes      | Organization slug |
| `project`      | string | yes      | Project slug      |
| `event_id`     | string | yes      | Event ID          |

Returns: full event detail including `exception`, `stacktrace`, `breadcrumbs`, `contexts`, `tags`, `user`, `request`.

### Get latest event

```
sentry get_latest_event --issue_id 12345
```

| Argument   | Type   | Required | Description |
|------------|--------|----------|-------------|
| `issue_id` | string | yes      | Issue ID    |

Returns: most recent event for the issue with full detail (same as `get_event`).

## Releases

### List releases

```
sentry list_releases --organization my-org --project backend --per_page 10
```

| Argument       | Type   | Required | Default | Description           |
|----------------|--------|----------|---------|-----------------------|
| `organization` | string | yes      |         | Organization slug     |
| `project`      | string | no       |         | Filter by project     |
| `per_page`     | int    | no       | 25      | Results per page      |

Returns: list with `version`, `short_version`, `date_created`, `date_released`, `new_groups`, `authors`, `commit_count`, `deploy_count`.

### Create release

```
sentry create_release --organization my-org --version "v1.2.0" --projects '["backend","dashboard"]' --ref main
```

| Argument       | Type     | Required | Description                             |
|----------------|----------|----------|-----------------------------------------|
| `organization` | string   | yes      | Organization slug                       |
| `version`      | string   | yes      | Release version (e.g. `v1.2.0`, SHA)   |
| `projects`     | string[] | yes      | Project slugs to associate              |
| `ref`          | string   | no       | Git ref (branch or SHA)                 |
| `url`          | string   | no       | URL for the release (e.g. changelog)    |

Returns: `version`, `date_created`, `projects`.

### Finalize release

```
sentry finalize_release --organization my-org --version "v1.2.0"
```

| Argument       | Type   | Required | Description       |
|----------------|--------|----------|-------------------|
| `organization` | string | yes      | Organization slug |
| `version`      | string | yes      | Release version   |

Returns: `version`, `date_released`.

### Delete release

```
sentry delete_release --organization my-org --version "v1.2.0"
```

| Argument       | Type   | Required | Description       |
|----------------|--------|----------|-------------------|
| `organization` | string | yes      | Organization slug |
| `version`      | string | yes      | Release version   |

Returns: confirmation with `version`.

### Set commits

```
sentry set_commits --organization my-org --version "v1.2.0" --repository "HarKro753/EnterpriseAgentOs" --commit abc123def
```

| Argument       | Type   | Required | Description                          |
|----------------|--------|----------|--------------------------------------|
| `organization` | string | yes      | Organization slug                    |
| `version`      | string | yes      | Release version                      |
| `repository`   | string | yes      | Repository name (`owner/repo`)       |
| `commit`       | string | yes      | Head commit SHA                      |
| `prev_commit`  | string | no       | Previous release commit SHA          |

Returns: `version`, `commit_count`.

## Deploys

### List deploys

```
sentry list_deploys --organization my-org --version "v1.2.0"
```

| Argument       | Type   | Required | Description       |
|----------------|--------|----------|-------------------|
| `organization` | string | yes      | Organization slug |
| `version`      | string | yes      | Release version   |

Returns: list with `id`, `environment`, `name`, `date_started`, `date_finished`.

### Create deploy

```
sentry create_deploy --organization my-org --version "v1.2.0" --environment production --name "Production deploy"
```

| Argument       | Type   | Required | Description                    |
|----------------|--------|----------|--------------------------------|
| `organization` | string | yes      | Organization slug              |
| `version`      | string | yes      | Release version                |
| `environment`  | string | yes      | Environment name (e.g. `production`, `staging`) |
| `name`         | string | no       | Deploy name or description     |
| `url`          | string | no       | URL for the deploy             |

Returns: `id`, `environment`, `date_started`, `date_finished`.

## Source Maps

### Upload source maps

```
sentry upload_sourcemaps --organization my-org --project dashboard --version "v1.2.0" --path ./dist --url_prefix "~/static/js"
```

| Argument       | Type   | Required | Default | Description                             |
|----------------|--------|----------|---------|-----------------------------------------|
| `organization` | string | yes      |         | Organization slug                       |
| `project`      | string | yes      |         | Project slug                            |
| `version`      | string | yes      |         | Release version                         |
| `path`         | string | yes      |         | Directory containing source maps        |
| `url_prefix`   | string | no       | `~`     | URL prefix to prepend to filenames      |

Returns: `files_uploaded`, `version`.

## Alerts

### List alert rules

```
sentry list_alert_rules --organization my-org --project backend
```

| Argument       | Type   | Required | Description       |
|----------------|--------|----------|-------------------|
| `organization` | string | yes      | Organization slug |
| `project`      | string | yes      | Project slug      |

Returns: list with `id`, `name`, `conditions`, `actions`, `frequency`, `date_created`.

### Create alert rule

```
sentry create_alert_rule --organization my-org --project backend --name "High error rate" --conditions '[{"id":"sentry.rules.conditions.event_frequency.EventFrequencyCondition","value":100,"interval":"1h"}]' --actions '[{"id":"sentry.rules.actions.notify_event.NotifyEventAction"}]' --frequency 60
```

| Argument       | Type   | Required | Default | Description                                    |
|----------------|--------|----------|---------|------------------------------------------------|
| `organization` | string | yes      |         | Organization slug                              |
| `project`      | string | yes      |         | Project slug                                   |
| `name`         | string | yes      |         | Alert rule name                                |
| `conditions`   | string | yes      |         | JSON array of condition objects                |
| `actions`      | string | yes      |         | JSON array of action objects                   |
| `frequency`    | int    | no       | 30      | Minutes between alerts for the same issue      |
| `environment`  | string | no       |         | Filter to environment                          |

Returns: `id`, `name`, `conditions`, `actions`.

### Delete alert rule

```
sentry delete_alert_rule --organization my-org --project backend --rule_id 789
```

| Argument       | Type   | Required | Description       |
|----------------|--------|----------|-------------------|
| `organization` | string | yes      | Organization slug |
| `project`      | string | yes      | Project slug      |
| `rule_id`      | string | yes      | Alert rule ID     |

Returns: confirmation with `id`.

## Performance

### List transactions

```
sentry list_transactions --organization my-org --project backend --per_page 10 --query "transaction.duration:>1s"
```

| Argument       | Type   | Required | Default | Description                         |
|----------------|--------|----------|---------|-------------------------------------|
| `organization` | string | yes      |         | Organization slug                   |
| `project`      | string | yes      |         | Project slug                        |
| `query`        | string | no       |         | Search query                        |
| `per_page`     | int    | no       | 25      | Results per page                    |
| `sort`         | string | no       |         | Sort field (e.g. `p50`, `count`)    |

Returns: list with `transaction`, `count`, `p50`, `p75`, `p95`, `failure_rate`, `apdex`.

### Get transaction summary

```
sentry get_transaction_summary --organization my-org --project backend --transaction "GET /api/agents"
```

| Argument       | Type   | Required | Default | Description                   |
|----------------|--------|----------|---------|-------------------------------|
| `organization` | string | yes      |         | Organization slug             |
| `project`      | string | yes      |         | Project slug                  |
| `transaction`  | string | yes      |         | Transaction name              |
| `period`       | string | no       | `24h`   | Time period (e.g. `1h`, `7d`) |

Returns: `transaction`, `count`, `p50`, `p75`, `p95`, `p99`, `failure_rate`, `apdex`, `throughput`, `duration_histogram`.

## Stats

### Get project stats

```
sentry get_project_stats --organization my-org --project backend --stat received --resolution 1h --since -24h
```

| Argument       | Type   | Required | Default    | Description                                 |
|----------------|--------|----------|------------|---------------------------------------------|
| `organization` | string | yes      |            | Organization slug                           |
| `project`      | string | yes      |            | Project slug                                |
| `stat`         | string | no       | `received` | `received`, `rejected`, `blacklisted`       |
| `resolution`   | string | no       | `1h`       | Bucket resolution (e.g. `1h`, `1d`)         |
| `since`        | string | no       | `-24h`     | Start time (relative or ISO 8601)           |
| `until`        | string | no       |            | End time                                    |

Returns: time series of `timestamp` and `count` values.

### Get issue frequency

```
sentry get_issue_frequency --issue_id 12345 --resolution 1h --since -24h
```

| Argument     | Type   | Required | Default | Description                          |
|--------------|--------|----------|---------|--------------------------------------|
| `issue_id`   | string | yes      |         | Issue ID                             |
| `resolution` | string | no       | `1h`    | Bucket resolution (e.g. `1h`, `1d`) |
| `since`      | string | no       | `-24h`  | Start time (relative or ISO 8601)   |
| `until`      | string | no       |         | End time                             |

Returns: time series of `timestamp` and `count` values for the specific issue.

## Workflow

1. **List projects** with `sentry list_projects` to find the project slug.
2. **Triage errors** with `sentry list_issues --query "is:unresolved"` sorted by frequency or user impact.
3. **Investigate an issue** with `sentry get_issue` for metadata, then `sentry get_latest_event` for the full stacktrace and context.
4. **Resolve or assign** issues with `sentry resolve_issue` or `sentry assign_issue`.
5. **Track releases** with `sentry create_release`, `sentry set_commits`, and `sentry create_deploy` during CI/CD.
6. **Upload source maps** after builds with `sentry upload_sourcemaps` so stacktraces show original source.
7. **Set up alerts** with `sentry create_alert_rule` to get notified on error spikes.
8. **Monitor performance** with `sentry list_transactions` and `sentry get_transaction_summary` to find slow endpoints.
9. **Review stats** with `sentry get_project_stats` and `sentry get_issue_frequency` to track error trends over time.

## Safety notes

- `delete_issue` permanently removes the issue and all its events. This cannot be undone. Confirm with the user.
- `delete_release` removes the release record. Existing events are not affected, but deploy tracking is lost.
- Source map uploads are associated with a release version. Ensure the version matches the deployed code exactly.
- Alert rule conditions and actions use Sentry-specific IDs. Use `list_alert_rules` on an existing project to see valid formats.
- Issue IDs are numeric. Always discover them via `list_issues` rather than guessing.
- The `query` parameter for `list_issues` uses Sentry search syntax (e.g. `is:unresolved`, `level:error`, `assigned:me`).
- API rate limits apply. Avoid rapid bulk operations on issues.
