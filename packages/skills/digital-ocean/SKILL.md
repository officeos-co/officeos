# DigitalOcean

Manage DigitalOcean cloud infrastructure: Droplets, Databases, Domains, Kubernetes clusters, Spaces, Networking, Apps, and account resources via the DigitalOcean API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Droplets

### List droplets

```
digitalocean list_droplets --tag web --per_page 20
```

| Argument   | Type   | Required | Default | Description                  |
|------------|--------|----------|---------|------------------------------|
| `tag`      | string | no       |         | Filter by tag name           |
| `per_page` | int    | no       | 20      | Results per page (1-200)     |

Returns: array of `id`, `name`, `status`, `region`, `size`, `image`, `ip_address`, `private_ip`, `vcpus`, `memory`, `disk`, `tags`, `created_at`.

### Get droplet

```
digitalocean get_droplet --droplet_id 12345678
```

| Argument     | Type | Required | Description |
|--------------|------|----------|-------------|
| `droplet_id` | int  | yes      | Droplet ID  |

Returns: `id`, `name`, `status`, `region`, `size`, `image`, `ip_address`, `private_ip`, `vcpus`, `memory`, `disk`, `tags`, `volumes`, `vpc_uuid`, `created_at`, `kernel`, `features`, `networks`.

### Create droplet

```
digitalocean create_droplet --name web-1 --region nyc3 --size s-1vcpu-1gb --image ubuntu-24-04-x64 --ssh_keys '[12345]' --tags '["web","prod"]'
```

| Argument       | Type     | Required | Default | Description                              |
|----------------|----------|----------|---------|------------------------------------------|
| `name`         | string   | yes      |         | Droplet name                             |
| `region`       | string   | yes      |         | Region slug (e.g. `nyc3`, `sfo3`)        |
| `size`         | string   | yes      |         | Size slug (e.g. `s-1vcpu-1gb`)           |
| `image`        | string   | yes      |         | Image slug or ID                         |
| `ssh_keys`     | int[]    | no       |         | Array of SSH key IDs                     |
| `backups`      | boolean  | no       | false   | Enable automated backups                 |
| `ipv6`         | boolean  | no       | false   | Enable IPv6                              |
| `monitoring`   | boolean  | no       | false   | Enable monitoring agent                  |
| `user_data`    | string   | no       |         | Cloud-init user data script              |
| `tags`         | string[] | no       |         | Tags to apply                            |
| `vpc_uuid`     | string   | no       |         | VPC UUID                                 |
| `volumes`      | string[] | no       |         | Volume IDs to attach                     |

Returns: `id`, `name`, `status`, `region`, `ip_address`, `created_at`.

### Delete droplet

```
digitalocean delete_droplet --droplet_id 12345678
```

| Argument     | Type | Required | Description |
|--------------|------|----------|-------------|
| `droplet_id` | int  | yes      | Droplet ID  |

Returns: `deleted` (boolean).

### Power on

```
digitalocean power_on --droplet_id 12345678
```

| Argument     | Type | Required | Description |
|--------------|------|----------|-------------|
| `droplet_id` | int  | yes      | Droplet ID  |

Returns: `action_id`, `status`, `type`, `started_at`.

### Power off

```
digitalocean power_off --droplet_id 12345678
```

| Argument     | Type | Required | Description |
|--------------|------|----------|-------------|
| `droplet_id` | int  | yes      | Droplet ID  |

Returns: `action_id`, `status`, `type`, `started_at`.

### Reboot

```
digitalocean reboot --droplet_id 12345678
```

| Argument     | Type | Required | Description |
|--------------|------|----------|-------------|
| `droplet_id` | int  | yes      | Droplet ID  |

Returns: `action_id`, `status`, `type`, `started_at`.

### Resize

```
digitalocean resize --droplet_id 12345678 --size s-2vcpu-4gb --disk true
```

| Argument     | Type    | Required | Default | Description                          |
|--------------|---------|----------|---------|--------------------------------------|
| `droplet_id` | int     | yes      |         | Droplet ID                           |
| `size`       | string  | yes      |         | New size slug                        |
| `disk`       | boolean | no       | false   | Also resize disk (irreversible)      |

Returns: `action_id`, `status`, `type`, `started_at`.

### Snapshot

```
digitalocean snapshot --droplet_id 12345678 --name "pre-deploy-snapshot"
```

