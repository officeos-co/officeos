# Meta Ads

Manage Facebook and Instagram ad campaigns: create campaigns, ad sets, ads, and creatives, configure targeting, retrieve performance insights, and manage custom audiences via the Meta Marketing API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Accounts

### List ad accounts

```
meta-ads list_ad_accounts --per_page 25
```

| Argument   | Type | Required | Default | Description              |
|------------|------|----------|---------|--------------------------|
| `per_page` | int  | no       | 25      | Results per page (1-100) |

Returns: `account_id`, `name`, `account_status`, `currency`, `timezone_name`, `amount_spent`, `balance`.

### Get ad account

```
meta-ads get_ad_account --account_id "act_123456789"
```

| Argument     | Type   | Required | Description           |
|--------------|--------|----------|-----------------------|
| `account_id` | string | yes      | Ad account ID (act_*) |

Returns: `account_id`, `name`, `account_status`, `currency`, `timezone_name`, `business_name`, `spend_cap`, `amount_spent`, `balance`, `age`.

## Campaigns

### List campaigns

```
meta-ads list_campaigns --account_id "act_123456789" --status ACTIVE --date_range '{"since":"2026-01-01","until":"2026-03-31"}' --per_page 20
```

| Argument     | Type   | Required | Default | Description                                         |
|--------------|--------|----------|---------|-----------------------------------------------------|
| `account_id` | string | yes      |         | Ad account ID                                       |
| `status`     | string | no       |         | Filter: `ACTIVE`, `PAUSED`, `DELETED`, `ARCHIVED`   |
| `date_range` | object | no       |         | JSON with `since` and `until` (YYYY-MM-DD)          |
| `per_page`   | int    | no       | 25      | Results per page (1-100)                             |

Returns: list of `id`, `name`, `objective`, `status`, `daily_budget`, `lifetime_budget`, `created_time`, `updated_time`.

### Get campaign

```
meta-ads get_campaign --campaign_id "123456789"
```

| Argument      | Type   | Required | Description |
|---------------|--------|----------|-------------|
| `campaign_id` | string | yes      | Campaign ID |

Returns: `id`, `name`, `objective`, `status`, `daily_budget`, `lifetime_budget`, `bid_strategy`, `special_ad_categories`, `created_time`, `updated_time`.

### Create campaign

```
meta-ads create_campaign --account_id "act_123456789" --name "Summer Sale 2026" --objective OUTCOME_SALES --status PAUSED --daily_budget 5000 --bid_strategy LOWEST_COST_WITHOUT_CAP --special_ad_categories '[]'
```

| Argument               | Type     | Required | Default  | Description                                                        |
|------------------------|----------|----------|----------|--------------------------------------------------------------------|
| `account_id`           | string   | yes      |          | Ad account ID                                                      |
| `name`                 | string   | yes      |          | Campaign name                                                      |
| `objective`            | string   | yes      |          | `OUTCOME_AWARENESS`, `OUTCOME_TRAFFIC`, `OUTCOME_ENGAGEMENT`, `OUTCOME_LEADS`, `OUTCOME_APP_PROMOTION`, `OUTCOME_SALES` |
| `status`               | string   | no       | `PAUSED` | `ACTIVE` or `PAUSED`                                               |
| `daily_budget`         | int      | no       |          | Daily budget in cents                                              |
| `lifetime_budget`      | int      | no       |          | Lifetime budget in cents (mutually exclusive with daily_budget)     |
| `bid_strategy`         | string   | no       |          | `LOWEST_COST_WITHOUT_CAP`, `LOWEST_COST_WITH_BID_CAP`, `COST_CAP` |
| `special_ad_categories`| string[] | no       | `[]`     | `CREDIT`, `EMPLOYMENT`, `HOUSING`, `SOCIAL_ISSUES_ELECTIONS_POLITICS` |

Returns: `id`, `name`, `status`.

### Update campaign

```
meta-ads update_campaign --campaign_id "123456789" --name "Summer Sale 2026 v2" --daily_budget 7500
```

