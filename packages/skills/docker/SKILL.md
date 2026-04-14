# Docker

Manage Docker containers, images, volumes, networks, and Compose stacks via the Docker Engine API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Containers

### List containers

```
docker list_containers --all true --limit 20
```

| Argument | Type    | Required | Default | Description                        |
|----------|---------|----------|---------|------------------------------------|
| `all`    | boolean | no       | false   | Include stopped containers         |
| `limit`  | int     | no       | 0       | Max containers to return (0 = all) |
| `filter` | string  | no       |         | Filter expression (e.g. `status=running`, `name=myapp`) |

Returns: `id`, `name`, `image`, `status`, `state`, `ports`, `created`.

### Get container

```
docker get_container --container_id abc123
```

| Argument       | Type   | Required | Description              |
|----------------|--------|----------|--------------------------|
| `container_id` | string | yes      | Container ID or name     |

Returns: full container details including `config`, `network_settings`, `mounts`, `state`.

### Run container

```
docker run --image nginx:latest --name my-nginx --ports '["8080:80"]' --env '["NGINX_HOST=example.com"]' --detach true
```

| Argument  | Type     | Required | Default | Description                              |
|-----------|----------|----------|---------|------------------------------------------|
| `image`   | string   | yes      |         | Image to run (e.g. `nginx:latest`)       |
| `name`    | string   | no       |         | Container name                           |
| `ports`   | string[] | no       |         | Port mappings (`host:container`)         |
| `env`     | string[] | no       |         | Environment variables (`KEY=VALUE`)      |
| `volumes` | string[] | no       |         | Volume mounts (`host:container[:ro]`)    |
| `detach`  | boolean  | no       | true    | Run in background                        |
| `network` | string   | no       |         | Network to connect to                    |
| `command` | string   | no       |         | Override default command                 |

Returns: `container_id`, `name`, `status`.

### Stop container

```
docker stop --container_id abc123 --timeout 10
```

| Argument       | Type   | Required | Default | Description                          |
|----------------|--------|----------|---------|--------------------------------------|
| `container_id` | string | yes      |         | Container ID or name                 |
| `timeout`      | int    | no       | 10      | Seconds to wait before killing       |

Returns: confirmation with `container_id` and `status`.

### Start container

```
docker start --container_id abc123
```

| Argument       | Type   | Required | Description          |
|----------------|--------|----------|----------------------|
| `container_id` | string | yes      | Container ID or name |

Returns: confirmation with `container_id` and `status`.

### Restart container

```
docker restart --container_id abc123 --timeout 10
```

| Argument       | Type   | Required | Default | Description                    |
|----------------|--------|----------|---------|--------------------------------|
| `container_id` | string | yes      |         | Container ID or name           |
| `timeout`      | int    | no       | 10      | Seconds to wait before killing |

Returns: confirmation with `container_id` and `status`.

### Remove container

```
docker rm --container_id abc123 --force true --volumes true
```

| Argument       | Type    | Required | Default | Description                       |
|----------------|---------|----------|---------|-----------------------------------|
| `container_id` | string  | yes      |         | Container ID or name              |
| `force`        | boolean | no       | false   | Force remove running container    |
| `volumes`      | boolean | no       | false   | Remove associated anonymous volumes |

Returns: confirmation with `container_id`.

### Container logs

```
docker logs --container_id abc123 --tail 100 --follow false --timestamps true
```

| Argument       | Type    | Required | Default | Description                    |
|----------------|---------|----------|---------|--------------------------------|
| `container_id` | string  | yes      |         | Container ID or name           |
| `tail`         | int     | no       | 200     | Number of lines from the end   |
| `follow`       | boolean | no       | false   | Stream logs in real time       |
| `timestamps`   | boolean | no       | false   | Show timestamps                |
| `since`        | string  | no       |         | Logs since timestamp or duration (e.g. `10m`, `2024-01-01T00:00:00Z`) |

Returns: log output as text.

### Exec in container

```
docker exec --container_id abc123 --command "ls -la /app" --workdir /app
```

| Argument       | Type   | Required | Default | Description                     |
|----------------|--------|----------|---------|---------------------------------|
| `container_id` | string | yes      |         | Container ID or name            |
| `command`      | string | yes      |         | Command to execute              |
| `workdir`      | string | no       |         | Working directory inside container |

Returns: `exit_code`, `stdout`, `stderr`.

### Inspect container

```
docker inspect --container_id abc123
```

| Argument       | Type   | Required | Description          |
|----------------|--------|----------|----------------------|
| `container_id` | string | yes      | Container ID or name |

Returns: full JSON inspection output including `config`, `network_settings`, `mounts`, `state`, `host_config`.

### Container stats

```
docker stats --container_id abc123
```

| Argument       | Type   | Required | Description          |
|----------------|--------|----------|----------------------|
| `container_id` | string | yes      | Container ID or name |

Returns: `cpu_percent`, `memory_usage`, `memory_limit`, `memory_percent`, `network_rx`, `network_tx`, `block_read`, `block_write`, `pids`.

## Images

### List images

```
docker list_images --all false
```

