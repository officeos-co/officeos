# Vercel

Manage Vercel projects, deployments, domains, environment variables, logs, teams, checks, aliases, and secrets via the Vercel REST API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Project operations

### List projects

```
vercel list_projects --team_id team_abc123 --limit 20
```

| Argument  | Type   | Required | Default | Description                  |
|-----------|--------|----------|---------|------------------------------|
| `team_id` | string | no       |         | Team ID to scope the request |
| `limit`   | int    | no       | 20      | Results per page (1-100)     |

### Get project

```
vercel get_project --project_id prj_abc123
```

| Argument     | Type   | Required | Description                    |
|--------------|--------|----------|--------------------------------|
| `project_id` | string | yes      | Project ID or name             |

Returns: `id`, `name`, `framework`, `nodeVersion`, `link`, `createdAt`, `updatedAt`.

### Create project

```
vercel create_project --name my-app --framework nextjs --git_repository '{"type":"github","repo":"user/repo"}'
```

| Argument         | Type   | Required | Description                                  |
|------------------|--------|----------|----------------------------------------------|
| `name`           | string | yes      | Project name                                 |
| `framework`      | string | no       | Framework preset (nextjs, vite, etc.)         |
| `git_repository` | object | no       | Git repository to connect (`type` and `repo`) |

### Update project

```
vercel update_project --project_id prj_abc123 --name new-name --framework vite
```

| Argument     | Type   | Required | Description                     |
|--------------|--------|----------|---------------------------------|
| `project_id` | string | yes      | Project ID or name              |
| `name`       | string | no       | New project name                |
| `framework`  | string | no       | New framework preset            |

### Delete project

```
vercel delete_project --project_id prj_abc123
```

| Argument     | Type   | Required | Description        |
|--------------|--------|----------|--------------------|
| `project_id` | string | yes      | Project ID or name |

## Deployments

### List deployments

```
vercel list_deployments --project_id prj_abc123 --state READY --limit 10
```

| Argument     | Type   | Required | Default | Description                              |
|--------------|--------|----------|---------|------------------------------------------|
| `project_id` | string | no       |         | Filter by project ID                     |
| `state`      | string | no       |         | `BUILDING`, `READY`, `ERROR`, `QUEUED`, `CANCELED` |
| `limit`      | int    | no       | 20      | Results per page (1-100)                 |
| `team_id`    | string | no       |         | Team ID to scope the request             |

### Get deployment

```
vercel get_deployment --deployment_id dpl_abc123
```

| Argument        | Type   | Required | Description   |
|-----------------|--------|----------|---------------|
| `deployment_id` | string | yes      | Deployment ID |

Returns: `id`, `name`, `url`, `state`, `readyState`, `createdAt`, `buildingAt`, `ready`, `meta`.

### Create deployment

```
vercel create_deployment --name my-app --target production --project_id prj_abc123
```

| Argument     | Type   | Required | Description                       |
|--------------|--------|----------|-----------------------------------|
| `name`       | string | yes      | Deployment name                   |
| `project_id` | string | no       | Project to deploy                 |
| `target`     | string | no       | `production` or `preview`         |
| `git_source` | object | no       | Git source (`type`, `ref`, `repoId`) |

### Cancel deployment

```
vercel cancel_deployment --deployment_id dpl_abc123
```

| Argument        | Type   | Required | Description   |
|-----------------|--------|----------|---------------|
| `deployment_id` | string | yes      | Deployment ID |

### Redeploy

```
vercel redeploy --deployment_id dpl_abc123 --target production
```

| Argument        | Type   | Required | Description                |
|-----------------|--------|----------|----------------------------|
| `deployment_id` | string | yes      | Deployment ID to redeploy  |
| `target`        | string | no       | `production` or `preview`  |

## Domains

### List domains

```
vercel list_domains --project_id prj_abc123
```

| Argument     | Type   | Required | Description        |
|--------------|--------|----------|--------------------|
| `project_id` | string | yes      | Project ID or name |

### Add domain

```
vercel add_domain --project_id prj_abc123 --domain example.com
```

| Argument     | Type   | Required | Description        |
|--------------|--------|----------|--------------------|
| `project_id` | string | yes      | Project ID or name |
| `domain`     | string | yes      | Domain name to add |

### Remove domain

```
vercel remove_domain --project_id prj_abc123 --domain example.com
```

| Argument     | Type   | Required | Description           |
|--------------|--------|----------|-----------------------|
| `project_id` | string | yes      | Project ID or name    |
| `domain`     | string | yes      | Domain name to remove |

### Get domain config

```
vercel get_domain_config --domain example.com
```

| Argument | Type   | Required | Description |
|----------|--------|----------|-------------|
| `domain` | string | yes      | Domain name |

Returns: `configuredBy`, `acceptedChallenges`, `misconfigured`.

### Verify domain

```
vercel verify_domain --project_id prj_abc123 --domain example.com
```

| Argument     | Type   | Required | Description        |
|--------------|--------|----------|--------------------|
| `project_id` | string | yes      | Project ID or name |
| `domain`     | string | yes      | Domain to verify   |

## Environment Variables

### List environment variables

```
vercel list_env_vars --project_id prj_abc123
```

| Argument     | Type   | Required | Description        |
|--------------|--------|----------|--------------------|
| `project_id` | string | yes      | Project ID or name |

### Get environment variable

```
vercel get_env_var --project_id prj_abc123 --env_id env_abc123
```

| Argument     | Type   | Required | Description        |
|--------------|--------|----------|--------------------|
| `project_id` | string | yes      | Project ID or name |
| `env_id`     | string | yes      | Environment var ID |

