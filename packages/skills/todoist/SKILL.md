# Todoist

Full Todoist REST API v2 coverage: manage tasks, projects, sections, comments, labels, and collaborators.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Task operations

### List tasks

```
todoist list_tasks --project_id 123456 --filter "today" --label "urgent" --priority 4
```

| Argument     | Type   | Required | Default | Description                                      |
|--------------|--------|----------|---------|--------------------------------------------------|
| `project_id` | string | no       |         | Filter by project ID                             |
| `filter`     | string | no       |         | Todoist filter string (e.g. "today", "overdue")  |
| `label`      | string | no       |         | Filter by label name                             |
| `priority`   | int    | no       |         | Filter by priority (1=normal, 4=urgent)          |

### Get task

```
todoist get_task --task_id 123456789
```

| Argument  | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `task_id` | string | yes      | Task ID     |

Returns: `id`, `content`, `description`, `project_id`, `section_id`, `parent_id`, `priority`, `due`, `labels`, `assignee_id`, `is_completed`, `url`, `created_at`.

### Create task

```
todoist create_task --content "Buy groceries" --description "Milk, eggs, bread" --project_id 123 --priority 3 --due_string "tomorrow at 3pm" --labels '["shopping"]'
```

| Argument      | Type     | Required | Default | Description                              |
|---------------|----------|----------|---------|------------------------------------------|
| `content`     | string   | yes      |         | Task content (title)                     |
| `description` | string   | no       |         | Task description (markdown)              |
| `project_id`  | string   | no       |         | Project to add the task to               |
| `section_id`  | string   | no       |         | Section to add the task to               |
| `parent_id`   | string   | no       |         | Parent task ID for sub-tasks             |
| `priority`    | int      | no       | 1       | Priority (1=normal, 2, 3, 4=urgent)     |
| `due_string`  | string   | no       |         | Natural language due date                |
| `due_date`    | string   | no       |         | Due date in YYYY-MM-DD format            |
| `labels`      | string[] | no       |         | Labels to apply                          |
| `assignee_id` | string   | no       |         | User ID to assign the task to            |

### Update task

```
todoist update_task --task_id 123456789 --content "Updated title" --priority 4
```

| Argument      | Type     | Required | Description                              |
|---------------|----------|----------|------------------------------------------|
| `task_id`     | string   | yes      | Task ID                                  |
| `content`     | string   | no       | New task content                         |
| `description` | string   | no       | New description                          |
| `priority`    | int      | no       | New priority (1-4)                       |
| `due_string`  | string   | no       | New natural language due date            |
| `due_date`    | string   | no       | New due date in YYYY-MM-DD format        |
| `labels`      | string[] | no       | Replace labels                           |
| `assignee_id` | string   | no       | New assignee user ID                     |

### Close task

```
todoist close_task --task_id 123456789
```

Marks a task as completed.

### Reopen task

```
todoist reopen_task --task_id 123456789
```

Reopens a previously completed task.

### Delete task

```
todoist delete_task --task_id 123456789
```

Permanently deletes a task.

## Project operations

### List projects

```
todoist list_projects
```

Returns all projects.

### Get project

```
todoist get_project --project_id 123456
```

| Argument     | Type   | Required | Description |
|--------------|--------|----------|-------------|
| `project_id` | string | yes      | Project ID  |

Returns: `id`, `name`, `color`, `parent_id`, `order`, `is_favorite`, `is_inbox_project`, `url`.

### Create project

```
todoist create_project --name "Work" --color "blue" --parent_id 123 --is_favorite true
```

| Argument      | Type    | Required | Default | Description                    |
|---------------|---------|----------|---------|--------------------------------|
| `name`        | string  | yes      |         | Project name                   |
| `color`       | string  | no       |         | Project color                  |
| `parent_id`   | string  | no       |         | Parent project ID              |
| `is_favorite` | boolean | no       | false   | Add to favorites               |

### Update project

```
todoist update_project --project_id 123456 --name "Updated" --color "red"
```

| Argument      | Type    | Required | Description                    |
|---------------|---------|----------|--------------------------------|
| `project_id`  | string  | yes      | Project ID                     |
| `name`        | string  | no       | New project name               |
| `color`       | string  | no       | New project color              |
| `is_favorite` | boolean | no       | Update favorite status         |

### Delete project

```
todoist delete_project --project_id 123456
```