| Argument     | Type   | Required | Description              |
|--------------|--------|----------|--------------------------|
| `droplet_id` | int    | yes      | Droplet ID               |
| `name`       | string | yes      | Snapshot name             |

Returns: `action_id`, `status`, `type`, `started_at`.

## Databases

### List databases

```
digitalocean list_databases
```

| Argument   | Type   | Required | Default | Description              |
|------------|--------|----------|---------|--------------------------|
| `per_page` | int    | no       | 20      | Results per page (1-200) |

Returns: array of `id`, `name`, `engine`, `version`, `size`, `region`, `status`, `num_nodes`, `created_at`, `connection_uri`, `private_connection_uri`.

### Get database

```
digitalocean get_database --database_id a1b2c3d4-5678-90ab-cdef
```

| Argument      | Type   | Required | Description          |
|---------------|--------|----------|----------------------|
| `database_id` | string | yes      | Database cluster ID  |

Returns: `id`, `name`, `engine`, `version`, `size`, `region`, `status`, `num_nodes`, `created_at`, `connection`, `private_connection`, `maintenance_window`, `db_names`, `users`.

### Create database

```
digitalocean create_database --name my-pg --engine pg --region nyc3 --size db-s-1vcpu-1gb --num_nodes 1
```

| Argument    | Type   | Required | Default | Description                                   |
|-------------|--------|----------|---------|-----------------------------------------------|
| `name`      | string | yes      |         | Cluster name                                  |
| `engine`    | string | yes      |         | `pg`, `mysql`, `redis`, `mongodb`, `kafka`    |
| `region`    | string | yes      |         | Region slug                                   |
| `size`      | string | yes      |         | Size slug (e.g. `db-s-1vcpu-1gb`)             |
| `num_nodes` | int    | no       | 1       | Number of nodes (1-3)                         |
| `version`   | string | no       |         | Engine version (e.g. `16` for Postgres 16)    |
| `tags`      | string[]| no      |         | Tags to apply                                 |

Returns: `id`, `name`, `engine`, `status`, `connection_uri`, `created_at`.

### Delete database

```
digitalocean delete_database --database_id a1b2c3d4-5678-90ab-cdef
```

| Argument      | Type   | Required | Description          |
|---------------|--------|----------|----------------------|
| `database_id` | string | yes      | Database cluster ID  |

Returns: `deleted` (boolean).

### List connection pools

```
digitalocean list_connection_pools --database_id a1b2c3d4-5678-90ab-cdef
```

| Argument      | Type   | Required | Description          |
|---------------|--------|----------|----------------------|
| `database_id` | string | yes      | Database cluster ID  |

Returns: array of `name`, `mode`, `size`, `db`, `user`, `connection_uri`.

### Create connection pool

```
digitalocean create_connection_pool --database_id a1b2c3d4-5678-90ab-cdef --name my-pool --mode transaction --size 10 --db defaultdb --user doadmin
```

| Argument      | Type   | Required | Default       | Description                               |
|---------------|--------|----------|---------------|-------------------------------------------|
| `database_id` | string | yes      |               | Database cluster ID                       |
| `name`        | string | yes      |               | Pool name                                 |
| `mode`        | string | no       | `transaction` | `session`, `transaction`, `statement`     |
| `size`        | int    | yes      |               | Pool size                                 |
| `db`          | string | yes      |               | Database name                             |
| `user`        | string | yes      |               | Database user                             |

Returns: `name`, `mode`, `size`, `connection_uri`.

### List database users

```
digitalocean list_db_users --database_id a1b2c3d4-5678-90ab-cdef
```

| Argument      | Type   | Required | Description          |
|---------------|--------|----------|----------------------|
| `database_id` | string | yes      | Database cluster ID  |

Returns: array of `name`, `role`, `password`.

## Domains

### List domains

```
digitalocean list_domains
```

| Argument   | Type | Required | Default | Description              |
|------------|------|----------|---------|--------------------------|
| `per_page` | int  | no       | 20      | Results per page (1-200) |

Returns: array of `name`, `ttl`, `zone_file`.

### Get domain

```
digitalocean get_domain --domain_name example.com
```

| Argument      | Type   | Required | Description      |
|---------------|--------|----------|------------------|
| `domain_name` | string | yes      | Domain name      |

Returns: `name`, `ttl`, `zone_file`.

### Create domain

```
digitalocean create_domain --domain_name example.com --ip_address 1.2.3.4
```

