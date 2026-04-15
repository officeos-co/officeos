# Grafana

Full Grafana HTTP API coverage: manage dashboards, panels, datasources, alerts, annotations, folders, users, orgs, and search via the Grafana REST API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Dashboard operations

### List dashboards

```
grafana list_dashboards --query "production" --tag monitoring --folder_id 1
```

| Argument    | Type   | Required | Default | Description                   |
|-------------|--------|----------|---------|-------------------------------|
| `query`     | string | no       |         | Search query string           |
| `tag`       | string | no       |         | Filter by tag                 |
| `folder_id` | int   | no       |         | Filter by folder ID           |

### Get dashboard

```
grafana get_dashboard --uid abc123
```

| Argument | Type   | Required | Description     |
|----------|--------|----------|-----------------|
| `uid`    | string | yes      | Dashboard UID   |

Returns: `uid`, `title`, `url`, `tags`, `panels`, `version`, `folder_id`, `folder_title`.

### Create dashboard

```
grafana create_dashboard --title "My Dashboard" --folder_id 0 --panels '[]' --tags '["prod"]'
```

| Argument    | Type     | Required | Default | Description                    |
|-------------|----------|----------|---------|--------------------------------|
| `title`     | string   | yes      |         | Dashboard title                |
| `folder_id` | int     | no       | 0       | Target folder ID (0 = General) |
| `panels`    | json     | no       | `[]`    | Array of panel definitions     |
| `tags`      | string[] | no       | `[]`    | Tags to apply                  |
| `overwrite` | boolean  | no       | false   | Overwrite existing dashboard   |

### Update dashboard

```
grafana update_dashboard --uid abc123 --title "Updated Title" --panels '[...]' --version 3
```

| Argument    | Type     | Required | Description                           |
|-------------|----------|----------|---------------------------------------|
| `uid`       | string   | yes      | Dashboard UID                         |
| `title`     | string   | no       | New title                             |
| `panels`    | json     | no       | Updated panel definitions             |
| `tags`      | string[] | no       | Updated tags                          |
| `folder_id` | int     | no       | Move to folder                        |
| `version`   | int      | yes      | Current version (for conflict check)  |
| `overwrite` | boolean  | no       | Overwrite without version check       |

### Delete dashboard

```
grafana delete_dashboard --uid abc123
```

| Argument | Type   | Required | Description     |
|----------|--------|----------|-----------------|
| `uid`    | string | yes      | Dashboard UID   |

## Panel operations

### Get panel data

```
grafana get_panel_data --dashboard_uid abc123 --panel_id 2 --from "now-1h" --to "now"
```

| Argument        | Type   | Required | Default    | Description          |
|-----------------|--------|----------|------------|----------------------|
| `dashboard_uid` | string | yes      |            | Dashboard UID        |
| `panel_id`      | int    | yes      |            | Panel ID             |
| `from`          | string | no       | `now-1h`   | Start time           |
| `to`            | string | no       | `now`      | End time             |

## Datasource operations

### List datasources

```
grafana list_datasources
```

No arguments required.

### Get datasource

```
grafana get_datasource --uid ds-uid-123
```

| Argument | Type   | Required | Description      |
|----------|--------|----------|------------------|
| `uid`    | string | yes      | Datasource UID   |

Returns: `uid`, `name`, `type`, `url`, `access`, `is_default`, `database`, `json_data`.

### Create datasource

```
grafana create_datasource --name "Prometheus" --type prometheus --url "http://prom:9090" --access proxy
```

| Argument     | Type    | Required | Default | Description                      |
|--------------|---------|----------|---------|----------------------------------|
| `name`       | string  | yes      |         | Datasource name                  |
| `type`       | string  | yes      |         | Datasource type (prometheus, etc)|
| `url`        | string  | yes      |         | Datasource URL                   |
| `access`     | string  | no       | `proxy` | `proxy` or `direct`              |
| `is_default` | boolean | no       | false   | Set as default datasource        |
| `database`   | string  | no       |         | Database name (if applicable)    |
| `json_data`  | json    | no       |         | Additional type-specific config  |

### Delete datasource

```
grafana delete_datasource --uid ds-uid-123
```

| Argument | Type   | Required | Description      |
|----------|--------|----------|------------------|
| `uid`    | string | yes      | Datasource UID   |

### Test datasource

```
grafana test_datasource --uid ds-uid-123
```

| Argument | Type   | Required | Description      |
|----------|--------|----------|------------------|
| `uid`    | string | yes      | Datasource UID   |

Returns: `status`, `message`.

## Query operations

### Query datasource

```
grafana query_datasource --datasource_uid ds-uid-123 --expr "up{job=\"prometheus\"}" --from "now-1h" --to "now" --interval "15s"
```

| Argument         | Type   | Required | Default  | Description                        |
|------------------|--------|----------|----------|------------------------------------|
| `datasource_uid` | string | yes      |          | Datasource UID                     |
| `expr`           | string | yes      |          | Query expression (PromQL, SQL, etc)|
| `from`           | string | no       | `now-1h` | Start time                         |
| `to`             | string | no       | `now`    | End time                           |
| `interval`       | string | no       |          | Step interval (e.g. `15s`, `1m`)   |

## Alert operations

### List alert rules

```
grafana list_alert_rules
```

No arguments required.

### Get alert rule

```
grafana get_alert_rule --uid alert-uid-123
```

| Argument | Type   | Required | Description      |
|----------|--------|----------|------------------|
| `uid`    | string | yes      | Alert rule UID   |

Returns: `uid`, `title`, `condition`, `data`, `no_data_state`, `exec_err_state`, `folder_uid`, `for`, `labels`, `annotations`.

