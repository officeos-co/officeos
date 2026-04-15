# GitLab

Manage GitLab projects, merge requests, issues, pipelines, branches, tags, and search via the GitLab REST API v4. Works with GitLab.com and self-hosted instances.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Projects

### List projects

```
gitlab list_projects --membership true --per_page 20
```

| Argument      | Type    | Required | Default | Description                             |
|---------------|---------|----------|---------|-----------------------------------------|
| `membership`  | boolean | no       | true    | Only projects the user is a member of   |
| `search`      | string  | no       |         | Filter by name                          |
| `per_page`    | int     | no       | 20      | Results per page (1-100)                |

### Get project

```
gitlab get_project --id 12345
```

| Argument | Type         | Required | Description                              |
|----------|--------------|----------|------------------------------------------|
| `id`     | string/int   | yes      | Project ID or URL-encoded path           |

Returns: `id`, `name`, `path_with_namespace`, `description`, `web_url`, `default_branch`, `visibility`, `created_at`.

## Merge Requests

### List merge requests

```
gitlab list_merge_requests --project_id 12345 --state opened
```

| Argument     | Type   | Required | Default  | Description                    |
|--------------|--------|----------|----------|--------------------------------|
| `project_id` | string | yes     |          | Project ID or path             |
| `state`      | string | no       | `opened` | `opened`, `closed`, `merged`, `all` |
| `per_page`   | int    | no       | 20       | Results per page               |

### Get merge request

```
gitlab get_mr --project_id 12345 --mr_iid 1
```

| Argument     | Type   | Required | Description          |
|--------------|--------|----------|----------------------|
| `project_id` | string | yes      | Project ID or path   |
| `mr_iid`     | int    | yes      | MR internal ID       |

### Create merge request

```
gitlab create_mr --project_id 12345 --source_branch feature --target_branch main --title "My MR"
```

| Argument        | Type   | Required | Default | Description              |
|-----------------|--------|----------|---------|--------------------------|
| `project_id`    | string | yes      |         | Project ID or path       |
| `source_branch` | string | yes      |         | Source branch name       |
| `target_branch` | string | yes      |         | Target branch name       |
| `title`         | string | yes      |         | MR title                 |
| `description`   | string | no       |         | MR description           |

### Merge a merge request

```
gitlab merge_mr --project_id 12345 --mr_iid 1
```

| Argument              | Type    | Required | Default | Description                      |
|-----------------------|---------|----------|---------|----------------------------------|
| `project_id`          | string  | yes      |         | Project ID or path               |
| `mr_iid`              | int     | yes      |         | MR internal ID                   |
| `squash`              | boolean | no       | false   | Squash commits on merge          |
| `should_remove_source_branch` | boolean | no | false | Remove source branch after merge |

## Issues

### List issues

```
gitlab list_issues --project_id 12345 --state opened
```

| Argument     | Type   | Required | Default  | Description                    |
|--------------|--------|----------|----------|--------------------------------|
| `project_id` | string | yes     |          | Project ID or path             |
| `state`      | string | no       | `opened` | `opened`, `closed`, `all`      |
| `labels`     | string | no       |          | Comma-separated label names    |
| `per_page`   | int    | no       | 20       | Results per page               |

### Get issue

```
gitlab get_issue --project_id 12345 --issue_iid 10
```

| Argument     | Type   | Required | Description          |
|--------------|--------|----------|----------------------|
| `project_id` | string | yes      | Project ID or path   |
| `issue_iid`  | int    | yes      | Issue internal ID    |

### Create issue

```
gitlab create_issue --project_id 12345 --title "Bug report" --labels "bug,urgent"
```

| Argument     | Type   | Required | Description              |
|--------------|--------|----------|--------------------------|
| `project_id` | string | yes      | Project ID or path       |
| `title`      | string | yes      | Issue title              |
| `description`| string | no       | Issue body               |
| `labels`     | string | no       | Comma-separated labels   |
| `assignee_ids`| string| no       | Comma-separated user IDs |

## Pipelines

### List pipelines

```
gitlab list_pipelines --project_id 12345 --status success
```

| Argument     | Type   | Required | Default | Description                       |
|--------------|--------|----------|---------|-----------------------------------|
| `project_id` | string | yes      |         | Project ID or path                |
| `status`     | string | no       |         | `running`, `pending`, `success`, `failed`, `canceled` |
| `ref`        | string | no       |         | Branch or tag name                |
| `per_page`   | int    | no       | 20      | Results per page                  |

### Get pipeline

```
gitlab get_pipeline --project_id 12345 --pipeline_id 678
```

| Argument      | Type   | Required | Description       |
|---------------|--------|----------|-------------------|
| `project_id`  | string | yes      | Project ID        |
| `pipeline_id` | int    | yes      | Pipeline ID       |

### Retry pipeline

```
gitlab retry_pipeline --project_id 12345 --pipeline_id 678
```

| Argument      | Type   | Required | Description       |
|---------------|--------|----------|-------------------|
| `project_id`  | string | yes      | Project ID        |
| `pipeline_id` | int    | yes      | Pipeline ID       |

## Branches

### List branches

```
gitlab list_branches --project_id 12345 --search feature
```

| Argument     | Type   | Required | Default | Description          |
|--------------|--------|----------|---------|----------------------|
| `project_id` | string | yes      |         | Project ID or path   |
| `search`     | string | no       |         | Filter by name       |
| `per_page`   | int    | no       | 20      | Results per page     |

### Create branch

```
gitlab create_branch --project_id 12345 --branch feature-x --ref main
```

| Argument     | Type   | Required | Description            |
|--------------|--------|----------|------------------------|
| `project_id` | string | yes      | Project ID or path     |
| `branch`     | string | yes      | New branch name        |
| `ref`        | string | yes      | Source branch or SHA   |

## Tags

### List tags

```
gitlab list_tags --project_id 12345
```

| Argument     | Type   | Required | Default | Description          |
|--------------|--------|----------|---------|----------------------|
| `project_id` | string | yes      |         | Project ID or path   |
| `search`     | string | no       |         | Filter by name       |
| `per_page`   | int    | no       | 20      | Results per page     |

## Search

### Search across projects

```
gitlab search --scope projects --search "my-project"
```

| Argument | Type   | Required | Description                                    |
|----------|--------|----------|------------------------------------------------|
| `scope`  | string | yes      | `projects`, `issues`, `merge_requests`, `blobs` |
| `search` | string | yes      | Search query                                   |
| `per_page`| int   | no       | Results per page                               |