| Argument      | Type   | Required | Default | Description                     |
|---------------|--------|----------|---------|---------------------------------|
| `domain_name` | string | yes      |         | Domain name                     |
| `ip_address`  | string | no       |         | IP for automatic A record       |

Returns: `name`, `ttl`.

### Delete domain

```
digitalocean delete_domain --domain_name example.com
```

| Argument      | Type   | Required | Description      |
|---------------|--------|----------|------------------|
| `domain_name` | string | yes      | Domain name      |

Returns: `deleted` (boolean).

### List records

```
digitalocean list_records --domain_name example.com --type A
```

| Argument      | Type   | Required | Default | Description                              |
|---------------|--------|----------|---------|------------------------------------------|
| `domain_name` | string | yes      |         | Domain name                              |
| `type`        | string | no       |         | Filter by type: `A`, `AAAA`, `CNAME`, `MX`, `TXT`, `NS`, `SRV` |

Returns: array of `id`, `type`, `name`, `data`, `priority`, `port`, `ttl`, `weight`.

### Create record

```
digitalocean create_record --domain_name example.com --type A --name api --data 1.2.3.4 --ttl 300
```

| Argument      | Type   | Required | Default | Description                              |
|---------------|--------|----------|---------|------------------------------------------|
| `domain_name` | string | yes      |         | Domain name                              |
| `type`        | string | yes      |         | Record type (`A`, `AAAA`, `CNAME`, `MX`, `TXT`, `NS`, `SRV`) |
| `name`        | string | yes      |         | Record name (e.g. `api`, `@`)            |
| `data`        | string | yes      |         | Record value                             |
| `ttl`         | int    | no       | 1800    | TTL in seconds                           |
| `priority`    | int    | no       |         | Priority (MX and SRV records)            |
| `port`        | int    | no       |         | Port (SRV records)                       |
| `weight`      | int    | no       |         | Weight (SRV records)                     |

Returns: `id`, `type`, `name`, `data`, `ttl`.

### Update record

```
digitalocean update_record --domain_name example.com --record_id 12345 --data 5.6.7.8
```

| Argument      | Type   | Required | Description                   |
|---------------|--------|----------|-------------------------------|
| `domain_name` | string | yes      | Domain name                   |
| `record_id`   | int    | yes      | Record ID                     |
| `name`        | string | no       | Updated record name           |
| `data`        | string | no       | Updated record value          |
| `ttl`         | int    | no       | Updated TTL                   |
| `priority`    | int    | no       | Updated priority              |

Returns: `id`, `type`, `name`, `data`, `ttl`.

### Delete record

```
digitalocean delete_record --domain_name example.com --record_id 12345
```

| Argument      | Type   | Required | Description      |
|---------------|--------|----------|------------------|
| `domain_name` | string | yes      | Domain name      |
| `record_id`   | int    | yes      | Record ID        |

Returns: `deleted` (boolean).

## Kubernetes

### List clusters

```
digitalocean list_clusters
```

| Argument   | Type | Required | Default | Description              |
|------------|------|----------|---------|--------------------------|
| `per_page` | int  | no       | 20      | Results per page (1-200) |

Returns: array of `id`, `name`, `region`, `version`, `status`, `endpoint`, `node_pools`, `created_at`, `auto_upgrade`, `surge_upgrade`.

### Get cluster

```
digitalocean get_cluster --cluster_id a1b2c3d4-5678-90ab-cdef
```

| Argument     | Type   | Required | Description          |
|--------------|--------|----------|----------------------|
| `cluster_id` | string | yes      | Kubernetes cluster ID|

Returns: `id`, `name`, `region`, `version`, `status`, `endpoint`, `ipv4`, `node_pools`, `maintenance_policy`, `auto_upgrade`, `surge_upgrade`, `created_at`, `updated_at`.

### Create cluster

```
digitalocean create_cluster --name my-k8s --region nyc3 --version 1.29.1-do.0 --node_pool_name worker --node_pool_size s-2vcpu-4gb --node_pool_count 3
```

