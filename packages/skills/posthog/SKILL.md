# PostHog

Query events, manage persons, feature flags, insights, dashboards, cohorts, and annotations via the PostHog API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Events

### List events

```
posthog list_events --event "$pageview" --limit 50 --after "2024-01-15T00:00:00Z"
```

| Argument     | Type   | Required | Default | Description                         |
|--------------|--------|----------|---------|-------------------------------------|
| `event`      | string | no       |         | Filter by event name                |
| `distinct_id`| string | no       |         | Filter by person distinct ID        |
| `limit`      | int    | no       | 100     | Max results                         |
| `after`      | string | no       |         | ISO 8601 timestamp lower bound      |
| `before`     | string | no       |         | ISO 8601 timestamp upper bound      |

Returns: list of `id`, `event`, `distinct_id`, `properties`, `timestamp`, `person`.

### Get event

```
posthog get_event --event_id "12345678-1234-1234-1234-123456789012"
```

| Argument   | Type   | Required | Description |
|------------|--------|----------|-------------|
| `event_id` | string | yes      | Event UUID  |

Returns: `id`, `event`, `distinct_id`, `properties`, `timestamp`, `person`, `elements`.

## Persons

### List persons

```
posthog list_persons --search "jane@example.com" --limit 20
```

| Argument | Type   | Required | Default | Description                          |
|----------|--------|----------|---------|--------------------------------------|
| `search` | string | no       |         | Search by email, name, distinct_id   |
| `limit`  | int    | no       | 100     | Max results                          |

Returns: list of `id`, `uuid`, `distinct_ids`, `properties`, `created_at`.

### Get person

```
posthog get_person --person_id 12345
```

| Argument    | Type | Required | Description |
|-------------|------|----------|-------------|
| `person_id` | int  | yes      | Person ID   |

Returns: `id`, `uuid`, `distinct_ids`, `properties`, `created_at`.

### Delete person

```
posthog delete_person --person_id 12345 --delete_events true
```

| Argument        | Type    | Required | Default | Description                             |
|-----------------|---------|----------|---------|-----------------------------------------|
| `person_id`     | int     | yes      |         | Person ID                               |
| `delete_events` | boolean | no       | false   | Also delete all associated events       |

Returns: `{}` (204 on success).

## Feature Flags

### List feature flags

```
posthog list_feature_flags
```

No required arguments.

Returns: list of `id`, `key`, `name`, `active`, `rollout_percentage`, `filters`, `created_at`.

### Get feature flag

```
posthog get_feature_flag --flag_id 42
```

| Argument  | Type | Required | Description     |
|-----------|------|----------|-----------------|
| `flag_id` | int  | yes      | Feature flag ID |

Returns: `id`, `key`, `name`, `active`, `rollout_percentage`, `filters`, `ensure_experience_continuity`, `created_at`, `created_by`.

### Create feature flag

```
posthog create_feature_flag --key "new-checkout" --name "New Checkout Flow" --rollout_percentage 20 --active true
```

| Argument              | Type    | Required | Default | Description                                    |
|-----------------------|---------|----------|---------|------------------------------------------------|
| `key`                 | string  | yes      |         | Unique flag key (snake_case)                   |
| `name`                | string  | no       |         | Human-readable name                            |
| `active`              | boolean | no       | true    | Whether the flag is enabled                    |
| `rollout_percentage`  | int     | no       | 100     | Percentage of users to enable for (0-100)      |
| `filters`             | object  | no       |         | Advanced filter groups JSON                    |

Returns: `id`, `key`, `name`, `active`, `rollout_percentage`.

### Update feature flag

```
posthog update_feature_flag --flag_id 42 --rollout_percentage 50 --active true
```

| Argument             | Type    | Required | Description                  |
|----------------------|---------|----------|------------------------------|
| `flag_id`            | int     | yes      | Feature flag ID              |
| `name`               | string  | no       | Updated name                 |
| `active`             | boolean | no       | Enable or disable the flag   |
| `rollout_percentage` | int     | no       | Updated rollout percentage   |
| `filters`            | object  | no       | Updated filter groups        |

Returns: `id`, `key`, `active`, `rollout_percentage`.

## Insights

### List insights

```
posthog list_insights --saved true --limit 20
```

| Argument | Type    | Required | Description                    |
|----------|---------|----------|--------------------------------|
| `saved`  | boolean | no       | Return only saved insights     |
| `limit`  | int     | no       | Max results                    |

