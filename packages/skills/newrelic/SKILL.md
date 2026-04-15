# New Relic

Run NRQL queries, inspect applications, manage alert policies and conditions, dashboards, synthetics monitors, and deployments via the New Relic NerdGraph and REST APIs.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## NRQL Queries

### Run NRQL query

```
newrelic nrql --query "SELECT count(*) FROM Transaction WHERE appName = 'my-app' SINCE 1 hour ago"
```

| Argument  | Type   | Required | Description                            |
|-----------|--------|----------|----------------------------------------|
| `query`   | string | yes      | NRQL query string                      |
| `timeout` | int    | no       | Timeout in seconds (default 5)         |

Returns: `results` (array), `metadata` (facets, eventTypes, messages, performanceStats), `rawResponse`.

### Run async NRQL query

```
newrelic nrql_async --query "SELECT count(*) FROM Log SINCE 7 days ago" --timeout 60
```

| Argument  | Type   | Required | Description                   |
|-----------|--------|----------|-------------------------------|
| `query`   | string | yes      | NRQL query string             |
| `timeout` | int    | no       | Max wait seconds (default 60) |

Returns: `results`, `metadata`.

## Applications (REST v2)

### List applications

```
newrelic list_applications --filter_name "api-service" --limit 25
```

| Argument       | Type   | Required | Description                       |
|----------------|--------|----------|-----------------------------------|
| `filter_name`  | string | no       | Filter by application name        |
| `filter_language` | string | no    | Filter by language (ruby, java, etc.) |
| `limit`        | int    | no       | Max results (default 25)          |

Returns: list of `id`, `name`, `language`, `health_status`, `reporting`, `last_reported_at`, `application_summary`.

### Get application

```
newrelic get_application --app_id 123456789
```

| Argument | Type | Required | Description    |
|----------|------|----------|----------------|
| `app_id` | int  | yes      | Application ID |

Returns: `id`, `name`, `language`, `health_status`, `reporting`, `settings`, `links`, `application_summary`.

## Alert Policies

### List alert policies

```
newrelic list_alert_policies --cursor ""
```

| Argument | Type   | Required | Description                  |
|----------|--------|----------|------------------------------|
| `cursor` | string | no       | Pagination cursor            |
| `name`   | string | no       | Filter by policy name        |

Returns: list of `id`, `name`, `incidentPreference`, `nextCursor` for pagination.

### Get alert policy

```
newrelic get_alert_policy --policy_id "MTIz"
```

| Argument    | Type   | Required | Description   |
|-------------|--------|----------|---------------|
| `policy_id` | string | yes      | Policy ID     |

Returns: `id`, `name`, `incidentPreference`, `conditions` count.

### Create alert policy

```
newrelic create_alert_policy --name "High Error Rate" --incident_preference PER_CONDITION
```

| Argument              | Type   | Required | Default           | Description                                            |
|-----------------------|--------|----------|-------------------|--------------------------------------------------------|
| `name`                | string | yes      |                   | Policy name                                            |
| `incident_preference` | string | no       | `PER_POLICY`      | `PER_POLICY`, `PER_CONDITION`, `PER_CONDITION_AND_TARGET` |

Returns: `id`, `name`, `incidentPreference`.

## Alert Conditions

### List NRQL alert conditions

```
newrelic list_alert_conditions --policy_id "MTIz"
```

| Argument    | Type   | Required | Description             |
|-------------|--------|----------|-------------------------|
| `policy_id` | string | no       | Filter by policy ID     |
| `cursor`    | string | no       | Pagination cursor       |

Returns: list of `id`, `name`, `enabled`, `type`, `nrql.query`, `signal`, `terms` (threshold config).

### Create NRQL alert condition

```
newrelic create_alert_condition --policy_id "MTIz" --name "Error rate > 5%" --nrql "SELECT percentage(count(*), WHERE error IS true) FROM Transaction WHERE appName='api'" --critical_threshold 5 --warning_threshold 3
```

| Argument             | Type   | Required | Description                                             |
|----------------------|--------|----------|---------------------------------------------------------|
| `policy_id`          | string | yes      | Policy ID to attach the condition to                    |
| `name`               | string | yes      | Condition name                                          |
| `nrql`               | string | yes      | NRQL query for the condition                            |
| `critical_threshold` | number | yes      | Critical alert threshold value                          |
| `warning_threshold`  | number | no       | Warning alert threshold value                           |
| `aggregation_window` | int    | no       | Aggregation window in seconds (default 60)              |
| `operator`           | string | no       | `ABOVE`, `BELOW`, `EQUALS` (default `ABOVE`)            |
| `fill_option`        | string | no       | `NONE`, `LAST_VALUE`, `STATIC`                          |