| Argument            | Type     | Required | Default | Description                          |
|---------------------|----------|----------|---------|--------------------------------------|
| `name`              | string   | yes      |         | Cluster name                         |
| `region`            | string   | yes      |         | Region slug                          |
| `version`           | string   | yes      |         | Kubernetes version slug              |
| `node_pool_name`    | string   | yes      |         | Default node pool name               |
| `node_pool_size`    | string   | yes      |         | Node size slug                       |
| `node_pool_count`   | int      | yes      |         | Number of nodes                      |
| `node_pool_tags`    | string[] | no       |         | Tags for nodes                       |
| `auto_upgrade`      | boolean  | no       | false   | Enable auto version upgrades         |
| `surge_upgrade`     | boolean  | no       | false   | Enable surge upgrades                |
| `vpc_uuid`          | string   | no       |         | VPC UUID                             |
| `tags`              | string[] | no       |         | Tags for the cluster                 |

Returns: `id`, `name`, `region`, `version`, `status`, `endpoint`, `created_at`.

### Delete cluster

```
digitalocean delete_cluster --cluster_id a1b2c3d4-5678-90ab-cdef
```

| Argument     | Type   | Required | Description          |
|--------------|--------|----------|----------------------|
| `cluster_id` | string | yes      | Kubernetes cluster ID|

Returns: `deleted` (boolean).

### Get kubeconfig

```
digitalocean get_kubeconfig --cluster_id a1b2c3d4-5678-90ab-cdef
```

| Argument     | Type   | Required | Description          |
|--------------|--------|----------|----------------------|
| `cluster_id` | string | yes      | Kubernetes cluster ID|

Returns: `kubeconfig` (YAML string), `expires_at`.

### List node pools

```
digitalocean list_node_pools --cluster_id a1b2c3d4-5678-90ab-cdef
```

| Argument     | Type   | Required | Description          |
|--------------|--------|----------|----------------------|
| `cluster_id` | string | yes      | Kubernetes cluster ID|

Returns: array of `id`, `name`, `size`, `count`, `tags`, `auto_scale`, `min_nodes`, `max_nodes`, `nodes`.

## Spaces (S3-compatible)

### List spaces

```
digitalocean list_spaces --region nyc3
```

| Argument | Type   | Required | Default | Description                |
|----------|--------|----------|---------|----------------------------|
| `region` | string | no       |         | Filter by region           |

Returns: array of `name`, `region`, `created_at`.

### Create space

```
digitalocean create_space --name my-space --region nyc3
```

| Argument | Type   | Required | Description          |
|----------|--------|----------|----------------------|
| `name`   | string | yes      | Space name           |
| `region` | string | yes      | Region slug          |

Returns: `name`, `region`, `created_at`.

### Delete space

```
digitalocean delete_space --name my-space --region nyc3
```

| Argument | Type   | Required | Description          |
|----------|--------|----------|----------------------|
| `name`   | string | yes      | Space name           |
| `region` | string | yes      | Region slug          |

Returns: `deleted` (boolean).

### List objects

```
digitalocean list_objects --space my-space --region nyc3 --prefix uploads/ --max_keys 100
```

| Argument   | Type   | Required | Default | Description                   |
|------------|--------|----------|---------|-------------------------------|
| `space`    | string | yes      |         | Space name                    |
| `region`   | string | yes      |         | Region slug                   |
| `prefix`   | string | no       |         | Key prefix filter             |
| `max_keys` | int    | no       | 1000    | Maximum objects to return     |

Returns: array of `key`, `size`, `last_modified`, `etag`.

### Put object

```
digitalocean put_object --space my-space --region nyc3 --key data/config.json --body '{"env":"prod"}' --content_type application/json
```

| Argument       | Type   | Required | Default                | Description          |
|----------------|--------|----------|------------------------|----------------------|
| `space`        | string | yes      |                        | Space name           |
| `region`       | string | yes      |                        | Region slug          |
| `key`          | string | yes      |                        | Object key           |
| `body`         | string | yes      |                        | Object content       |
| `content_type` | string | no       | `application/octet-stream` | MIME type        |
| `acl`          | string | no       | `private`              | `private` or `public-read` |

Returns: `etag`, `key`.

### Get object

```
digitalocean get_object --space my-space --region nyc3 --key data/config.json
```

| Argument | Type   | Required | Description          |
|----------|--------|----------|----------------------|
| `space`  | string | yes      | Space name           |
| `region` | string | yes      | Region slug          |
| `key`    | string | yes      | Object key           |

Returns: `content_type`, `content_length`, `last_modified`, `body` (text content or base64 for binary).

### Delete object

```
digitalocean delete_object --space my-space --region nyc3 --key old-file.txt
```