| Argument      | Type   | Required | Description                        |
|---------------|--------|----------|------------------------------------|
| `campaign_id` | string | yes      | Campaign ID                        |
| `name`        | string | no       | Updated name                       |
| `status`      | string | no       | `ACTIVE` or `PAUSED`              |
| `daily_budget`| int    | no       | Updated daily budget in cents      |

Returns: `success` boolean.

### Pause campaign

```
meta-ads pause_campaign --campaign_id "123456789"
```

| Argument      | Type   | Required | Description |
|---------------|--------|----------|-------------|
| `campaign_id` | string | yes      | Campaign ID |

Returns: `success` boolean.

### Delete campaign

```
meta-ads delete_campaign --campaign_id "123456789"
```

| Argument      | Type   | Required | Description |
|---------------|--------|----------|-------------|
| `campaign_id` | string | yes      | Campaign ID |

Returns: `success` boolean.

## Ad Sets

### List ad sets

```
meta-ads list_ad_sets --account_id "act_123456789" --campaign_id "123456789" --status ACTIVE --per_page 20
```

| Argument      | Type   | Required | Default | Description                                       |
|---------------|--------|----------|---------|---------------------------------------------------|
| `account_id`  | string | yes      |         | Ad account ID                                     |
| `campaign_id` | string | no       |         | Filter by campaign                                |
| `status`      | string | no       |         | Filter: `ACTIVE`, `PAUSED`, `DELETED`, `ARCHIVED` |
| `per_page`    | int    | no       | 25      | Results per page (1-100)                           |

Returns: list of `id`, `name`, `campaign_id`, `status`, `daily_budget`, `targeting`, `optimization_goal`, `billing_event`, `start_time`, `end_time`.

### Get ad set

```
meta-ads get_ad_set --ad_set_id "123456789"
```

| Argument    | Type   | Required | Description |
|-------------|--------|----------|-------------|
| `ad_set_id` | string | yes      | Ad set ID   |

Returns: `id`, `name`, `campaign_id`, `status`, `daily_budget`, `lifetime_budget`, `targeting`, `optimization_goal`, `billing_event`, `bid_amount`, `start_time`, `end_time`, `created_time`.

### Create ad set

```
meta-ads create_ad_set --campaign_id "123456789" --name "US Women 25-45" --daily_budget 2000 --billing_event IMPRESSIONS --optimization_goal CONVERSIONS --targeting '{"geo_locations":{"countries":["US"]},"age_min":25,"age_max":45,"genders":[2]}' --start_time "2026-05-01T00:00:00" --end_time "2026-06-01T00:00:00"
```

| Argument            | Type   | Required | Default | Description                                                    |
|---------------------|--------|----------|---------|----------------------------------------------------------------|
| `campaign_id`       | string | yes      |         | Parent campaign ID                                             |
| `name`              | string | yes      |         | Ad set name                                                    |
| `daily_budget`      | int    | no       |         | Daily budget in cents                                          |
| `lifetime_budget`   | int    | no       |         | Lifetime budget in cents                                       |
| `billing_event`     | string | yes      |         | `IMPRESSIONS`, `LINK_CLICKS`, `APP_INSTALLS`                   |
| `optimization_goal` | string | yes      |         | `CONVERSIONS`, `LINK_CLICKS`, `IMPRESSIONS`, `REACH`, `LEAD_GENERATION` |
| `targeting`         | object | yes      |         | JSON targeting spec (geo, age, gender, interests, behaviors)   |
| `start_time`        | string | no       |         | ISO 8601 start time                                            |
| `end_time`          | string | no       |         | ISO 8601 end time                                              |

Returns: `id`, `name`, `status`.

### Update ad set

```
meta-ads update_ad_set --ad_set_id "123456789" --daily_budget 3000 --targeting '{"geo_locations":{"countries":["US","CA"]}}'
```

| Argument    | Type   | Required | Description            |
|-------------|--------|----------|------------------------|
| `ad_set_id` | string | yes      | Ad set ID              |
| `name`      | string | no       | Updated name           |
| `daily_budget` | int | no       | Updated budget in cents |
| `targeting` | object | no       | Updated targeting spec |

Returns: `success` boolean.

### Pause ad set