### Create environment variable

```
vercel create_env_var --project_id prj_abc123 --key DATABASE_URL --value "postgres://..." --target '["production","preview"]' --type encrypted
```

| Argument     | Type     | Required | Default     | Description                                  |
|--------------|----------|----------|-------------|----------------------------------------------|
| `project_id` | string   | yes      |             | Project ID or name                           |
| `key`        | string   | yes      |             | Variable name                                |
| `value`      | string   | yes      |             | Variable value                               |
| `target`     | string[] | no       | `["production","preview","development"]` | Target environments |
| `type`       | string   | no       | `encrypted` | `plain`, `encrypted`, `secret`, `sensitive`  |

### Update environment variable

```
vercel update_env_var --project_id prj_abc123 --env_id env_abc123 --value "new-value"
```

| Argument     | Type     | Required | Description                    |
|--------------|----------|----------|--------------------------------|
| `project_id` | string   | yes      | Project ID or name             |
| `env_id`     | string   | yes      | Environment variable ID        |
| `value`      | string   | no       | New value                      |
| `target`     | string[] | no       | New target environments        |
| `type`       | string   | no       | New type                       |

### Delete environment variable

```
vercel delete_env_var --project_id prj_abc123 --env_id env_abc123
```

| Argument     | Type   | Required | Description         |
|--------------|--------|----------|---------------------|
| `project_id` | string | yes      | Project ID or name  |
| `env_id`     | string | yes      | Environment var ID  |

## Logs

### Get deployment logs

```
vercel get_deployment_logs --deployment_id dpl_abc123
```

| Argument        | Type   | Required | Description   |
|-----------------|--------|----------|---------------|
| `deployment_id` | string | yes      | Deployment ID |

Returns array of log entries with `timestamp`, `type`, `text`.

### Get build logs

```
vercel get_build_logs --deployment_id dpl_abc123
```

| Argument        | Type   | Required | Description   |
|-----------------|--------|----------|---------------|
| `deployment_id` | string | yes      | Deployment ID |

Returns array of build output entries.

## Teams

### List teams

```
vercel list_teams --limit 20
```

| Argument | Type | Required | Default | Description              |
|----------|------|----------|---------|--------------------------|
| `limit`  | int  | no       | 20      | Results per page (1-100) |

### Get team

```
vercel get_team --team_id team_abc123
```

| Argument  | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `team_id` | string | yes      | Team ID     |

Returns: `id`, `name`, `slug`, `createdAt`, `updatedAt`.

## Checks

### List checks

```
vercel list_checks --deployment_id dpl_abc123
```

| Argument        | Type   | Required | Description   |
|-----------------|--------|----------|---------------|
| `deployment_id` | string | yes      | Deployment ID |

### Get check

```
vercel get_check --deployment_id dpl_abc123 --check_id chk_abc123
```

| Argument        | Type   | Required | Description   |
|-----------------|--------|----------|---------------|
| `deployment_id` | string | yes      | Deployment ID |
| `check_id`      | string | yes      | Check ID      |

Returns: `id`, `name`, `status`, `conclusion`, `detailsUrl`, `completedAt`.

## Aliases

### List aliases

```
vercel list_aliases --project_id prj_abc123 --limit 20
```

| Argument     | Type   | Required | Default | Description              |
|--------------|--------|----------|---------|--------------------------|
| `project_id` | string | no       |         | Filter by project        |
| `limit`      | int    | no       | 20      | Results per page (1-100) |

### Set alias

```
vercel set_alias --deployment_id dpl_abc123 --alias my-app.vercel.app
```

| Argument        | Type   | Required | Description            |
|-----------------|--------|----------|------------------------|
| `deployment_id` | string | yes      | Deployment ID          |
| `alias`         | string | yes      | Alias domain to assign |

### Remove alias

```
vercel remove_alias --alias_id al_abc123
```

| Argument   | Type   | Required | Description |
|------------|--------|----------|-------------|
| `alias_id` | string | yes      | Alias ID    |

## Secrets

### List secrets

```
vercel list_secrets --limit 20
```

| Argument | Type | Required | Default | Description              |
|----------|------|----------|---------|--------------------------|
| `limit`  | int  | no       | 20      | Results per page (1-100) |

### Create secret

```
vercel create_secret --name my-secret --value "secret-value"
```

| Argument | Type   | Required | Description  |
|----------|--------|----------|--------------|
| `name`   | string | yes      | Secret name  |
| `value`  | string | yes      | Secret value |

### Delete secret

```
vercel delete_secret --name my-secret
```

| Argument | Type   | Required | Description              |
|----------|--------|----------|--------------------------|
| `name`   | string | yes      | Secret name or ID        |

## Workflow

1. Start with `vercel list_projects` to discover projects.
2. Use `vercel get_project` for detailed info about a specific project.
3. Manage deployments: create, list, check status, cancel, or redeploy.
4. Configure domains: add, verify, check DNS config, remove.
5. Manage environment variables per project and target environment.
6. Monitor builds: check deployment logs, build logs, and deployment checks.
7. Manage aliases to point custom domains to deployments.
8. Use teams to scope operations to a specific team.

## Safety notes

- Write operations (create, update, delete) require appropriate token scopes.
- Results are paginated. Use `limit` to control page size.
- Only resources accessible to the configured token are visible.
- Deleting a project is irreversible and removes all deployments.
- Environment variable values of type `secret` cannot be read back after creation.
- The `team_id` credential is optional; when set, all requests are scoped to that team.