| Argument | Type   | Required | Description          |
|----------|--------|----------|----------------------|
| `space`  | string | yes      | Space name           |
| `region` | string | yes      | Region slug          |
| `key`    | string | yes      | Object key           |

Returns: `deleted` (boolean).

## Networking

### List firewalls

```
digitalocean list_firewalls --per_page 20
```

| Argument   | Type | Required | Default | Description              |
|------------|------|----------|---------|--------------------------|
| `per_page` | int  | no       | 20      | Results per page (1-200) |

Returns: array of `id`, `name`, `status`, `inbound_rules`, `outbound_rules`, `droplet_ids`, `tags`, `created_at`.

### Create firewall

```
digitalocean create_firewall --name web-fw --inbound_rules '[{"protocol":"tcp","ports":"80","sources":{"addresses":["0.0.0.0/0"]}}]' --outbound_rules '[{"protocol":"tcp","ports":"all","destinations":{"addresses":["0.0.0.0/0"]}}]' --droplet_ids '[12345678]'
```

| Argument        | Type     | Required | Description                                |
|-----------------|----------|----------|--------------------------------------------|
| `name`          | string   | yes      | Firewall name                              |
| `inbound_rules` | string   | yes      | JSON array of inbound rules                |
| `outbound_rules`| string   | yes      | JSON array of outbound rules               |
| `droplet_ids`   | int[]    | no       | Droplet IDs to apply to                    |
| `tags`          | string[] | no       | Tags to apply to (applies to tagged droplets) |

Returns: `id`, `name`, `status`, `created_at`.

### List load balancers

```
digitalocean list_load_balancers --per_page 20
```

| Argument   | Type | Required | Default | Description              |
|------------|------|----------|---------|--------------------------|
| `per_page` | int  | no       | 20      | Results per page (1-200) |

Returns: array of `id`, `name`, `ip`, `status`, `region`, `algorithm`, `forwarding_rules`, `health_check`, `droplet_ids`, `created_at`.

### Create load balancer

```
digitalocean create_load_balancer --name web-lb --region nyc3 --forwarding_rules '[{"entry_protocol":"http","entry_port":80,"target_protocol":"http","target_port":8080}]' --droplet_ids '[12345678]'
```

| Argument           | Type     | Required | Default        | Description                         |
|--------------------|----------|----------|----------------|-------------------------------------|
| `name`             | string   | yes      |                | Load balancer name                  |
| `region`           | string   | yes      |                | Region slug                         |
| `forwarding_rules` | string   | yes      |                | JSON array of forwarding rules      |
| `droplet_ids`      | int[]    | no       |                | Droplet IDs to balance across       |
| `tag`              | string   | no       |                | Tag to auto-include droplets        |
| `algorithm`        | string   | no       | `round_robin`  | `round_robin` or `least_connections`|
| `health_check`     | string   | no       |                | JSON health check config            |
| `vpc_uuid`         | string   | no       |                | VPC UUID                            |

Returns: `id`, `name`, `ip`, `status`, `region`, `created_at`.

### List floating IPs

```
digitalocean list_floating_ips
```

| Argument   | Type | Required | Default | Description              |
|------------|------|----------|---------|--------------------------|
| `per_page` | int  | no       | 20      | Results per page (1-200) |

Returns: array of `ip`, `region`, `droplet` (assigned droplet or null), `locked`.

## Apps

### List apps

```
digitalocean list_apps --per_page 20
```

| Argument   | Type | Required | Default | Description              |
|------------|------|----------|---------|--------------------------|
| `per_page` | int  | no       | 20      | Results per page (1-200) |

Returns: array of `id`, `default_ingress`, `live_url`, `active_deployment`, `spec`, `created_at`, `updated_at`.

### Get app

```
digitalocean get_app --app_id a1b2c3d4-5678-90ab-cdef
```

| Argument | Type   | Required | Description |
|----------|--------|----------|-------------|
| `app_id` | string | yes      | App ID      |

Returns: `id`, `default_ingress`, `live_url`, `active_deployment`, `spec`, `created_at`, `updated_at`, `last_deployment_active_at`.

### Create app

```
digitalocean create_app --spec '{"name":"my-app","region":"nyc","services":[{"name":"web","github":{"repo":"user/repo","branch":"main"},"run_command":"npm start","http_port":8080}]}'
```

| Argument | Type   | Required | Description                      |
|----------|--------|----------|----------------------------------|
| `spec`   | string | yes      | JSON app spec (App Platform format) |