```
meta-ads pause_ad_set --ad_set_id "123456789"
```

| Argument    | Type   | Required | Description |
|-------------|--------|----------|-------------|
| `ad_set_id` | string | yes      | Ad set ID   |

Returns: `success` boolean.

## Ads

### List ads

```
meta-ads list_ads --account_id "act_123456789" --ad_set_id "123456789" --status ACTIVE --per_page 20
```

| Argument     | Type   | Required | Default | Description                                       |
|--------------|--------|----------|---------|---------------------------------------------------|
| `account_id` | string | yes      |         | Ad account ID                                     |
| `ad_set_id`  | string | no       |         | Filter by ad set                                  |
| `status`     | string | no       |         | Filter: `ACTIVE`, `PAUSED`, `DELETED`, `ARCHIVED` |
| `per_page`   | int    | no       | 25      | Results per page (1-100)                           |

Returns: list of `id`, `name`, `ad_set_id`, `creative_id`, `status`, `created_time`.

### Get ad

```
meta-ads get_ad --ad_id "123456789"
```

| Argument | Type   | Required | Description |
|----------|--------|----------|-------------|
| `ad_id`  | string | yes      | Ad ID       |

Returns: `id`, `name`, `ad_set_id`, `creative`, `status`, `created_time`, `updated_time`, `preview_shareable_link`.

### Create ad

```
meta-ads create_ad --ad_set_id "123456789" --creative_id "987654321" --name "Summer Sale - Image A" --status PAUSED
```

| Argument      | Type   | Required | Default  | Description              |
|---------------|--------|----------|----------|--------------------------|
| `ad_set_id`   | string | yes      |          | Parent ad set ID         |
| `creative_id` | string | yes      |          | Ad creative ID to use    |
| `name`        | string | yes      |          | Ad name                  |
| `status`      | string | no       | `PAUSED` | `ACTIVE` or `PAUSED`     |

Returns: `id`, `name`, `status`.

### Update ad

```
meta-ads update_ad --ad_id "123456789" --name "Summer Sale - Image B" --status ACTIVE
```

| Argument | Type   | Required | Description              |
|----------|--------|----------|--------------------------|
| `ad_id`  | string | yes      | Ad ID                    |
| `name`   | string | no       | Updated name             |
| `status` | string | no       | `ACTIVE` or `PAUSED`     |

Returns: `success` boolean.

### Pause ad

```
meta-ads pause_ad --ad_id "123456789"
```

| Argument | Type   | Required | Description |
|----------|--------|----------|-------------|
| `ad_id`  | string | yes      | Ad ID       |

Returns: `success` boolean.

## Creatives

### List creatives

```
meta-ads list_creatives --account_id "act_123456789" --per_page 20
```

| Argument     | Type   | Required | Default | Description              |
|--------------|--------|----------|---------|--------------------------|
| `account_id` | string | yes      |         | Ad account ID            |
| `per_page`   | int    | no       | 25      | Results per page (1-100) |

Returns: list of `id`, `name`, `object_story_spec`, `thumbnail_url`, `status`.

### Get creative

```
meta-ads get_creative --creative_id "987654321"
```

| Argument      | Type   | Required | Description  |
|---------------|--------|----------|--------------|
| `creative_id` | string | yes      | Creative ID  |

Returns: `id`, `name`, `object_story_spec`, `url_tags`, `thumbnail_url`, `image_url`, `video_id`.

### Create creative

```
meta-ads create_creative --account_id "act_123456789" --name "Summer Banner" --object_story_spec '{"page_id":"123456","link_data":{"link":"https://example.com","message":"Shop now!","image_hash":"abc123"}}'
```

| Argument            | Type   | Required | Description                                                               |
|---------------------|--------|----------|---------------------------------------------------------------------------|
| `account_id`        | string | yes      | Ad account ID                                                             |
| `name`              | string | yes      | Creative name                                                             |
| `object_story_spec` | object | yes      | JSON spec with `page_id` and `link_data` (link, message, image_hash/video_data) |
| `url_tags`          | string | no       | URL parameters to append to all links                                     |

Returns: `id`, `name`.

