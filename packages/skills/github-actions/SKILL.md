# GitHub Actions

Manage GitHub Actions workflows, runs, jobs, artifacts, secrets, and variables via the GitHub REST API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Workflows

### List workflows

```
github-actions list_workflows --owner myorg --repo myrepo
```

| Argument | Type   | Required | Description       |
|----------|--------|----------|-------------------|
| `owner`  | string | yes      | Repository owner  |
| `repo`   | string | yes      | Repository name   |

### List workflow runs

```
github-actions list_runs --owner myorg --repo myrepo --workflow_id deploy.yml --status completed
```

| Argument      | Type   | Required | Default | Description                                     |
|---------------|--------|----------|---------|-------------------------------------------------|
| `owner`       | string | yes      |         | Repository owner                                |
| `repo`        | string | yes      |         | Repository name                                 |
| `workflow_id` | string | no       |         | Workflow filename or ID to filter               |
| `status`      | string | no       |         | `queued`, `in_progress`, `completed`, `waiting` |
| `branch`      | string | no       |         | Filter by branch                                |
| `per_page`    | int    | no       | 20      | Results per page (1-100)                        |

### Get a workflow run

```
github-actions get_run --owner myorg --repo myrepo --run_id 12345678
```

| Argument | Type   | Required | Description    |
|----------|--------|----------|----------------|
| `owner`  | string | yes      | Repo owner     |
| `repo`   | string | yes      | Repo name      |
| `run_id` | int    | yes      | Workflow run ID |

### Re-run a workflow run

```
github-actions rerun --owner myorg --repo myrepo --run_id 12345678
```

| Argument | Type   | Required | Description     |
|----------|--------|----------|-----------------|
| `owner`  | string | yes      | Repo owner      |
| `repo`   | string | yes      | Repo name       |
| `run_id` | int    | yes      | Workflow run ID |

### Cancel a workflow run

```
github-actions cancel --owner myorg --repo myrepo --run_id 12345678
```

| Argument | Type   | Required | Description     |
|----------|--------|----------|-----------------|
| `owner`  | string | yes      | Repo owner      |
| `repo`   | string | yes      | Repo name       |
| `run_id` | int    | yes      | Workflow run ID |

### Trigger a workflow (workflow_dispatch)

```
github-actions trigger_workflow --owner myorg --repo myrepo --workflow_id deploy.yml --ref main
```

| Argument      | Type   | Required | Description                          |
|---------------|--------|----------|--------------------------------------|
| `owner`       | string | yes      | Repo owner                           |
| `repo`        | string | yes      | Repo name                            |
| `workflow_id` | string | yes      | Workflow filename or numeric ID      |
| `ref`         | string | yes      | Branch, tag, or SHA to run on        |
| `inputs`      | string | no       | JSON-encoded object of input values  |

## Jobs

### List jobs for a run

```
github-actions list_jobs --owner myorg --repo myrepo --run_id 12345678
```

| Argument   | Type   | Required | Default | Description               |
|------------|--------|----------|---------|---------------------------|
| `owner`    | string | yes      |         | Repo owner                |
| `repo`     | string | yes      |         | Repo name                 |
| `run_id`   | int    | yes      |         | Workflow run ID           |
| `filter`   | string | no       | `all`   | `latest` or `all`         |

### Get job logs (URL)

```
github-actions get_logs --owner myorg --repo myrepo --job_id 987654321
```

| Argument | Type   | Required | Description  |
|----------|--------|----------|--------------|
| `owner`  | string | yes      | Repo owner   |
| `repo`   | string | yes      | Repo name    |
| `job_id` | int    | yes      | Job ID       |

## Artifacts

### List artifacts for a run

```
github-actions list_artifacts --owner myorg --repo myrepo --run_id 12345678
```

| Argument | Type   | Required | Description     |
|----------|--------|----------|-----------------|
| `owner`  | string | yes      | Repo owner      |
| `repo`   | string | yes      | Repo name       |
| `run_id` | int    | yes      | Workflow run ID |

## Secrets

### List repository secrets

```
github-actions list_secrets --owner myorg --repo myrepo
```

| Argument | Type   | Required | Description |
|----------|--------|----------|-------------|
| `owner`  | string | yes      | Repo owner  |
| `repo`   | string | yes      | Repo name   |

### Set a repository secret

```
github-actions set_secret --owner myorg --repo myrepo --secret_name MY_TOKEN --secret_value abc123
```

| Argument       | Type   | Required | Description       |
|----------------|--------|----------|-------------------|
| `owner`        | string | yes      | Repo owner        |
| `repo`         | string | yes      | Repo name         |
| `secret_name`  | string | yes      | Secret name       |
| `secret_value` | string | yes      | Plaintext value   |

Note: Values are encrypted with the repo's public key before being sent.

### Delete a repository secret

```
github-actions delete_secret --owner myorg --repo myrepo --secret_name MY_TOKEN
```

| Argument      | Type   | Required | Description   |
|---------------|--------|----------|---------------|
| `owner`       | string | yes      | Repo owner    |
| `repo`        | string | yes      | Repo name     |
| `secret_name` | string | yes      | Secret name   |

## Variables

### List repository variables

```
github-actions list_variables --owner myorg --repo myrepo
```

| Argument | Type   | Required | Description |
|----------|--------|----------|-------------|
| `owner`  | string | yes      | Repo owner  |
| `repo`   | string | yes      | Repo name   |

### Set a repository variable

```
github-actions set_variable --owner myorg --repo myrepo --name DEPLOY_ENV --value production
```

| Argument | Type   | Required | Description     |
|----------|--------|----------|-----------------|
| `owner`  | string | yes      | Repo owner      |
| `repo`   | string | yes      | Repo name       |
| `name`   | string | yes      | Variable name   |
| `value`  | string | yes      | Variable value  |