Returns: `id`, `default_ingress`, `live_url`, `created_at`.

### Delete app

```
digitalocean delete_app --app_id a1b2c3d4-5678-90ab-cdef
```

| Argument | Type   | Required | Description |
|----------|--------|----------|-------------|
| `app_id` | string | yes      | App ID      |

Returns: `deleted` (boolean).

### List deployments

```
digitalocean list_deployments --app_id a1b2c3d4-5678-90ab-cdef --per_page 10
```

| Argument   | Type   | Required | Default | Description              |
|------------|--------|----------|---------|--------------------------|
| `app_id`   | string | yes      |         | App ID                   |
| `per_page` | int    | no       | 20      | Results per page (1-200) |

Returns: array of `id`, `cause`, `phase`, `created_at`, `updated_at`, `progress`.

## Account

### Get account

```
digitalocean get_account
```

No arguments.

Returns: `email`, `uuid`, `droplet_limit`, `floating_ip_limit`, `volume_limit`, `status`, `team`.

### List SSH keys

```
digitalocean list_ssh_keys --per_page 20
```

| Argument   | Type | Required | Default | Description              |
|------------|------|----------|---------|--------------------------|
| `per_page` | int  | no       | 20      | Results per page (1-200) |

Returns: array of `id`, `name`, `fingerprint`, `public_key`.

### Add SSH key

```
digitalocean add_ssh_key --name my-key --public_key "ssh-ed25519 AAAA..."
```

| Argument     | Type   | Required | Description              |
|--------------|--------|----------|--------------------------|
| `name`       | string | yes      | Key name                 |
| `public_key` | string | yes      | Public key content       |

Returns: `id`, `name`, `fingerprint`.

### List regions

```
digitalocean list_regions
```

No arguments.

Returns: array of `slug`, `name`, `available`, `sizes`, `features`.

### List sizes

```
digitalocean list_sizes
```

No arguments.

Returns: array of `slug`, `memory`, `vcpus`, `disk`, `transfer`, `price_monthly`, `price_hourly`, `available`, `regions`.

### List images

```
digitalocean list_images --type distribution --per_page 20
```

| Argument   | Type   | Required | Default | Description                                  |
|------------|--------|----------|---------|----------------------------------------------|
| `type`     | string | no       |         | `distribution`, `application`, or `user`     |
| `per_page` | int    | no       | 20      | Results per page (1-200)                     |

Returns: array of `id`, `name`, `slug`, `distribution`, `type`, `regions`, `min_disk_size`, `size_gigabytes`, `created_at`, `status`.

## Workflow

1. **Start with `get_account`** to verify API access and check resource limits.
2. Use `list_regions` and `list_sizes` to discover available infrastructure options before creating resources.
3. For Droplets: use `list_images` to find the right image, then `create_droplet`. Always attach SSH keys for access.
4. For Databases: choose the right engine and size with `create_database`. Use `list_connection_pools` and `list_db_users` to manage access.
5. For Domains: create the domain first with `create_domain`, then add records with `create_record`.
6. For Kubernetes: use `create_cluster`, then `get_kubeconfig` to retrieve access credentials.
7. For Spaces: use like S3 -- `create_space`, then `put_object`/`get_object` for file operations.
8. For Apps: provide a complete app spec JSON to `create_app`. Monitor with `list_deployments`.
9. Always set up firewalls with `create_firewall` to secure Droplets.

## Safety notes

- **Destructive operations** (`delete_droplet`, `delete_database`, `delete_cluster`, `delete_domain`, `delete_app`) cannot be undone. Confirm with the user before executing.
- `delete_space` requires the space to be empty. Delete all objects first.
- `resize --disk true` permanently increases disk size and cannot be reversed.
- `power_off` performs a hard shutdown. Prefer a graceful OS shutdown when possible.
- Database `delete_database` destroys all data including backups. Consider creating a snapshot first.
- Kubernetes `delete_cluster` destroys all workloads and volumes. Download kubeconfig and back up persistent volumes before deleting.
- Firewall rules take effect immediately. Misconfigured rules can lock you out of Droplets.
- SSH keys cannot be added to existing Droplets after creation. Plan key access before creating Droplets.
- All operations are subject to account resource limits. Check `get_account` for current limits.
- Results are paginated. Use `per_page` to control page size (maximum 200).
- DigitalOcean API is rate-limited to 5000 requests per hour.