## Targeting

### Search targeting options

```
meta-ads search_targeting --query "fitness" --type interest --per_page 20
```

| Argument  | Type   | Required | Default | Description                                         |
|-----------|--------|----------|---------|-----------------------------------------------------|
| `query`   | string | yes      |         | Search term                                         |
| `type`    | string | yes      |         | `interest`, `behavior`, `demographics`, `education`, `work_employer`, `work_position` |
| `per_page`| int    | no       | 25      | Results per page                                    |

Returns: list of `id`, `name`, `type`, `audience_size_lower_bound`, `audience_size_upper_bound`, `path`.

### Get targeting suggestions

```
meta-ads get_targeting_suggestions --targeting_list '["6003139266461"]' --type interest
```

| Argument         | Type     | Required | Description                       |
|------------------|----------|----------|-----------------------------------|
| `targeting_list` | string[] | yes      | List of targeting IDs to seed     |
| `type`           | string   | no       | Filter suggestion type            |

Returns: list of suggested targeting options with `id`, `name`, `type`, `audience_size_lower_bound`, `audience_size_upper_bound`.

## Insights

### Get insights

```
meta-ads get_insights --object_id "123456789" --level campaign --date_range '{"since":"2026-01-01","until":"2026-03-31"}' --fields '["spend","impressions","clicks","cpc","ctr","conversions","cost_per_conversion"]' --breakdowns '["age","gender"]'
```

| Argument     | Type     | Required | Default    | Description                                                        |
|--------------|----------|----------|------------|--------------------------------------------------------------------|
| `object_id`  | string   | yes      |            | Account, campaign, ad set, or ad ID                                |
| `level`      | string   | no       | `campaign` | Aggregation level: `account`, `campaign`, `adset`, `ad`            |
| `date_range` | object   | no       |            | JSON with `since` and `until` (YYYY-MM-DD)                         |
| `fields`     | string[] | no       | all        | Metrics: `spend`, `impressions`, `clicks`, `cpc`, `cpm`, `ctr`, `reach`, `frequency`, `conversions`, `cost_per_conversion`, `actions`, `action_values` |
| `breakdowns` | string[] | no       |            | `age`, `gender`, `country`, `placement`, `device_platform`, `publisher_platform` |
| `time_increment` | int  | no       |            | Aggregate by N days (1 = daily, 7 = weekly)                        |

Returns: list of metrics per breakdown combination with `date_start`, `date_stop`, and requested field values.

### Get campaign insights

```
meta-ads get_campaign_insights --campaign_id "123456789" --date_range '{"since":"2026-03-01","until":"2026-03-31"}'
```

| Argument      | Type   | Required | Description                                |
|---------------|--------|----------|--------------------------------------------|
| `campaign_id` | string | yes      | Campaign ID                                |
| `date_range`  | object | no       | JSON with `since` and `until` (YYYY-MM-DD) |

Returns: `spend`, `impressions`, `clicks`, `cpc`, `ctr`, `conversions`, `cost_per_conversion`, `reach`, `frequency`.

### Get ad set insights

```
meta-ads get_ad_set_insights --ad_set_id "123456789" --date_range '{"since":"2026-03-01","until":"2026-03-31"}'
```

| Argument    | Type   | Required | Description                                |
|-------------|--------|----------|--------------------------------------------|
| `ad_set_id` | string | yes      | Ad set ID                                  |
| `date_range`| object | no       | JSON with `since` and `until` (YYYY-MM-DD) |

Returns: `spend`, `impressions`, `clicks`, `cpc`, `ctr`, `conversions`, `cost_per_conversion`, `reach`, `frequency`.

### Get ad insights

```
meta-ads get_ad_insights --ad_id "123456789" --date_range '{"since":"2026-03-01","until":"2026-03-31"}'
```

| Argument    | Type   | Required | Description                                |
|-------------|--------|----------|--------------------------------------------|
| `ad_id`     | string | yes      | Ad ID                                      |
| `date_range`| object | no       | JSON with `since` and `until` (YYYY-MM-DD) |