Returns: list of `id`, `name`, `filters`, `created_at`, `last_modified_at`, `dashboard`.

### Get insight

```
posthog get_insight --insight_id 123
```

| Argument     | Type | Required | Description |
|--------------|------|----------|-------------|
| `insight_id` | int  | yes      | Insight ID  |

Returns: `id`, `name`, `description`, `filters`, `result`, `created_at`, `last_modified_at`.

### Create insight

```
posthog create_insight --name "Weekly signups" --filters '{"insight":"TRENDS","events":[{"id":"$signup","type":"events"}]}'
```

| Argument      | Type   | Required | Description                      |
|---------------|--------|----------|----------------------------------|
| `name`        | string | no       | Insight name                     |
| `description` | string | no       | Insight description              |
| `filters`     | object | yes      | PostHog filters object (see docs)|

Returns: `id`, `name`, `filters`, `created_at`.

## Dashboards

### List dashboards

```
posthog list_dashboards
```

No required arguments.

Returns: list of `id`, `name`, `description`, `pinned`, `created_at`, `created_by`.

### Get dashboard

```
posthog get_dashboard --dashboard_id 5
```

| Argument       | Type | Required | Description  |
|----------------|------|----------|--------------|
| `dashboard_id` | int  | yes      | Dashboard ID |

Returns: `id`, `name`, `description`, `pinned`, `tiles` (list of insight tiles), `created_at`, `created_by`.

## Cohorts

### List cohorts

```
posthog list_cohorts
```

No required arguments.

Returns: list of `id`, `name`, `description`, `count`, `created_at`, `created_by`, `is_calculating`.

### Get cohort

```
posthog get_cohort --cohort_id 7
```

| Argument    | Type | Required | Description |
|-------------|------|----------|-------------|
| `cohort_id` | int  | yes      | Cohort ID   |

Returns: `id`, `name`, `description`, `count`, `filters`, `created_at`, `created_by`.

### Create cohort

```
posthog create_cohort --name "Power Users" --description "Users with 10+ events in last 7 days" --filters '{"properties":{"type":"AND","values":[]}}'
```

| Argument      | Type   | Required | Description                            |
|---------------|--------|----------|----------------------------------------|
| `name`        | string | yes      | Cohort name                            |
| `description` | string | no       | Cohort description                     |
| `filters`     | object | no       | Filter conditions JSON                 |

Returns: `id`, `name`, `is_calculating`.

## Annotations

### List annotations

```
posthog list_annotations --limit 20
```

| Argument | Type | Required | Description |
|----------|------|----------|-------------|
| `limit`  | int  | no       | Max results |

Returns: list of `id`, `content`, `date_marker`, `scope`, `created_at`, `created_by`.

### Create annotation

```
posthog create_annotation --content "Deployed v2.0.0" --date_marker "2024-01-15T14:00:00Z" --scope project
```

| Argument      | Type   | Required | Default     | Description                         |
|---------------|--------|----------|-------------|-------------------------------------|
| `content`     | string | yes      |             | Annotation text                     |
| `date_marker` | string | no       | now         | ISO 8601 timestamp for the marker   |
| `scope`       | string | no       | `project`   | `project` or `organization`         |
| `dashboard_id`| int    | no       |             | Associate with a specific dashboard |

Returns: `id`, `content`, `date_marker`, `scope`, `created_at`.

## Workflow

1. **Explore user behaviour** with `list_events` to see what actions users are taking.
2. **Find specific users** with `list_persons` or `get_person` to investigate individual journeys.
3. **Analyse trends** with `list_insights` and `get_insight` to review key metrics.
4. **Create feature flags** with `create_feature_flag` to roll out features incrementally.
5. **Mark deployments** with `create_annotation` to correlate events with metric changes.
6. **Segment users** with `create_cohort` for targeted feature rollouts or analysis.
7. **Monitor dashboards** with `get_dashboard` for the latest metrics snapshot.

## Safety notes

- `delete_person` with `delete_events: true` permanently erases the person and all their events. This cannot be undone.
- The `api_key` must be a **personal API key** (not the project API key used for event ingestion). Create one at PostHog → Settings → Personal API Keys.
- Self-hosted instances: set `host` to your PostHog URL (e.g. `https://posthog.mycompany.com`).
- API rate limits depend on your PostHog plan. Cloud: 240 requests/minute for most endpoints.
