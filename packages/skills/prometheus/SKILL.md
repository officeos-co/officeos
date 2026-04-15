# Prometheus

Query metrics, explore series, list labels, inspect targets, rules, and alerts via the Prometheus HTTP API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Querying

### Instant query

```
prometheus query --expr "up" --time "2024-01-15T14:00:00Z"
```

| Argument  | Type   | Required | Description                                  |
|-----------|--------|----------|----------------------------------------------|
| `expr`    | string | yes      | PromQL expression                            |
| `time`    | string | no       | Evaluation timestamp (RFC 3339 or Unix secs) |
| `timeout` | string | no       | Evaluation timeout (e.g. `30s`)              |

Returns: `resultType` (`vector`, `scalar`, `string`, `matrix`), `result` array of `{metric, value}`.

### Range query

```
prometheus query_range --expr "rate(http_requests_total[5m])" --start "2024-01-15T00:00:00Z" --end "2024-01-15T01:00:00Z" --step "60s"
```

| Argument  | Type   | Required | Description                                      |
|-----------|--------|----------|--------------------------------------------------|
| `expr`    | string | yes      | PromQL expression                                |
| `start`   | string | yes      | Start time (RFC 3339 or Unix secs)               |
| `end`     | string | yes      | End time (RFC 3339 or Unix secs)                 |
| `step`    | string | yes      | Query resolution step (e.g. `60s`, `5m`, `1h`)  |
| `timeout` | string | no       | Evaluation timeout                               |

Returns: `resultType` (`matrix`), `result` array of `{metric, values}` where values are `[timestamp, value]` pairs.

## Series and Labels

### Find series

```
prometheus series --match '["up","node_cpu_seconds_total{job=\"node\"}"]' --start "2024-01-15T00:00:00Z" --end "2024-01-15T01:00:00Z"
```

| Argument | Type     | Required | Description                                   |
|----------|----------|----------|-----------------------------------------------|
| `match`  | string[] | yes      | Series selectors (PromQL label matchers)      |
| `start`  | string   | no       | Start time                                    |
| `end`    | string   | no       | End time                                      |

Returns: list of label sets matching the selectors.

### List labels

```
prometheus labels --start "2024-01-15T00:00:00Z" --end "2024-01-15T01:00:00Z"
```

| Argument | Type     | Required | Description                    |
|----------|----------|----------|--------------------------------|
| `start`  | string   | no       | Start time for active series   |
| `end`    | string   | no       | End time for active series     |
| `match`  | string[] | no       | Restrict to matching series    |

Returns: list of label names.

### Get label values

```
prometheus label_values --label "job" --start "2024-01-15T00:00:00Z"
```

| Argument | Type     | Required | Description                  |
|----------|----------|----------|------------------------------|
| `label`  | string   | yes      | Label name to get values for |
| `start`  | string   | no       | Start time                   |
| `end`    | string   | no       | End time                     |
| `match`  | string[] | no       | Restrict to matching series  |

Returns: list of string values for the label.

## Targets

### List targets

```
prometheus targets --state "active"
```

| Argument | Type   | Required | Description                              |
|----------|--------|----------|------------------------------------------|
| `state`  | string | no       | `active`, `dropped`, or `any` (default)  |

Returns: `activeTargets` list of `{scrapePool, scrapeUrl, globalUrl, lastError, lastScrape, lastScrapeDuration, health, labels}`, and `droppedTargets`.

## Rules and Alerts

### List rules

```
prometheus rules --type "alert"
```

| Argument | Type   | Required | Description                     |
|----------|--------|----------|---------------------------------|
| `type`   | string | no       | `alert` or `record` (all if omitted) |

Returns: list of rule groups, each with `name`, `file`, `rules` (each having `name`, `query`, `duration`, `health`, `lastEvaluation`, `evaluationTime`).

### List alerts

```
prometheus alerts
```

No required arguments.

Returns: list of active alerts with `labels`, `annotations`, `state`, `activeAt`, `value`.

## Configuration and Metadata

### Get config

```
prometheus config
```

No required arguments.

Returns: `yaml` — the current Prometheus configuration as a string.

### Get flags

```
prometheus flags
```

No required arguments.

Returns: object of runtime flag name → value pairs.

### Get metric metadata

```
prometheus metadata --metric "http_requests_total"
```

| Argument       | Type   | Required | Description                             |
|----------------|--------|----------|-----------------------------------------|
| `metric`       | string | no       | Metric name to filter by                |
| `limit`        | int    | no       | Max number of metrics to return         |

Returns: map of metric name → list of `{type, help, unit}`.

### Get TSDB stats

```
prometheus tsdb_stats
```

No required arguments.

Returns: `headStats` (numSeries, chunkCount, minTime, maxTime), `seriesCountByMetricName`, `labelValueCountByLabelName`, `memoryInBytesByLabelName`, `seriesCountByLabelValuePair`.

## Workflow

1. **Check what's up** with `query --expr "up"` to see all scraped targets.
2. **Profile a metric** with `query_range` over a time window to spot anomalies.
3. **Explore label cardinality** with `labels` and `label_values` before writing queries.
4. **Find firing alerts** with `alerts` to understand current alert state.
5. **Review rules** with `rules --type alert` to understand alerting conditions.
6. **Check target health** with `targets --state active` for scrape errors.
7. **Inspect configuration** with `config` to verify scrape configs and alert manager routing.

## Safety notes

- The Prometheus HTTP API is read-only (no write endpoints exposed via this skill).
- Heavy range queries (long time range, high cardinality, small step) can significantly impact Prometheus performance. Use `timeout` parameter to limit runaway queries.
- The `url` credential should point to your Prometheus server (not Alertmanager or Grafana).
- If Prometheus is behind a reverse proxy with auth, include credentials in the `url` (e.g. `https://user:pass@prometheus.internal`).