Returns: `id`, `name`, `enabled`, `nrql`.

## Dashboards

### List dashboards

```
newrelic list_dashboards --name "Operations"
```

| Argument | Type   | Required | Description                  |
|----------|--------|----------|------------------------------|
| `name`   | string | no       | Filter by dashboard name     |
| `cursor` | string | no       | Pagination cursor            |

Returns: list of `guid`, `name`, `description`, `createdAt`, `updatedAt`, `permalink`.

### Get dashboard

```
newrelic get_dashboard --guid "MXxxxYZZ"
```

| Argument | Type   | Required | Description      |
|----------|--------|----------|------------------|
| `guid`   | string | yes      | Dashboard entity GUID |

Returns: `guid`, `name`, `description`, `pages` (list of page name + widgets), `createdAt`, `updatedAt`, `permalink`.

### Create dashboard

```
newrelic create_dashboard --name "API Health" --description "Key API metrics" --pages '[{"name":"Overview","widgets":[]}]'
```

| Argument      | Type   | Required | Description                   |
|---------------|--------|----------|-------------------------------|
| `name`        | string | yes      | Dashboard name                |
| `description` | string | no       | Dashboard description         |
| `pages`       | array  | no       | Array of page definitions     |
| `permissions` | string | no       | `PUBLIC_READ_ONLY`, `PUBLIC_READ_WRITE`, `PRIVATE` |

Returns: `guid`, `name`, `permalink`.

## Synthetics

### List synthetic monitors

```
newrelic list_synthetic_monitors --cursor ""
```

| Argument | Type   | Required | Description       |
|----------|--------|----------|-------------------|
| `cursor` | string | no       | Pagination cursor |
| `name`   | string | no       | Filter by name    |

Returns: list of `guid`, `name`, `monitorType`, `status`, `period`, `locations`, `uri`.

### Get synthetic monitor

```
newrelic get_synthetic_monitor --guid "MXxxxYZZ"
```

| Argument | Type   | Required | Description       |
|----------|--------|----------|-------------------|
| `guid`   | string | yes      | Monitor GUID      |

Returns: `guid`, `name`, `monitorType`, `status`, `period`, `locations`, `uri`, `tags`.

## Deployments

### Record deployment

```
newrelic record_deployment --entity_guid "MXxxxYZZ" --version "v1.2.3" --description "Deployed to prod" --user "deploy-bot"
```

| Argument       | Type   | Required | Description                              |
|----------------|--------|----------|------------------------------------------|
| `entity_guid`  | string | yes      | Entity GUID of the application           |
| `version`      | string | yes      | Deployment version/tag                   |
| `description`  | string | no       | Deployment description                   |
| `user`         | string | no       | User or system that performed deployment |
| `changelog`    | string | no       | Changelog text                           |
| `commit`       | string | no       | Git commit SHA                           |

Returns: `id`, `version`, `timestamp`, `entityGuid`.

### List deployments

```
newrelic list_deployments --entity_guid "MXxxxYZZ" --limit 10
```

| Argument      | Type   | Required | Description                              |
|---------------|--------|----------|------------------------------------------|
| `entity_guid` | string | yes      | Entity GUID                              |
| `limit`       | int    | no       | Max results (default 10)                 |

Returns: list of `id`, `version`, `description`, `user`, `timestamp`, `changelog`.

## Workflow

1. **Diagnose issues** with `nrql --query "SELECT * FROM TransactionError LIMIT 10"`.
2. **Check application health** with `list_applications` and `get_application`.
3. **Review active alerts** with `list_alert_policies` and `list_alert_conditions`.
4. **Correlate deployments** with `list_deployments` against metric anomalies.
5. **Monitor uptime** with `list_synthetic_monitors` and check failures with NRQL.
6. **Create dashboards** with `create_dashboard` to surface key metrics.
7. **Record deployments** with `record_deployment` to track changes in New Relic.

## Safety notes

- `create_alert_condition` will immediately start evaluating when `enabled: true`. Verify thresholds before creating in production.
- `create_dashboard` with `permissions: PUBLIC_READ_WRITE` allows anyone with the link to edit. Default to `PRIVATE` or `PUBLIC_READ_ONLY`.
- NerdGraph rate limit: 3,000 requests/minute per account.
- NRQL queries on large datasets with wide time windows can be slow. Use `LIMIT` and narrow time windows during investigation.
- The `api_key` must be a **User API key** (prefix `NRAK-`), not a license/ingest key.
