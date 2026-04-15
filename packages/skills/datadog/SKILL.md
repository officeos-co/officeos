# Datadog

Monitor infrastructure, query metrics, manage monitors, dashboards, logs, hosts, events, and downtimes via the Datadog API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Metrics

### Query metrics

```
datadog query_metrics --query "avg:system.cpu.user{host:web-01}" --from 1700000000 --to 1700003600
```

| Argument | Type   | Required | Description                        |
|----------|--------|----------|------------------------------------|
| `query`  | string | yes      | Datadog metrics query string       |
| `from`   | int    | yes      | Start of timeframe (Unix epoch, s) |
| `to`     | int    | yes      | End of timeframe (Unix epoch, s)   |

Returns: `status`, `series` (list of `metric`, `displayName`, `pointlist`, `scope`, `unit`).

### Submit metrics

```
datadog submit_metrics --series '[{"metric":"custom.app.requests","points":[[1700000000,42]],"tags":["env:prod"]}]'
```

| Argument | Type  | Required | Description                                                |
|----------|-------|----------|------------------------------------------------------------|
| `series` | array | yes      | JSON array of metric series objects (metric, points, tags) |

Returns: `status`.

### List metrics

```
datadog list_metrics --from 1700000000 --host "web-01"
```

| Argument | Type   | Required | Description                           |
|----------|--------|----------|---------------------------------------|
| `from`   | int    | yes      | Seconds since POSIX epoch             |
| `host`   | string | no       | Filter metrics by host                |
| `tag_filter` | string | no   | Filter by tag family (e.g. `env`)     |

Returns: list of `metrics` names.

## Monitors

### List monitors

```
datadog list_monitors --tags "env:prod,service:api" --monitor_tags "team:backend"
```

| Argument       | Type   | Required | Description                      |
|----------------|--------|----------|----------------------------------|
| `tags`         | string | no       | Comma-separated scope tags       |
| `monitor_tags` | string | no       | Comma-separated monitor tags     |
| `name`         | string | no       | Filter by name (substring match) |

Returns: list of `id`, `name`, `type`, `status`, `query`, `tags`, `created`, `modified`.

### Get monitor

```
datadog get_monitor --monitor_id 12345
```

| Argument     | Type | Required | Description |
|--------------|------|----------|-------------|
| `monitor_id` | int  | yes      | Monitor ID  |

Returns: `id`, `name`, `type`, `status`, `query`, `message`, `tags`, `options`, `created`, `modified`.

### Create monitor

```
datadog create_monitor --name "High CPU" --type "metric alert" --query "avg(last_5m):avg:system.cpu.user{*} > 90" --message "CPU too high @pagerduty" --tags '["env:prod"]'
```

| Argument  | Type     | Required | Description                                   |
|-----------|----------|----------|-----------------------------------------------|
| `name`    | string   | yes      | Monitor name                                  |
| `type`    | string   | yes      | `metric alert`, `service check`, `event alert` |
| `query`   | string   | yes      | Monitor query                                 |
| `message` | string   | no       | Notification message                          |
| `tags`    | string[] | no       | Tags to apply                                 |
| `options` | object   | no       | Monitor options JSON                          |

Returns: `id`, `name`, `type`, `status`, `query`.

### Update monitor

```
datadog update_monitor --monitor_id 12345 --name "High CPU v2" --message "Updated alert @slack-ops"
```

| Argument     | Type     | Required | Description           |
|--------------|----------|----------|-----------------------|
| `monitor_id` | int      | yes      | Monitor ID            |
| `name`       | string   | no       | Updated name          |
| `query`      | string   | no       | Updated query         |
| `message`    | string   | no       | Updated message       |
| `tags`       | string[] | no       | Updated tags          |
| `options`    | object   | no       | Updated options JSON  |

Returns: `id`, `name`, `status`.

### Mute monitor

```
datadog mute_monitor --monitor_id 12345 --end 1700010000 --scope "host:web-01"
```