Returns: `spend`, `impressions`, `clicks`, `cpc`, `ctr`, `conversions`, `cost_per_conversion`, `reach`, `frequency`.

## Audiences

### List custom audiences

```
meta-ads list_custom_audiences --account_id "act_123456789" --per_page 20
```

| Argument     | Type   | Required | Default | Description              |
|--------------|--------|----------|---------|--------------------------|
| `account_id` | string | yes      |         | Ad account ID            |
| `per_page`   | int    | no       | 25      | Results per page (1-100) |

Returns: list of `id`, `name`, `subtype`, `approximate_count`, `delivery_status`, `operation_status`.

### Create custom audience

```
meta-ads create_custom_audience --account_id "act_123456789" --name "Website Visitors 30d" --subtype WEBSITE --rule '{"inclusions":{"operator":"or","rules":[{"event_sources":[{"id":"123456","type":"pixel"}],"retention_seconds":2592000}]}}'
```

| Argument     | Type   | Required | Description                                                    |
|--------------|--------|----------|----------------------------------------------------------------|
| `account_id` | string | yes      | Ad account ID                                                  |
| `name`       | string | yes      | Audience name                                                  |
| `subtype`    | string | yes      | `WEBSITE`, `APP`, `ENGAGEMENT`, `CUSTOMER_LIST`, `LOOKALIKE`   |
| `description`| string | no       | Audience description                                           |
| `rule`       | object | no       | JSON rule definition (required for WEBSITE/APP/ENGAGEMENT)     |

Returns: `id`, `name`, `subtype`.

### Get audience size

```
meta-ads get_audience_size --audience_id "123456789"
```

| Argument      | Type   | Required | Description       |
|---------------|--------|----------|-------------------|
| `audience_id` | string | yes      | Custom audience ID |

Returns: `id`, `name`, `approximate_count`, `delivery_status`.

## Images

### Upload image

```
meta-ads upload_image --account_id "act_123456789" --url "https://example.com/banner.jpg" --name "Summer Banner"
```

| Argument     | Type   | Required | Description                           |
|--------------|--------|----------|---------------------------------------|
| `account_id` | string | yes      | Ad account ID                         |
| `url`        | string | yes      | Public URL of the image to upload     |
| `name`       | string | no       | Image name for reference              |

Returns: `image_hash`, `url`, `name`, `width`, `height`.

### List images

```
meta-ads list_images --account_id "act_123456789" --per_page 20
```

| Argument     | Type   | Required | Default | Description              |
|--------------|--------|----------|---------|--------------------------|
| `account_id` | string | yes      |         | Ad account ID            |
| `per_page`   | int    | no       | 25      | Results per page (1-100) |

Returns: list of `image_hash`, `url`, `name`, `width`, `height`, `created_time`.

## Workflow

1. **Start with `list_ad_accounts`** to find your account ID. Never guess account IDs.
2. **Create campaigns top-down**: campaign -> ad set -> creative -> ad.
3. Use `search_targeting` to discover interest/behavior IDs before building targeting specs for ad sets.
4. Always create campaigns in `PAUSED` status first, then activate after verifying setup.
5. Use `get_insights` with breakdowns to analyze performance across dimensions.
6. Upload images first with `upload_image`, then reference the `image_hash` in creatives.
7. Use `list_custom_audiences` and `create_custom_audience` for retargeting setups.
8. Check `get_campaign_insights` regularly to monitor spend and performance.

## Safety notes

- All budgets are specified in **cents** (e.g. 5000 = $50.00). Double-check values before creating or updating.
- Creating campaigns with `ACTIVE` status will immediately start spending. Always use `PAUSED` for initial setup.
- `delete_campaign` is **permanent** and cannot be undone. Prefer `pause_campaign` to stop spend.
- Meta API rate limits apply. Batch requests where possible and avoid rapid polling.
- Special ad categories (`CREDIT`, `EMPLOYMENT`, `HOUSING`) restrict targeting options and must be declared.
- Insights data may have up to 24-48 hours of reporting delay.
- Only ad accounts accessible to the configured access token are visible.
- Image uploads require publicly accessible URLs. Ensure URLs are reachable before uploading.
