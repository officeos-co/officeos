# Google Analytics

Query reports, explore dimensions and metrics, manage audiences, and track events and conversions via the Google Analytics 4 Data API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Properties

### List properties

```
google-analytics list_properties
```

Returns: list of `property_id`, `display_name`, `time_zone`, `currency_code`, `industry_category`, `create_time`.

### Get property

```
google-analytics get_property --property_id "properties/123456789"
```

| Argument      | Type   | Required | Description                              |
|---------------|--------|----------|------------------------------------------|
| `property_id` | string | yes      | Property ID (e.g. `properties/123456789`)|

Returns: `property_id`, `display_name`, `time_zone`, `currency_code`, `industry_category`, `parent`, `create_time`, `update_time`.

## Reports

### Run report

```
google-analytics run_report --property_id "properties/123456789" --date_ranges '[{"start_date":"2026-03-01","end_date":"2026-03-31"}]' --dimensions '["country","city"]' --metrics '["activeUsers","sessions","screenPageViews"]' --limit 25
```

| Argument           | Type     | Required | Default | Description                                                      |
|--------------------|----------|----------|---------|------------------------------------------------------------------|
| `property_id`      | string   | yes      |         | GA4 property ID                                                  |
| `date_ranges`      | string   | yes      |         | JSON array of `{start_date, end_date}` objects                   |
| `dimensions`       | string[] | no       |         | Dimension names (e.g. `country`, `pagePath`, `sessionSource`)    |
| `metrics`          | string[] | yes      |         | Metric names (e.g. `activeUsers`, `sessions`, `conversions`)     |
| `dimension_filter` | string   | no       |         | JSON filter expression for dimensions                            |
| `metric_filter`    | string   | no       |         | JSON filter expression for metrics                               |
| `order_bys`        | string   | no       |         | JSON array of order-by specifications                            |
| `limit`            | int      | no       | 10      | Maximum rows to return (1-100000)                                |

Returns: `dimension_headers`, `metric_headers`, `rows` (each with `dimension_values` and `metric_values`), `row_count`, `metadata`.

### Run realtime report

```
google-analytics run_realtime_report --property_id "properties/123456789" --dimensions '["country"]' --metrics '["activeUsers"]' --limit 10
```

| Argument           | Type     | Required | Default | Description                                    |
|--------------------|----------|----------|---------|------------------------------------------------|
| `property_id`      | string   | yes      |         | GA4 property ID                                |
| `dimensions`       | string[] | no       |         | Dimension names                                |
| `metrics`          | string[] | yes      |         | Metric names                                   |
| `dimension_filter` | string   | no       |         | JSON filter expression for dimensions          |
| `metric_filter`    | string   | no       |         | JSON filter expression for metrics             |
| `limit`            | int      | no       | 10      | Maximum rows to return                         |

Returns: `dimension_headers`, `metric_headers`, `rows`, `row_count`.

### Run pivot report

```
google-analytics run_pivot_report --property_id "properties/123456789" --date_ranges '[{"start_date":"2026-03-01","end_date":"2026-03-31"}]' --pivots '[{"field_names":["country"],"limit":5}]' --metrics '["sessions"]'
```

| Argument      | Type     | Required | Description                                              |
|---------------|----------|----------|----------------------------------------------------------|
| `property_id` | string   | yes      | GA4 property ID                                          |
| `date_ranges` | string   | yes      | JSON array of `{start_date, end_date}` objects           |
| `pivots`      | string   | yes      | JSON array of pivot definitions (`field_names`, `limit`) |
| `metrics`     | string[] | yes      | Metric names                                             |
| `dimensions`  | string[] | no       | Dimension names                                          |

Returns: `pivot_headers`, `rows`, `metadata`.

### Batch run reports

```
google-analytics batch_run_reports --property_id "properties/123456789" --requests '[{"date_ranges":[{"start_date":"2026-03-01","end_date":"2026-03-31"}],"metrics":["activeUsers"]},{"date_ranges":[{"start_date":"2026-03-01","end_date":"2026-03-31"}],"metrics":["sessions"]}]'
```

| Argument      | Type   | Required | Description                                       |
|---------------|--------|----------|---------------------------------------------------|
| `property_id` | string | yes      | GA4 property ID                                   |
| `requests`    | string | yes      | JSON array of report request objects               |

Returns: list of report responses, each containing `dimension_headers`, `metric_headers`, `rows`.

## Dimensions / Metrics

### List dimensions

```
google-analytics list_dimensions --property_id "properties/123456789"
```

| Argument      | Type   | Required | Description     |
|---------------|--------|----------|-----------------|
| `property_id` | string | yes      | GA4 property ID |