| Argument     | Type   | Required | Description                                |
|--------------|--------|----------|--------------------------------------------|
| `monitor_id` | int    | yes      | Monitor ID                                 |
| `scope`      | string | no       | Scope to mute (e.g. `host:web-01`)         |
| `end`        | int    | no       | End of mute window (Unix epoch, s)         |

Returns: `id`, `name`, `options`.

### Delete monitor

```
datadog delete_monitor --monitor_id 12345
```

| Argument     | Type | Required | Description |
|--------------|------|----------|-------------|
| `monitor_id` | int  | yes      | Monitor ID  |

Returns: `deleted_monitor_id`.

## Dashboards

### List dashboards

```
datadog list_dashboards --filter_shared false
```

| Argument        | Type    | Required | Description                        |
|-----------------|---------|----------|------------------------------------|
| `filter_shared` | boolean | no       | Return only shared dashboards      |

Returns: list of `id`, `title`, `layout_type`, `url`, `created_at`, `modified_at`, `author_handle`.

### Get dashboard

```
datadog get_dashboard --dashboard_id "abc-def-ghi"
```

| Argument       | Type   | Required | Description  |
|----------------|--------|----------|--------------|
| `dashboard_id` | string | yes      | Dashboard ID |

Returns: `id`, `title`, `description`, `layout_type`, `url`, `widgets`, `template_variables`, `created_at`, `modified_at`.

## Logs

### Search logs

```
datadog search_logs --query "service:api status:error" --from "2024-01-15T00:00:00Z" --to "2024-01-15T01:00:00Z" --limit 50
```

| Argument | Type   | Required | Default | Description                             |
|----------|--------|----------|---------|-----------------------------------------|
| `query`  | string | yes      |         | Datadog log search query                |
| `from`   | string | yes      |         | Start time (ISO 8601)                   |
| `to`     | string | yes      |         | End time (ISO 8601)                     |
| `limit`  | int    | no       | 25      | Max results (1-1000)                    |
| `sort`   | string | no       | `-timestamp` | `timestamp` or `-timestamp`        |

Returns: list of `id`, `attributes` (timestamp, host, service, status, message, tags).

### Aggregate logs

```
datadog aggregate_logs --query "service:api" --from "2024-01-15T00:00:00Z" --to "2024-01-15T01:00:00Z" --group_by '["service","status"]' --compute '[{"aggregation":"count","type":"total"}]'
```

| Argument   | Type     | Required | Description                             |
|------------|----------|----------|-----------------------------------------|
| `query`    | string   | yes      | Filter query                            |
| `from`     | string   | yes      | Start time (ISO 8601)                   |
| `to`       | string   | yes      | End time (ISO 8601)                     |
| `group_by` | string[] | no       | Fields to group by                      |
| `compute`  | array    | no       | Aggregation compute specs               |

Returns: `buckets` with grouped results and aggregated values.

## Hosts

### List hosts

```
datadog list_hosts --filter "env:prod" --count 100
```

| Argument         | Type   | Required | Default | Description                        |
|------------------|--------|----------|---------|------------------------------------|
| `filter`         | string | no       |         | Filter string (e.g. `env:prod`)    |
| `sort_field`     | string | no       |         | Sort field                         |
| `sort_dir`       | string | no       |         | `asc` or `desc`                    |
| `start`          | int    | no       | 0       | Offset for pagination              |
| `count`          | int    | no       | 100     | Max results                        |
| `include_muted_hosts_data` | boolean | no | true | Include muted hosts              |

Returns: `total_returned`, `total_matching`, list of `name`, `id`, `aliases`, `apps`, `tags_by_source`, `is_muted`, `up`, `last_reported_time`.

### Get host totals

```
datadog get_host_totals
```

No arguments required.

Returns: `total_active`, `total_up`.

## Events

### List events

```
datadog list_events --start 1700000000 --end 1700003600 --priority "normal" --tags "env:prod"
```

