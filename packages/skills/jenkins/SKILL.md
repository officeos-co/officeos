# Jenkins

Manage Jenkins jobs, builds, queues, nodes, and views via the Jenkins REST API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Jobs

### List jobs

```
jenkins list_jobs
```

Returns all top-level jobs. Nested jobs (inside folders) can be retrieved by specifying a `folder_path`.

| Argument      | Type   | Required | Description                            |
|---------------|--------|----------|----------------------------------------|
| `folder_path` | string | no       | Folder path, e.g. `MyFolder/SubFolder` |

### Get job

```
jenkins get_job --name my-pipeline
```

| Argument | Type   | Required | Description                                           |
|----------|--------|----------|-------------------------------------------------------|
| `name`   | string | yes      | Job name (URL-encoded if it contains special chars)   |

Returns: `name`, `url`, `color`, `description`, `buildable`, `last_build`, `health_report`.

### Build a job

```
jenkins build_job --name my-pipeline
jenkins build_job --name my-pipeline --params '{"BRANCH":"main","DEPLOY":"true"}'
```

| Argument | Type   | Required | Description                                    |
|----------|--------|----------|------------------------------------------------|
| `name`   | string | yes      | Job name                                       |
| `params` | string | no       | JSON object of build parameters                |

### Get a specific build

```
jenkins get_build --name my-pipeline --build_number 42
```

| Argument       | Type   | Required | Description       |
|----------------|--------|----------|-------------------|
| `name`         | string | yes      | Job name          |
| `build_number` | int    | yes      | Build number      |

Returns: `number`, `result`, `duration`, `timestamp`, `url`, `building`.

### Get build log

```
jenkins get_log --name my-pipeline --build_number 42
jenkins get_log --name my-pipeline --build_number 42 --start 0
```

| Argument       | Type   | Required | Default | Description                          |
|----------------|--------|----------|---------|--------------------------------------|
| `name`         | string | yes      |         | Job name                             |
| `build_number` | int    | yes      |         | Build number                         |
| `start`        | int    | no       | 0       | Byte offset for progressive log read |

### Stop a build

```
jenkins stop_build --name my-pipeline --build_number 42
```

| Argument       | Type   | Required | Description   |
|----------------|--------|----------|---------------|
| `name`         | string | yes      | Job name      |
| `build_number` | int    | yes      | Build number  |

### List builds for a job

```
jenkins list_builds --name my-pipeline --limit 10
```

| Argument | Type   | Required | Default | Description              |
|----------|--------|----------|---------|--------------------------|
| `name`   | string | yes      |         | Job name                 |
| `limit`  | int    | no       | 10      | Number of builds to list |

## Queue

### Get build queue

```
jenkins get_queue
```

Returns pending queue items with cause and why (reason still waiting).

## Nodes

### List nodes (agents)

```
jenkins list_nodes
```

Returns all Jenkins agents/nodes with their online status and number of executors.

## Views

### List views

```
jenkins list_views
```

Returns all configured views (dashboards) in the Jenkins instance.

## Pipelines

### Get pipeline stages

```
jenkins get_stages --name my-pipeline --build_number 42
```

| Argument       | Type   | Required | Description  |
|----------------|--------|----------|--------------|
| `name`         | string | yes      | Pipeline job name |
| `build_number` | int    | yes      | Build number |

Returns stage names, status, and duration from the Pipeline Steps API.