Returns: list of `api_name`, `ui_name`, `description`, `category`, `deprecated`.

### List metrics

```
google-analytics list_metrics --property_id "properties/123456789"
```

| Argument      | Type   | Required | Description     |
|---------------|--------|----------|-----------------|
| `property_id` | string | yes      | GA4 property ID |

Returns: list of `api_name`, `ui_name`, `description`, `category`, `type`, `deprecated`.

## Audiences

### List audiences

```
google-analytics list_audiences --property_id "properties/123456789"
```

| Argument      | Type   | Required | Description     |
|---------------|--------|----------|-----------------|
| `property_id` | string | yes      | GA4 property ID |

Returns: list of `audience_id`, `display_name`, `description`, `membership_duration_days`, `ads_personalization_enabled`.

### Get audience

```
google-analytics get_audience --property_id "properties/123456789" --audience_id "123"
```

| Argument      | Type   | Required | Description       |
|---------------|--------|----------|-------------------|
| `property_id` | string | yes      | GA4 property ID   |
| `audience_id` | string | yes      | Audience ID       |

Returns: `audience_id`, `display_name`, `description`, `membership_duration_days`, `filter_clauses`, `event_trigger`.

## Events

### List event names

```
google-analytics list_event_names --property_id "properties/123456789" --date_ranges '[{"start_date":"2026-03-01","end_date":"2026-03-31"}]' --limit 50
```

| Argument      | Type   | Required | Default | Description                              |
|---------------|--------|----------|---------|------------------------------------------|
| `property_id` | string | yes      |         | GA4 property ID                          |
| `date_ranges` | string | yes      |         | JSON array of `{start_date, end_date}`   |
| `limit`       | int    | no       | 25      | Maximum events to return                 |

Returns: list of `event_name`, `event_count`.

## Conversions

### List conversions

```
google-analytics list_conversions --property_id "properties/123456789" --date_ranges '[{"start_date":"2026-03-01","end_date":"2026-03-31"}]'
```

| Argument      | Type   | Required | Description                              |
|---------------|--------|----------|------------------------------------------|
| `property_id` | string | yes      | GA4 property ID                          |
| `date_ranges` | string | yes      | JSON array of `{start_date, end_date}`   |

Returns: list of `event_name`, `conversions`, `total_revenue`.

### Get conversion rate

```
google-analytics get_conversion_rate --property_id "properties/123456789" --date_ranges '[{"start_date":"2026-03-01","end_date":"2026-03-31"}]' --event_name "purchase"
```

| Argument      | Type   | Required | Description                              |
|---------------|--------|----------|------------------------------------------|
| `property_id` | string | yes      | GA4 property ID                          |
| `date_ranges` | string | yes      | JSON array of `{start_date, end_date}`   |
| `event_name`  | string | yes      | Conversion event name                    |

Returns: `event_name`, `sessions`, `conversions`, `conversion_rate`.

## Workflow

1. **Start with `google-analytics list_properties`** to discover available GA4 properties.
2. Use `list_dimensions` and `list_metrics` to explore available data fields for a property.
3. Build reports with `run_report` specifying date ranges, dimensions, and metrics.
4. Use `run_realtime_report` for live traffic data.
5. Use `batch_run_reports` to run multiple reports in a single request for efficiency.
6. Use `list_event_names` to discover tracked events before querying them.
7. Use `list_conversions` and `get_conversion_rate` to analyze conversion performance.

## Safety notes

- Property IDs use the format `properties/123456789`. **Never fabricate them** -- always discover via `list_properties`.
- Date ranges use `YYYY-MM-DD` format. Relative dates like `today`, `yesterday`, `7daysAgo`, `30daysAgo` are also supported.
- Dimension and metric names are API names (e.g. `activeUsers`, not `Active Users`). Use `list_dimensions` and `list_metrics` to find valid names.
- `dimension_filter` and `metric_filter` use the GA4 filter expression format. Example: `{"filter":{"field_name":"country","string_filter":{"value":"United States"}}}`.
- `order_bys` format: `[{"metric":{"metric_name":"activeUsers"},"desc":true}]` or `[{"dimension":{"dimension_name":"date"}}]`.
- Realtime reports have a limited set of available dimensions and metrics compared to standard reports.
- Reports are subject to data sampling for large datasets. Check `metadata` in the response for sampling indicators.
- GA4 Data API has quota limits (varies by property tier). Batch operations when possible.
- Only properties accessible to the authenticated service account or user are visible.
- Historical data availability depends on property retention settings (default 14 months for GA4).