| Argument    | Type   | Required | Description                         |
|-------------|--------|----------|-------------------------------------|
| `start`     | int    | yes      | Start of timeframe (Unix epoch, s)  |
| `end`       | int    | yes      | End of timeframe (Unix epoch, s)    |
| `priority`  | string | no       | `normal` or `low`                   |
| `sources`   | string | no       | Comma-separated event sources       |
| `tags`      | string | no       | Comma-separated tags                |

Returns: list of `id`, `title`, `text`, `date_happened`, `host`, `tags`, `alert_type`, `source`.

### Create event

```
datadog create_event --title "Deploy completed" --text "v1.2.3 deployed to prod" --tags '["env:prod","service:api"]' --alert_type "success"
```

| Argument       | Type     | Required | Default | Description                                            |
|----------------|----------|----------|---------|--------------------------------------------------------|
| `title`        | string   | yes      |         | Event title                                            |
| `text`         | string   | yes      |         | Event body                                             |
| `date_happened`| int      | no       |         | POSIX timestamp (defaults to now)                      |
| `host`         | string   | no       |         | Host name to associate                                 |
| `tags`         | string[] | no       |         | Tags to apply                                          |
| `alert_type`   | string   | no       | `info`  | `error`, `warning`, `info`, `success`                  |
| `priority`     | string   | no       | `normal`| `normal` or `low`                                      |
| `source_type_name` | string | no     |         | Source type                                            |

Returns: `status`, `event.id`, `event.url`.

## Downtimes

### List downtimes

```
datadog list_downtimes --current_only true
```

| Argument       | Type    | Required | Description                           |
|----------------|---------|----------|---------------------------------------|
| `current_only` | boolean | no       | Return only currently active downtimes|

Returns: list of `id`, `scope`, `message`, `start`, `end`, `monitor_id`, `monitor_tags`, `active`, `disabled`, `creator_id`.

### Create downtime

```
datadog create_downtime --scope "host:web-01" --start 1700000000 --end 1700007200 --message "Scheduled maintenance" --monitor_tags '["env:prod"]'
```

| Argument       | Type     | Required | Description                                        |
|----------------|----------|----------|----------------------------------------------------|
| `scope`        | string   | yes      | Scope to silence (e.g. `host:web-01`, `*`)         |
| `start`        | int      | no       | Start time (Unix epoch, defaults to now)           |
| `end`          | int      | no       | End time (Unix epoch, omit for indefinite)         |
| `message`      | string   | no       | Downtime message                                   |
| `monitor_id`   | int      | no       | Silence a specific monitor only                    |
| `monitor_tags` | string[] | no       | Silence monitors matching these tags               |
| `recurrence`   | object   | no       | Recurrence spec JSON                               |

Returns: `id`, `scope`, `start`, `end`, `active`.

### Cancel downtime

```
datadog cancel_downtime --downtime_id 67890
```

| Argument      | Type | Required | Description |
|---------------|------|----------|-------------|
| `downtime_id` | int  | yes      | Downtime ID |

Returns: `{}` (204 No Content on success).

## Workflow

1. **Check infrastructure health** with `list_hosts` and `get_host_totals`.
2. **Query metrics** with `query_metrics` to investigate performance issues.
3. **Search logs** with `search_logs` to find error patterns.
4. **Review monitors** with `list_monitors` to see alerting state.
5. **Mute noisy monitors** with `mute_monitor` during investigations.
6. **Schedule maintenance** with `create_downtime` before changes.
7. **Mark deployments** with `create_event` to correlate with metric changes.
8. **Create dashboards** to visualise key metrics and service health.

## Safety notes

- `delete_monitor` is irreversible. Prefer muting with `mute_monitor` for temporary silencing.
- Downtime scope `"*"` silences ALL monitors. Use specific scopes in production.
- Metrics submitted via `submit_metrics` count toward your custom metrics quota.
- The `site` credential must match your Datadog region (e.g. `datadoghq.com`, `datadoghq.eu`, `us3.datadoghq.com`).
- API rate limits vary by endpoint; metrics ingestion allows up to 500,000 points per minute.