| Argument | Type    | Required | Default | Description                 |
|----------|---------|----------|---------|-----------------------------|
| `all`    | boolean | no       | false   | Include intermediate images |
| `filter` | string  | no       |         | Filter expression (e.g. `dangling=true`) |

Returns: `id`, `repo_tags`, `size`, `created`.

### Pull image

```
docker pull --image nginx:latest
```

| Argument | Type   | Required | Description                        |
|----------|--------|----------|------------------------------------|
| `image`  | string | yes      | Image to pull (e.g. `nginx:1.25`) |

Returns: `status`, `image`, `digest`.

### Push image

```
docker push --image harkro123/myapp:latest
```

| Argument | Type   | Required | Description         |
|----------|--------|----------|---------------------|
| `image`  | string | yes      | Image to push       |

Returns: `status`, `digest`.

### Build image

```
docker build --path ./app --tag myapp:latest --dockerfile Dockerfile --no_cache false
```

| Argument     | Type    | Required | Default      | Description                     |
|--------------|---------|----------|--------------|---------------------------------|
| `path`       | string  | yes      |              | Build context path              |
| `tag`        | string  | yes      |              | Image tag                       |
| `dockerfile` | string  | no       | `Dockerfile` | Dockerfile path                 |
| `no_cache`   | boolean | no       | false        | Build without cache             |
| `build_args` | string  | no       |              | JSON object of build arguments  |

Returns: `image_id`, `tag`, `size`.

### Tag image

```
docker tag --source myapp:latest --target harkro123/myapp:v1.0
```

| Argument | Type   | Required | Description      |
|----------|--------|----------|------------------|
| `source` | string | yes      | Source image tag  |
| `target` | string | yes      | Target image tag  |

Returns: confirmation with `source` and `target`.

### Remove image

```
docker rm_image --image myapp:latest --force false
```

| Argument | Type    | Required | Default | Description             |
|----------|---------|----------|---------|-------------------------|
| `image`  | string  | yes      |         | Image ID or tag         |
| `force`  | boolean | no       | false   | Force remove            |

Returns: `deleted` image layers list.

### Inspect image

```
docker inspect_image --image nginx:latest
```

| Argument | Type   | Required | Description     |
|----------|--------|----------|-----------------|
| `image`  | string | yes      | Image ID or tag |

Returns: full image metadata including `config`, `layers`, `os`, `architecture`, `size`.

### Image history

```
docker history --image nginx:latest
```

| Argument | Type   | Required | Description     |
|----------|--------|----------|-----------------|
| `image`  | string | yes      | Image ID or tag |

Returns: list of layers with `created_by`, `size`, `created`.

## Volumes

### List volumes

```
docker list_volumes
```

| Argument | Type   | Required | Description                              |
|----------|--------|----------|------------------------------------------|
| `filter` | string | no       | Filter expression (e.g. `dangling=true`) |

Returns: `name`, `driver`, `mountpoint`, `created`.

### Create volume

```
docker create_volume --name my-data --driver local
```

| Argument | Type   | Required | Default | Description      |
|----------|--------|----------|---------|------------------|
| `name`   | string | yes      |         | Volume name      |
| `driver` | string | no       | `local` | Volume driver    |
| `labels` | string | no       |         | JSON object of labels |

Returns: `name`, `driver`, `mountpoint`.

### Remove volume

```
docker rm_volume --name my-data --force false
```

| Argument | Type    | Required | Default | Description           |
|----------|---------|----------|---------|-----------------------|
| `name`   | string  | yes      |         | Volume name           |
| `force`  | boolean | no       | false   | Force remove          |

Returns: confirmation with `name`.

### Inspect volume

```
docker inspect_volume --name my-data
```

| Argument | Type   | Required | Description |
|----------|--------|----------|-------------|
| `name`   | string | yes      | Volume name |

Returns: `name`, `driver`, `mountpoint`, `labels`, `options`, `created`.

## Networks

### List networks

```
docker list_networks
```

| Argument | Type   | Required | Description                                  |
|----------|--------|----------|----------------------------------------------|
| `filter` | string | no       | Filter expression (e.g. `driver=bridge`)     |

Returns: `id`, `name`, `driver`, `scope`, `containers`.

### Create network

```
docker create_network --name my-network --driver bridge --subnet 172.20.0.0/16
```

| Argument   | Type    | Required | Default  | Description              |
|------------|---------|----------|----------|--------------------------|
| `name`     | string  | yes      |          | Network name             |
| `driver`   | string  | no       | `bridge` | Network driver           |
| `subnet`   | string  | no       |          | Subnet CIDR              |
| `gateway`  | string  | no       |          | Gateway address          |
| `internal` | boolean | no       | false    | Restrict external access |

Returns: `id`, `name`, `driver`.

### Remove network

```
docker rm_network --name my-network
```

| Argument | Type   | Required | Description  |
|----------|--------|----------|--------------|
| `name`   | string | yes      | Network name |

Returns: confirmation with `name`.

### Connect container to network

```
docker connect --network my-network --container_id abc123
```

