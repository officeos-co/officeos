# Asana

Full Asana project management: workspaces, projects, tasks, sections, comments, tags, and search via the Asana REST API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Workspaces

### list_workspaces

List all workspaces the authenticated user belongs to.

```
asana list_workspaces
```

Returns: array of `{ gid, name }`.

## Projects

### list_projects

List projects in a workspace.

```
asana list_projects --workspace_gid "12345" --team_gid "67890"
```

| Argument        | Type   | Required | Description               |
| --------------- | ------ | -------- | ------------------------- |
| `workspace_gid` | string | yes      | Workspace GID             |
| `team_gid`      | string | no       | Filter by team GID        |
| `archived`      | boolean | no      | Include archived projects |

Returns: array of `{ gid, name, color, archived, created_at, modified_at }`.

### get_project

Get details of a project.

```
asana get_project --project_gid "12345"
```

| Argument      | Type   | Required | Description  |
| ------------- | ------ | -------- | ------------ |
| `project_gid` | string | yes      | Project GID  |

Returns: `gid`, `name`, `notes`, `color`, `archived`, `workspace`, `team`, `created_at`, `modified_at`, `due_date`, `owner`.

### create_project

Create a new project.

```
asana create_project --workspace_gid "12345" --name "Q3 Launch" --color "light-blue"
```

| Argument        | Type   | Required | Description                      |
| --------------- | ------ | -------- | -------------------------------- |
| `workspace_gid` | string | yes      | Workspace GID                    |
| `name`          | string | yes      | Project name                     |
| `team_gid`      | string | no       | Team GID                         |
| `notes`         | string | no       | Project description               |
| `color`         | string | no       | Project color                    |
| `due_date`      | string | no       | Due date (YYYY-MM-DD)            |

Returns: `gid`, `name`, `color`.

### update_project

Update a project.

```
asana update_project --project_gid "12345" --name "New Name" --archived true
```

| Argument      | Type    | Required | Description         |
| ------------- | ------- | -------- | ------------------- |
| `project_gid` | string  | yes      | Project GID         |
| `name`        | string  | no       | New name            |
| `notes`       | string  | no       | New description     |
| `archived`    | boolean | no       | Archive/unarchive   |
| `color`       | string  | no       | New color           |

Returns: `gid`, `name`.

## Sections

### list_sections

List sections in a project.

```
asana list_sections --project_gid "12345"
```

| Argument      | Type   | Required | Description |
| ------------- | ------ | -------- | ----------- |
| `project_gid` | string | yes      | Project GID |

Returns: array of `{ gid, name, created_at }`.

### create_section

Create a section in a project.

```
asana create_section --project_gid "12345" --name "In Review"
```

| Argument      | Type   | Required | Description  |
| ------------- | ------ | -------- | ------------ |
| `project_gid` | string | yes      | Project GID  |
| `name`        | string | yes      | Section name |

Returns: `gid`, `name`.

## Tasks

### list_tasks

List tasks in a project or section.

```
asana list_tasks --project_gid "12345" --completed false
```

| Argument        | Type    | Required | Description                       |
| --------------- | ------- | -------- | --------------------------------- |
| `project_gid`   | string  | no       | Project GID                       |
| `section_gid`   | string  | no       | Section GID                       |
| `assignee`      | string  | no       | Filter by assignee GID or `me`    |
| `completed`     | boolean | no       | Filter by completion status       |
| `modified_since` | string | no      | ISO 8601 — tasks modified after   |

Returns: array of `{ gid, name, completed, assignee, due_on, created_at, modified_at }`.

### get_task

Get details of a task.

```
asana get_task --task_gid "12345"
```

| Argument   | Type   | Required | Description |
| ---------- | ------ | -------- | ----------- |
| `task_gid` | string | yes      | Task GID    |

Returns: `gid`, `name`, `notes`, `completed`, `assignee`, `due_on`, `projects`, `tags`, `subtasks`, `created_at`, `modified_at`.

### create_task

Create a new task.

```
asana create_task --project_gid "12345" --name "Write tests" --assignee "me" --due_on "2025-06-30"
```

| Argument      | Type         | Required | Description                         |
| ------------- | ------------ | -------- | ----------------------------------- |
| `project_gid` | string       | yes      | Project GID                         |
| `name`        | string       | yes      | Task name                           |
| `notes`       | string       | no       | Task description                    |
| `assignee`    | string       | no       | Assignee GID or `me`                |
| `due_on`      | string       | no       | Due date (YYYY-MM-DD)               |
| `section_gid` | string       | no       | Section GID to add task to          |
| `tags`        | string array | no       | Tag GIDs to attach                  |
| `parent_gid`  | string       | no       | Parent task GID (for subtasks)      |

Returns: `gid`, `name`, `created_at`.

### update_task

Update a task.

```
asana update_task --task_gid "12345" --completed true --assignee "me"
```

| Argument    | Type    | Required | Description              |
| ----------- | ------- | -------- | ------------------------ |
| `task_gid`  | string  | yes      | Task GID                 |
| `name`      | string  | no       | New name                 |
| `notes`     | string  | no       | New description          |
| `completed` | boolean | no       | Mark complete/incomplete |
| `assignee`  | string  | no       | New assignee GID         |
| `due_on`    | string  | no       | New due date             |

Returns: `gid`, `name`, `completed`.

### delete_task

Delete a task.

```
asana delete_task --task_gid "12345"
```

| Argument   | Type   | Required | Description |
| ---------- | ------ | -------- | ----------- |
| `task_gid` | string | yes      | Task GID    |

Returns: `success: true`.

## Comments (Stories)

### list_comments

List comments (stories) on a task.

```
asana list_comments --task_gid "12345"
```

| Argument   | Type   | Required | Description |
| ---------- | ------ | -------- | ----------- |
| `task_gid` | string | yes      | Task GID    |

Returns: array of `{ gid, type, text, created_at, created_by }`.

### add_comment

Add a comment to a task.

```
asana add_comment --task_gid "12345" --text "Looks good to me!"
```

| Argument   | Type   | Required | Description  |
| ---------- | ------ | -------- | ------------ |
| `task_gid` | string | yes      | Task GID     |
| `text`     | string | yes      | Comment text |

Returns: `gid`, `text`, `created_at`.

## Tags

### list_tags

List all tags in a workspace.

```
asana list_tags --workspace_gid "12345"
```

| Argument        | Type   | Required | Description   |
| --------------- | ------ | -------- | ------------- |
| `workspace_gid` | string | yes      | Workspace GID |

Returns: array of `{ gid, name, color }`.

## Search

### search_tasks

Search for tasks in a workspace using text.

```
asana search_tasks --workspace_gid "12345" --text "login bug" --completed false
```

| Argument        | Type    | Required | Description                          |
| --------------- | ------- | -------- | ------------------------------------ |
| `workspace_gid` | string  | yes      | Workspace GID                        |
| `text`          | string  | yes      | Full-text search query               |
| `completed`     | boolean | no       | Filter by completion status          |
| `assignee`      | string  | no       | Filter by assignee GID               |
| `project_gid`   | string  | no       | Filter by project GID                |

Returns: array of `{ gid, name, completed, assignee, due_on }`.