### Create alert rule

```
grafana create_alert_rule --title "High CPU" --folder_uid folder-123 --condition C --data '[...]' --for "5m" --no_data_state NoData --exec_err_state Error
```

| Argument         | Type   | Required | Default    | Description                      |
|------------------|--------|----------|------------|----------------------------------|
| `title`          | string | yes      |            | Alert rule title                 |
| `folder_uid`     | string | yes      |            | Folder UID for the rule          |
| `condition`      | string | yes      |            | Condition ref ID (e.g. `C`)      |
| `data`           | json   | yes      |            | Array of query/condition objects  |
| `for`            | string | no       | `5m`       | Pending duration before firing   |
| `no_data_state`  | string | no       | `NoData`   | `NoData`, `Alerting`, `OK`       |
| `exec_err_state` | string | no       | `Error`    | `Error`, `Alerting`, `OK`        |
| `labels`         | json   | no       |            | Additional labels                |
| `annotations`    | json   | no       |            | Additional annotations           |

### Delete alert rule

```
grafana delete_alert_rule --uid alert-uid-123
```

| Argument | Type   | Required | Description      |
|----------|--------|----------|------------------|
| `uid`    | string | yes      | Alert rule UID   |

### List alert instances

```
grafana list_alert_instances
```

No arguments required. Returns all firing and pending alert instances.

### Pause alert rule

```
grafana pause_alert_rule --uid alert-uid-123 --paused true
```

| Argument | Type    | Required | Description                |
|----------|---------|----------|----------------------------|
| `uid`    | string  | yes      | Alert rule UID             |
| `paused` | boolean | yes      | true to pause, false to resume |

## Annotation operations

### List annotations

```
grafana list_annotations --from 1609459200000 --to 1609545600000 --dashboard_id 1 --tags '["deploy"]'
```

| Argument       | Type     | Required | Default | Description                 |
|----------------|----------|----------|---------|-----------------------------|
| `from`         | int      | no       |         | Epoch ms start              |
| `to`           | int      | no       |         | Epoch ms end                |
| `dashboard_id` | int      | no       |         | Filter by dashboard ID      |
| `panel_id`     | int      | no       |         | Filter by panel ID          |
| `tags`         | string[] | no       |         | Filter by tags              |

### Create annotation

```
grafana create_annotation --text "Deployed v1.2.3" --tags '["deploy"]' --dashboard_id 1
```

| Argument       | Type     | Required | Description              |
|----------------|----------|----------|--------------------------|
| `text`         | string   | yes      | Annotation text          |
| `tags`         | string[] | no       | Tags                     |
| `dashboard_id` | int      | no       | Associate with dashboard |
| `panel_id`     | int      | no       | Associate with panel     |
| `time`         | int      | no       | Epoch ms (default: now)  |
| `time_end`     | int      | no       | Epoch ms end (regions)   |

### Delete annotation

```
grafana delete_annotation --id 42
```

| Argument | Type | Required | Description    |
|----------|------|----------|----------------|
| `id`     | int  | yes      | Annotation ID  |

## Folder operations

### List folders

```
grafana list_folders
```

No arguments required.

### Create folder

```
grafana create_folder --title "Production Dashboards" --uid "prod-dash"
```

| Argument | Type   | Required | Description              |
|----------|--------|----------|--------------------------|
| `title`  | string | yes      | Folder title             |
| `uid`    | string | no       | Custom UID (auto if omitted) |

### Delete folder

```
grafana delete_folder --uid prod-dash
```

| Argument | Type   | Required | Description |
|----------|--------|----------|-------------|
| `uid`    | string | yes      | Folder UID  |

## User operations

### List users

```
grafana list_users --per_page 20
```

| Argument   | Type | Required | Default | Description              |
|------------|------|----------|---------|--------------------------|
| `per_page` | int  | no       | 100     | Results per page (1-1000)|

### Get user

```
grafana get_user --id 1
```

| Argument | Type | Required | Description |
|----------|------|----------|-------------|
| `id`     | int  | yes      | User ID     |

Returns: `id`, `login`, `name`, `email`, `is_admin`, `is_disabled`, `last_seen_at`.

## Org operations

### Get org

```
grafana get_org
```

No arguments required. Returns the current organization.

### List org users

```
grafana list_org_users
```

No arguments required. Returns users in the current organization.

## Search

### Search dashboards and folders

```
grafana search --query "production" --type "dash-db" --tag "monitoring"
```

| Argument | Type   | Required | Default | Description                               |
|----------|--------|----------|---------|-------------------------------------------|
| `query`  | string | no       |         | Search query                              |
| `type`   | string | no       |         | `dash-db` (dashboards) or `dash-folder`   |
| `tag`    | string | no       |         | Filter by tag                             |

## Workflow

1. Start with `grafana search` or `grafana list_dashboards` to discover dashboards.
2. Use `grafana get_dashboard` to inspect a specific dashboard and its panels.
3. Use `grafana get_panel_data` or `grafana query_datasource` to retrieve metric data.
4. Manage datasources: list, create, test, and delete.
5. Monitor alerts: list rules, check instances, create new rules, pause/resume.
6. Use annotations to mark events (deploys, incidents) on dashboards.
7. Organize dashboards with folders.

## Safety notes

- Write operations (create, update, delete) require an API key or Service Account token with appropriate permissions.
- Admin-level operations (users, orgs) require Admin role.
- Dashboard updates use version-based conflict detection. Always pass the current `version` unless using `overwrite: true`.
- Query results may be large. Use `from`/`to` parameters to limit the time range.
