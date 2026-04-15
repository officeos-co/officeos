# Azure DevOps

Manage Azure DevOps projects, repositories, pull requests, work items, builds, and pipelines via the Azure DevOps REST API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Projects

### List projects

```
azure-devops list_projects
```

| Argument    | Type | Required | Default | Description          |
|-------------|------|----------|---------|----------------------|
| `top`       | int  | no       | 100     | Max results          |
| `skip`      | int  | no       | 0       | Results to skip      |

### Get project

```
azure-devops get_project --project MyProject
```

| Argument  | Type   | Required | Description         |
|-----------|--------|----------|---------------------|
| `project` | string | yes      | Project name or ID  |

## Repositories

### List repositories

```
azure-devops list_repos --project MyProject
```

| Argument  | Type   | Required | Description  |
|-----------|--------|----------|--------------|
| `project` | string | yes      | Project name |

### Get repository

```
azure-devops get_repo --project MyProject --repo my-service
```

| Argument  | Type   | Required | Description        |
|-----------|--------|----------|--------------------|
| `project` | string | yes      | Project name       |
| `repo`    | string | yes      | Repository name/ID |

## Pull Requests

### List pull requests

```
azure-devops list_prs --project MyProject --repo my-service --status active
```

| Argument  | Type   | Required | Default    | Description                             |
|-----------|--------|----------|------------|-----------------------------------------|
| `project` | string | yes      |            | Project name                            |
| `repo`    | string | yes      |            | Repository name                         |
| `status`  | string | no       | `active`   | `active`, `abandoned`, `completed`, `all` |
| `top`     | int    | no       | 25         | Max results                             |

### Create pull request

```
azure-devops create_pr --project MyProject --repo my-service --source_branch feature/my-feature --target_branch main --title "My PR"
```

| Argument         | Type   | Required | Description           |
|------------------|--------|----------|-----------------------|
| `project`        | string | yes      | Project name          |
| `repo`           | string | yes      | Repository name       |
| `source_branch`  | string | yes      | Source branch (refs/heads/… or short name) |
| `target_branch`  | string | yes      | Target branch         |
| `title`          | string | yes      | PR title              |
| `description`    | string | no       | PR description        |
| `auto_complete`  | boolean | no      | false   | Set to auto-complete when policies pass |

### Get pull request

```
azure-devops get_pr --project MyProject --repo my-service --pr_id 42
```

| Argument  | Type   | Required | Description     |
|-----------|--------|----------|-----------------|
| `project` | string | yes      | Project name    |
| `repo`    | string | yes      | Repository name |
| `pr_id`   | int    | yes      | Pull request ID |

## Work Items

### List work items (by query)

```
azure-devops list_work_items --project MyProject --wiql "SELECT [System.Id],[System.Title] FROM WorkItems WHERE [System.WorkItemType]='Bug' AND [State]='Active'"
```

| Argument  | Type   | Required | Description                         |
|-----------|--------|----------|-------------------------------------|
| `project` | string | yes      | Project name                        |
| `wiql`    | string | yes      | WIQL query string                   |
| `top`     | int    | no       | 100 | Max results                     |

### Create work item

```
azure-devops create_work_item --project MyProject --type Bug --title "Login fails on mobile" --description "Steps to reproduce..."
```

| Argument      | Type   | Required | Description                                         |
|---------------|--------|----------|-----------------------------------------------------|
| `project`     | string | yes      | Project name                                        |
| `type`        | string | yes      | Work item type, e.g. `Bug`, `Task`, `User Story`    |
| `title`       | string | yes      | Work item title                                     |
| `description` | string | no       | Description / repro steps                          |
| `assigned_to` | string | no       | Assignee display name or email                      |
| `area_path`   | string | no       | Area path override                                  |

### Get work item

```
azure-devops get_work_item --id 1234
```

| Argument | Type | Required | Description     |
|----------|------|----------|-----------------|
| `id`     | int  | yes      | Work item ID    |

## Builds

### List builds

```
azure-devops list_builds --project MyProject --definition_id 5 --top 20
```

| Argument        | Type   | Required | Default | Description                     |
|-----------------|--------|----------|---------|---------------------------------|
| `project`       | string | yes      |         | Project name                    |
| `definition_id` | int    | no       |         | Filter by build definition ID   |
| `branch`        | string | no       |         | Filter by branch name           |
| `result`        | string | no       |         | `succeeded`, `failed`, `canceled`, `partiallySucceeded` |
| `top`           | int    | no       | 25      | Max results                     |

### Queue a build

```
azure-devops queue_build --project MyProject --definition_id 5 --branch main
```

| Argument        | Type   | Required | Description              |
|-----------------|--------|----------|--------------------------|
| `project`       | string | yes      | Project name             |
| `definition_id` | int    | yes      | Build definition ID      |
| `branch`        | string | no       | Branch override (refs/heads/…) |
| `parameters`    | string | no       | JSON string of parameters |

## Pipelines

### List pipelines

```
azure-devops list_pipelines --project MyProject
```

| Argument  | Type   | Required | Default | Description      |
|-----------|--------|----------|---------|------------------|
| `project` | string | yes      |         | Project name     |
| `top`     | int    | no       | 100     | Max results      |

### Run a pipeline

```
azure-devops run_pipeline --project MyProject --pipeline_id 12 --branch main
```

| Argument      | Type   | Required | Description               |
|---------------|--------|----------|---------------------------|
| `project`     | string | yes      | Project name              |
| `pipeline_id` | int    | yes      | Pipeline ID               |
| `branch`      | string | no       | Branch to run on          |
| `variables`   | string | no       | JSON string of variable overrides |
