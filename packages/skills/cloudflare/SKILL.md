# Cloudflare

Manage DNS, Workers, Pages, cache, SSL, firewall, and analytics via the Cloudflare API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## DNS

### List zones

```
cloudflare list_zones --name example.com --per_page 20
```

| Argument   | Type   | Required | Default | Description                    |
|------------|--------|----------|---------|--------------------------------|
| `name`     | string | no       |         | Filter by domain name          |
| `status`   | string | no       |         | `active`, `pending`, `moved`   |
| `per_page` | int    | no       | 20      | Results per page (1-50)        |

Returns: list with `id`, `name`, `status`, `name_servers`, `plan`, `created_on`.

### Get zone

```
cloudflare get_zone --zone_id abc123
```

| Argument  | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `zone_id` | string | yes      | Zone ID     |

Returns: `id`, `name`, `status`, `name_servers`, `plan`, `ssl_status`, `created_on`, `modified_on`.

### List DNS records

```
cloudflare list_dns_records --zone_id abc123 --type A --name api.example.com
```

| Argument   | Type   | Required | Default | Description                        |
|------------|--------|----------|---------|------------------------------------|
| `zone_id`  | string | yes      |         | Zone ID                            |
| `type`     | string | no       |         | `A`, `AAAA`, `CNAME`, `MX`, `TXT`, `NS`, `SRV` |
| `name`     | string | no       |         | Filter by record name              |
| `per_page` | int    | no       | 50      | Results per page (1-100)           |

Returns: list with `id`, `type`, `name`, `content`, `proxied`, `ttl`, `priority`.

### Create DNS record

```
cloudflare create_dns_record --zone_id abc123 --type A --name api.example.com --content 1.2.3.4 --proxied true --ttl 1
```

| Argument   | Type    | Required | Default | Description                             |
|------------|---------|----------|---------|-----------------------------------------|
| `zone_id`  | string  | yes      |         | Zone ID                                 |
| `type`     | string  | yes      |         | Record type (`A`, `AAAA`, `CNAME`, `MX`, `TXT`, etc.) |
| `name`     | string  | yes      |         | Record name (e.g. `api.example.com`)    |
| `content`  | string  | yes      |         | Record value (IP, hostname, text)       |
| `proxied`  | boolean | no       | false   | Route through Cloudflare proxy          |
| `ttl`      | int     | no       | 1       | TTL in seconds (1 = auto)              |
| `priority` | int     | no       |         | Priority (required for MX, SRV)        |

Returns: `id`, `type`, `name`, `content`, `proxied`, `ttl`.

### Update DNS record

```
cloudflare update_dns_record --zone_id abc123 --record_id def456 --content 5.6.7.8 --proxied true
```

| Argument    | Type    | Required | Description                          |
|-------------|---------|----------|--------------------------------------|
| `zone_id`   | string  | yes      | Zone ID                              |
| `record_id` | string  | yes      | DNS record ID                        |
| `type`      | string  | no       | Record type                          |
| `name`      | string  | no       | Record name                          |
| `content`   | string  | no       | Record value                         |
| `proxied`   | boolean | no       | Route through Cloudflare proxy       |
| `ttl`       | int     | no       | TTL in seconds                       |

Returns: updated record with `id`, `type`, `name`, `content`, `proxied`, `ttl`.

### Delete DNS record

```
cloudflare delete_dns_record --zone_id abc123 --record_id def456
```

| Argument    | Type   | Required | Description    |
|-------------|--------|----------|----------------|
| `zone_id`   | string | yes      | Zone ID        |
| `record_id` | string | yes      | DNS record ID  |

Returns: confirmation with `id`.

## Workers

### List Workers

```
cloudflare list_workers
```

Returns: list with `id`, `name`, `created_on`, `modified_on`, `routes`.

### Get Worker

```
cloudflare get_worker --name my-worker
```

| Argument | Type   | Required | Description   |
|----------|--------|----------|---------------|
| `name`   | string | yes      | Worker name   |

Returns: `name`, `script` (source code), `routes`, `bindings`, `created_on`, `modified_on`.

### Deploy Worker

```
cloudflare deploy_worker --name my-worker --script "export default { async fetch(req) { return new Response('Hello') } }" --routes '["api.example.com/*"]'
```

| Argument   | Type     | Required | Description                           |
|------------|----------|----------|---------------------------------------|
| `name`     | string   | yes      | Worker name                           |
| `script`   | string   | yes      | Worker script content                 |
| `routes`   | string[] | no       | Route patterns (e.g. `example.com/*`) |
| `bindings` | string   | no       | JSON object of bindings (KV, R2, etc.) |

Returns: `name`, `tag`, `routes`, `size`.

### Delete Worker

```
cloudflare delete_worker --name my-worker
```

| Argument | Type   | Required | Description |
|----------|--------|----------|-------------|
| `name`   | string | yes      | Worker name |