Permanently deletes a project and all its tasks.

## Section operations

### List sections

```
todoist list_sections --project_id 123456
```

| Argument     | Type   | Required | Description                  |
|--------------|--------|----------|------------------------------|
| `project_id` | string | no       | Filter by project ID         |

### Get section

```
todoist get_section --section_id 123456
```

Returns: `id`, `project_id`, `order`, `name`.

### Create section

```
todoist create_section --name "In Progress" --project_id 123456
```

| Argument     | Type   | Required | Description                  |
|--------------|--------|----------|------------------------------|
| `name`       | string | yes      | Section name                 |
| `project_id` | string | yes      | Project to add section to    |

### Update section

```
todoist update_section --section_id 123456 --name "Done"
```

| Argument     | Type   | Required | Description                  |
|--------------|--------|----------|------------------------------|
| `section_id` | string | yes      | Section ID                   |
| `name`       | string | yes      | New section name             |

### Delete section

```
todoist delete_section --section_id 123456
```

Permanently deletes a section.

## Comment operations

### List comments

```
todoist list_comments --task_id 123456789
todoist list_comments --project_id 123456
```

| Argument     | Type   | Required | Description                                  |
|--------------|--------|----------|----------------------------------------------|
| `task_id`    | string | no       | Task ID (mutually exclusive with project_id) |
| `project_id` | string | no       | Project ID (mutually exclusive with task_id) |

One of `task_id` or `project_id` is required.

### Get comment

```
todoist get_comment --comment_id 123456
```

Returns: `id`, `content`, `posted_at`, `task_id`, `project_id`, `attachment`.

### Create comment

```
todoist create_comment --task_id 123456789 --content "Looking good!"
```

| Argument     | Type   | Required | Description                                  |
|--------------|--------|----------|----------------------------------------------|
| `content`    | string | yes      | Comment content (markdown)                   |
| `task_id`    | string | no       | Task ID (mutually exclusive with project_id) |
| `project_id` | string | no       | Project ID (mutually exclusive with task_id) |
| `attachment` | object | no       | File attachment object                       |

One of `task_id` or `project_id` is required.

### Update comment

```
todoist update_comment --comment_id 123456 --content "Updated comment"
```

| Argument     | Type   | Required | Description          |
|--------------|--------|----------|----------------------|
| `comment_id` | string | yes      | Comment ID           |
| `content`    | string | yes      | New comment content  |

### Delete comment

```
todoist delete_comment --comment_id 123456
```

Permanently deletes a comment.

## Label operations

### List labels

```
todoist list_labels
```

Returns all personal labels.

### Create label

```
todoist create_label --name "urgent" --color "red"
```

| Argument | Type   | Required | Description    |
|----------|--------|----------|----------------|
| `name`   | string | yes      | Label name     |
| `color`  | string | no       | Label color    |
| `order`  | int    | no       | Display order  |

### Update label

```
todoist update_label --label_id 123456 --name "critical" --color "orange"
```

| Argument   | Type   | Required | Description    |
|------------|--------|----------|----------------|
| `label_id` | string | yes      | Label ID       |
| `name`     | string | no       | New label name |
| `color`    | string | no       | New color      |
| `order`    | int    | no       | New order      |

### Delete label

```
todoist delete_label --label_id 123456
```

Permanently deletes a label.

## Collaborator operations

### List collaborators

```
todoist list_collaborators --project_id 123456
```

| Argument     | Type   | Required | Description |
|--------------|--------|----------|-------------|
| `project_id` | string | yes      | Project ID  |

Returns: `id`, `name`, `email`.

## Workflow

1. Start with `todoist list_projects` to discover existing projects.
2. Use `todoist list_tasks` with filters to find relevant tasks.
3. Create, update, close, and reopen tasks as needed.
4. Organize with sections and labels.
5. Add comments to tasks for collaboration.
6. Use `todoist list_collaborators` to see project members.

## Safety notes

- Write operations (create, update, close, delete) require a valid API token.
- `delete_task`, `delete_project`, `delete_section`, `delete_comment`, and `delete_label` are permanent and cannot be undone.
- Priority values: 1 (normal), 2 (high), 3 (very high), 4 (urgent). Note this is the API priority, which is the inverse of the UI display.
- The `filter` parameter on `list_tasks` supports Todoist's filter syntax (e.g. "today", "overdue", "p1", "#Work").