| Argument       | Type   | Required | Description          |
|----------------|--------|----------|----------------------|
| `network`      | string | yes      | Network name or ID   |
| `container_id` | string | yes      | Container ID or name |

Returns: confirmation.

### Disconnect container from network

```
docker disconnect --network my-network --container_id abc123 --force false
```

| Argument       | Type    | Required | Default | Description          |
|----------------|---------|----------|---------|----------------------|
| `network`      | string  | yes      |         | Network name or ID   |
| `container_id` | string  | yes      |         | Container ID or name |
| `force`        | boolean | no       | false   | Force disconnect     |

Returns: confirmation.

## Compose

### Compose up

```
docker compose_up --project_dir ./myapp --detach true --build true
```

| Argument      | Type    | Required | Default | Description                       |
|---------------|---------|----------|---------|-----------------------------------|
| `project_dir` | string  | yes      |         | Path to docker-compose.yml        |
| `detach`      | boolean | no       | true    | Run in background                 |
| `build`       | boolean | no       | false   | Build images before starting      |
| `services`    | string  | no       |         | Comma-separated list of services  |

Returns: list of started services with `name` and `status`.

### Compose down

```
docker compose_down --project_dir ./myapp --volumes true --remove_orphans true
```

| Argument         | Type    | Required | Default | Description                    |
|------------------|---------|----------|---------|--------------------------------|
| `project_dir`    | string  | yes      |         | Path to docker-compose.yml     |
| `volumes`        | boolean | no       | false   | Remove named volumes           |
| `remove_orphans` | boolean | no       | false   | Remove orphan containers       |

Returns: list of stopped services.

### Compose ps

```
docker compose_ps --project_dir ./myapp
```

| Argument      | Type   | Required | Description                |
|---------------|--------|----------|----------------------------|
| `project_dir` | string | yes      | Path to docker-compose.yml |

Returns: list of services with `name`, `status`, `ports`.

### Compose logs

```
docker compose_logs --project_dir ./myapp --service api --tail 50
```

| Argument      | Type    | Required | Default | Description                    |
|---------------|---------|----------|---------|--------------------------------|
| `project_dir` | string  | yes      |         | Path to docker-compose.yml     |
| `service`     | string  | no       |         | Specific service name          |
| `tail`        | int     | no       | 200     | Number of lines from the end   |
| `follow`      | boolean | no       | false   | Stream logs in real time       |

Returns: log output as text.

### Compose build

```
docker compose_build --project_dir ./myapp --no_cache true --service api
```

| Argument      | Type    | Required | Default | Description                   |
|---------------|---------|----------|---------|-------------------------------|
| `project_dir` | string  | yes      |         | Path to docker-compose.yml    |
| `no_cache`    | boolean | no       | false   | Build without cache           |
| `service`     | string  | no       |         | Specific service to build     |

Returns: build output per service.

## System

### System info

```
docker system_info
```

Returns: `server_version`, `os`, `architecture`, `cpus`, `memory`, `containers_running`, `containers_stopped`, `images`, `storage_driver`.

### System disk usage

```
docker system_df --verbose false
```

| Argument  | Type    | Required | Default | Description             |
|-----------|---------|----------|---------|-------------------------|
| `verbose` | boolean | no       | false   | Show detailed breakdown |

Returns: disk usage breakdown by `images`, `containers`, `volumes`, `build_cache` with `total_count`, `active`, `size`, `reclaimable`.

### System prune

```
docker system_prune --all true --volumes true --force true
```

| Argument  | Type    | Required | Default | Description                        |
|-----------|---------|----------|---------|------------------------------------|
| `all`     | boolean | no       | false   | Remove all unused images, not just dangling |
| `volumes` | boolean | no       | false   | Also prune volumes                 |
| `force`   | boolean | no       | false   | Skip confirmation                  |
| `filter`  | string  | no       |         | Filter (e.g. `until=24h`)         |

Returns: `space_reclaimed`, deleted items by category.

## Workflow

1. **List running containers** with `docker list_containers` to see what is deployed.
2. **Inspect a container** with `docker get_container` or `docker inspect` for full details.
3. **Deploy a new service** with `docker run` specifying image, ports, env, and volumes.
4. **Check logs** with `docker logs` to debug issues.
5. **Exec into a container** with `docker exec` to run diagnostic commands.
6. **Manage images** with `docker pull`, `docker build`, and `docker push` for CI/CD workflows.
7. **Use Compose** for multi-container applications: `compose_up`, `compose_ps`, `compose_logs`.
8. **Clean up** with `docker system_prune` to reclaim disk space.

## Safety notes

- `docker rm --force` and `docker system_prune` are destructive. Confirm with the user before executing.
- `docker exec` runs commands inside containers with the container's privileges. Be cautious with destructive commands.
- `docker push` publishes images to a registry. Ensure the image does not contain secrets or sensitive data.
- Port bindings (`--ports`) expose services to the host network. Verify no port conflicts before binding.
- Volume mounts (`--volumes`) can expose host filesystem paths. Never mount sensitive host directories without explicit approval.
- `compose_down --volumes` permanently deletes named volumes and their data.