Returns: confirmation with `name`.

### Tail Worker

```
cloudflare tail_worker --name my-worker --status error --sampling_rate 0.1
```

| Argument        | Type   | Required | Default | Description                           |
|-----------------|--------|----------|---------|---------------------------------------|
| `name`          | string | yes      |         | Worker name                           |
| `status`        | string | no       |         | Filter: `ok` or `error`              |
| `sampling_rate` | float  | no       | 1.0     | Sample rate (0.0-1.0)                |

Returns: stream of log entries with `timestamp`, `outcome`, `event`, `logs`, `exceptions`.

## Pages

### List Pages projects

```
cloudflare list_pages_projects
```

Returns: list with `name`, `subdomain`, `production_branch`, `latest_deployment`, `created_on`.

### Create Pages project

```
cloudflare create_pages_project --name my-site --production_branch main --build_command "npm run build" --build_output_dir dist
```

| Argument            | Type   | Required | Default | Description                     |
|---------------------|--------|----------|---------|---------------------------------|
| `name`              | string | yes      |         | Project name                    |
| `production_branch` | string | no       | `main`  | Branch for production deploys   |
| `build_command`     | string | no       |         | Build command                   |
| `build_output_dir`  | string | no       |         | Output directory                |

Returns: `name`, `subdomain`, `production_branch`.

### Deploy Pages

```
cloudflare deploy_pages --name my-site --branch main
```

| Argument | Type   | Required | Default | Description                 |
|----------|--------|----------|---------|-----------------------------|
| `name`   | string | yes      |         | Project name                |
| `branch` | string | no       | `main`  | Branch to deploy            |

Returns: `id`, `url`, `environment`, `status`, `created_on`.

## Cache

### Purge cache

```
cloudflare purge_cache --zone_id abc123 --purge_everything true
```

```
cloudflare purge_cache --zone_id abc123 --files '["https://example.com/style.css","https://example.com/app.js"]'
```

| Argument           | Type     | Required | Default | Description                     |
|--------------------|----------|----------|---------|---------------------------------|
| `zone_id`          | string   | yes      |         | Zone ID                         |
| `purge_everything` | boolean  | no       | false   | Purge entire cache              |
| `files`            | string[] | no       |         | Specific URLs to purge          |
| `tags`             | string[] | no       |         | Cache tags to purge             |

Returns: `id` of the purge request.

### Cache settings

```
cloudflare cache_settings --zone_id abc123
```

```
cloudflare cache_settings --zone_id abc123 --browser_ttl 14400 --cache_level aggressive
```

| Argument      | Type   | Required | Description                                 |
|---------------|--------|----------|---------------------------------------------|
| `zone_id`     | string | yes      | Zone ID                                     |
| `browser_ttl` | int    | no       | Browser cache TTL in seconds                |
| `cache_level` | string | no       | `aggressive`, `basic`, `simplified`         |

Returns: current cache settings with `browser_ttl`, `cache_level`, `development_mode`.

## SSL

### List certificates

```
cloudflare list_certificates --zone_id abc123
```

| Argument  | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `zone_id` | string | yes      | Zone ID     |

Returns: list with `id`, `hosts`, `issuer`, `status`, `expires_on`.

### Get SSL settings

```
cloudflare get_ssl_settings --zone_id abc123
```

| Argument  | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `zone_id` | string | yes      | Zone ID     |

Returns: `mode` (`off`, `flexible`, `full`, `strict`), `certificate_status`, `min_tls_version`, `tls_1_3`.

### Update SSL settings

```
cloudflare update_ssl_settings --zone_id abc123 --mode strict --min_tls_version 1.2 --tls_1_3 on
```

| Argument          | Type   | Required | Description                               |
|-------------------|--------|----------|-------------------------------------------|
| `zone_id`         | string | yes      | Zone ID                                   |
| `mode`            | string | no       | `off`, `flexible`, `full`, `strict`       |
| `min_tls_version` | string | no       | `1.0`, `1.1`, `1.2`, `1.3`               |
| `tls_1_3`         | string | no       | `on`, `off`, `zrt`                        |

Returns: updated SSL settings.

## Firewall

### List firewall rules

```
cloudflare list_firewall_rules --zone_id abc123
```

| Argument  | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `zone_id` | string | yes      | Zone ID     |

Returns: list with `id`, `description`, `action`, `filter`, `priority`, `paused`.

### Create firewall rule

```
cloudflare create_firewall_rule --zone_id abc123 --description "Block bad bots" --action block --filter "(http.user_agent contains \"BadBot\")"
```

| Argument      | Type   | Required | Default | Description                                |
|---------------|--------|----------|---------|--------------------------------------------|
| `zone_id`     | string | yes      |         | Zone ID                                    |
| `description` | string | yes      |         | Human-readable description                 |
| `action`      | string | yes      |         | `block`, `challenge`, `js_challenge`, `allow`, `log`, `bypass` |
| `filter`      | string | yes      |         | Firewall filter expression                 |
| `priority`    | int    | no       |         | Rule priority (lower = higher priority)    |
| `paused`      | boolean| no       | false   | Create in paused state                     |

Returns: `id`, `description`, `action`, `filter`.

### Update firewall rule

```
cloudflare update_firewall_rule --zone_id abc123 --rule_id def456 --action challenge --paused false
```

| Argument      | Type    | Required | Description                    |
|---------------|---------|----------|--------------------------------|
| `zone_id`     | string  | yes      | Zone ID                        |
| `rule_id`     | string  | yes      | Firewall rule ID               |
| `description` | string  | no       | Updated description            |
| `action`      | string  | no       | Updated action                 |
| `filter`      | string  | no       | Updated filter expression      |
| `priority`    | int     | no       | Updated priority               |
| `paused`      | boolean | no       | Pause or unpause               |

Returns: updated rule with `id`, `description`, `action`, `filter`.

### Delete firewall rule

```
cloudflare delete_firewall_rule --zone_id abc123 --rule_id def456
```

| Argument  | Type   | Required | Description      |
|-----------|--------|----------|------------------|
| `zone_id` | string | yes      | Zone ID          |
| `rule_id` | string | yes      | Firewall rule ID |

Returns: confirmation with `id`.

## Analytics

### Get zone analytics

```
cloudflare get_zone_analytics --zone_id abc123 --since -1440 --continuous true
```

| Argument     | Type    | Required | Default | Description                              |
|--------------|---------|----------|---------|------------------------------------------|
| `zone_id`    | string  | yes      |         | Zone ID                                  |
| `since`      | int     | no       | -1440   | Minutes relative to now (negative) or Unix timestamp |
| `until`      | int     | no       |         | End time (same format as since)          |
| `continuous` | boolean | no       | true    | Continuous time series                   |

Returns: `requests` (total, cached, uncached), `bandwidth` (total, cached, uncached), `threats`, `pageviews`, `uniques`, `status_codes`.

### Get DNS analytics

```
cloudflare get_dns_analytics --zone_id abc123 --since -1440
```

| Argument  | Type   | Required | Default | Description                                          |
|-----------|--------|----------|---------|------------------------------------------------------|
| `zone_id` | string | yes      |         | Zone ID                                              |
| `since`   | int    | no       | -1440   | Minutes relative to now (negative) or Unix timestamp |
| `until`   | int    | no       |         | End time                                             |

Returns: `query_count`, `response_codes`, `query_types`, `top_records`.

## Settings

### Get zone settings

```
cloudflare get_zone_settings --zone_id abc123
```

| Argument  | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `zone_id` | string | yes      | Zone ID     |

Returns: all zone settings as key-value pairs including `always_use_https`, `min_tls_version`, `automatic_https_rewrites`, `browser_check`, `security_level`, `waf`, `minify`.

### Update zone setting

```
cloudflare update_zone_setting --zone_id abc123 --setting always_use_https --value on
```

| Argument  | Type   | Required | Description                                   |
|-----------|--------|----------|-----------------------------------------------|
| `zone_id` | string | yes      | Zone ID                                       |
| `setting` | string | yes      | Setting name (e.g. `always_use_https`, `minify`, `security_level`) |
| `value`   | string | yes      | New value for the setting                     |

Returns: `setting`, `value`, `modified_on`.

## Workflow

1. **Find your zone** with `cloudflare list_zones` to get the `zone_id`.
2. **Manage DNS** with `list_dns_records`, `create_dns_record`, `update_dns_record` to configure domains.
3. **Deploy edge logic** with Workers: `deploy_worker` with routes to handle requests at the edge.
4. **Deploy static sites** with Pages: `create_pages_project` then `deploy_pages`.
5. **Secure your site** with SSL settings (`update_ssl_settings --mode strict`) and firewall rules.
6. **Purge cache** after deployments with `purge_cache --purge_everything true` or targeted URL purges.
7. **Monitor traffic** with `get_zone_analytics` and `get_dns_analytics`.

## Safety notes

- Zone IDs and record IDs are opaque strings. Always discover them via `list_zones` and `list_dns_records`.
- `purge_cache --purge_everything` clears the entire CDN cache. This causes a temporary increase in origin load.
- DNS changes propagate globally. Incorrect records can cause downtime. Verify values before creating or updating.
- `delete_dns_record` is irreversible. Confirm the record ID and content before deleting.
- Firewall rules with `block` action immediately drop matching traffic. Test with `log` action first.
- SSL mode `off` or `flexible` does not encrypt traffic between Cloudflare and the origin. Prefer `full` or `strict`.
- Worker deployments are live immediately. Test scripts locally before deploying to production routes.
- Cloudflare API rate limit is 1200 requests per 5 minutes per user.
