# Jira

Full Jira Cloud project management: issues, projects, boards, sprints, transitions, comments, and JQL search via the Jira REST API v3.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Issues

### list_issues

Search for issues using JQL.

```
jira list_issues --jql "project = MYPROJ AND status != Done" --max_results 50
```

| Argument      | Type   | Required | Default | Description                          |
| ------------- | ------ | -------- | ------- | ------------------------------------ |
| `jql`         | string | yes      |         | JQL query string                     |
| `max_results` | number | no       | 50      | Maximum results (1-100)              |
| `start_at`    | number | no       | 0       | Offset for pagination                |
| `fields`      | string | no       |         | Comma-separated list of fields       |

Returns: `total`, `start_at`, `max_results`, `issues` (array of `{ id, key, summary, status, assignee, priority, created, updated }`).

### get_issue

Get a single issue by key or ID.

```
jira get_issue --issue_key "PROJ-123"
```

| Argument    | Type   | Required | Description    |
| ----------- | ------ | -------- | -------------- |
| `issue_key` | string | yes      | Issue key or ID |

Returns: full issue object including `id`, `key`, `summary`, `description`, `status`, `assignee`, `reporter`, `priority`, `labels`, `components`, `created`, `updated`, `due_date`, `subtasks`, `parent`, `transitions`.

### create_issue

Create a new issue.

```
jira create_issue --project_key "PROJ" --summary "Fix login bug" --issue_type "Bug" --priority "High" --description "Login fails on Safari"
```

| Argument      | Type         | Required | Default | Description                              |
| ------------- | ------------ | -------- | ------- | ---------------------------------------- |
| `project_key` | string       | yes      |         | Project key                              |
| `summary`     | string       | yes      |         | Issue summary                            |
| `issue_type`  | string       | no       | Task    | Issue type: Bug, Task, Story, Epic, etc. |
| `description` | string       | no       |         | Issue description (plain text)           |
| `priority`    | string       | no       |         | Priority: Highest, High, Medium, Low, Lowest |
| `assignee`    | string       | no       |         | Assignee account ID                      |
| `labels`      | string array | no       |         | Labels to attach                         |
| `due_date`    | string       | no       |         | Due date (YYYY-MM-DD)                    |
| `parent_key`  | string       | no       |         | Parent issue key (for sub-tasks)         |

Returns: `id`, `key`, `url`.

### update_issue

Update fields of an existing issue.

```
jira update_issue --issue_key "PROJ-123" --summary "Updated title" --priority "Medium"
```

| Argument      | Type         | Required | Description                       |
| ------------- | ------------ | -------- | --------------------------------- |
| `issue_key`   | string       | yes      | Issue key or ID                   |
| `summary`     | string       | no       | New summary                       |
| `description` | string       | no       | New description                   |
| `priority`    | string       | no       | New priority                      |
| `assignee`    | string       | no       | New assignee account ID           |
| `labels`      | string array | no       | Replace labels                    |
| `due_date`    | string       | no       | New due date (YYYY-MM-DD)         |

Returns: `success: true`.

### delete_issue

Delete an issue.

```
jira delete_issue --issue_key "PROJ-123"
```

| Argument    | Type   | Required | Description    |
| ----------- | ------ | -------- | -------------- |
| `issue_key` | string | yes      | Issue key or ID |

Returns: `success: true`.

### transition_issue

Move an issue to a new status using a transition.

```
jira transition_issue --issue_key "PROJ-123" --transition_id "31"
```

| Argument        | Type   | Required | Description                          |
| --------------- | ------ | -------- | ------------------------------------ |
| `issue_key`     | string | yes      | Issue key or ID                      |
| `transition_id` | string | yes      | Transition ID (from get_transitions) |
| `comment`       | string | no       | Comment to add when transitioning    |

Returns: `success: true`.

### get_transitions

List available transitions for an issue.

```
jira get_transitions --issue_key "PROJ-123"
```

| Argument    | Type   | Required | Description    |
| ----------- | ------ | -------- | -------------- |
| `issue_key` | string | yes      | Issue key or ID |

Returns: array of `{ id, name, to_status }`.

### assign_issue

Assign or unassign an issue.

```
jira assign_issue --issue_key "PROJ-123" --account_id "5f8a3b2c1d..."
```

| Argument     | Type   | Required | Description                              |
| ------------ | ------ | -------- | ---------------------------------------- |
| `issue_key`  | string | yes      | Issue key or ID                          |
| `account_id` | string | no       | Account ID to assign (null to unassign)  |

Returns: `success: true`.

## Comments

### list_comments

List comments on an issue.

```
jira list_comments --issue_key "PROJ-123"
```

| Argument    | Type   | Required | Default | Description           |
| ----------- | ------ | -------- | ------- | --------------------- |
| `issue_key` | string | yes      |         | Issue key or ID       |
| `max_results` | number | no     | 50      | Max comments to return |

Returns: array of `{ id, author, body, created, updated }`.

### add_comment

Add a comment to an issue.

```
jira add_comment --issue_key "PROJ-123" --body "Fixed in PR #456"
```

| Argument    | Type   | Required | Description          |
| ----------- | ------ | -------- | -------------------- |
| `issue_key` | string | yes      | Issue key or ID      |
| `body`      | string | yes      | Comment text         |

Returns: `id`, `author`, `body`, `created`.

### update_comment

Update an existing comment.

```
jira update_comment --issue_key "PROJ-123" --comment_id "10001" --body "Updated comment text"
```

| Argument     | Type   | Required | Description     |
| ------------ | ------ | -------- | --------------- |
| `issue_key`  | string | yes      | Issue key or ID |
| `comment_id` | string | yes      | Comment ID      |
| `body`       | string | yes      | New comment text |

Returns: `id`, `body`, `updated`.

### delete_comment

Delete a comment.

```
jira delete_comment --issue_key "PROJ-123" --comment_id "10001"
```

| Argument     | Type   | Required | Description     |
| ------------ | ------ | -------- | --------------- |
| `issue_key`  | string | yes      | Issue key or ID |
| `comment_id` | string | yes      | Comment ID      |

Returns: `success: true`.

## Projects

### list_projects

List all projects accessible to the user.

```
jira list_projects --max_results 50
```

| Argument      | Type   | Required | Default | Description           |
| ------------- | ------ | -------- | ------- | --------------------- |
| `max_results` | number | no       | 50      | Max projects to return |
| `query`       | string | no       |         | Filter by name/key     |

Returns: array of `{ id, key, name, type, style, lead }`.

### get_project

Get details of a project.

```
jira get_project --project_key "PROJ"
```

| Argument      | Type   | Required | Description  |
| ------------- | ------ | -------- | ------------ |
| `project_key` | string | yes      | Project key  |

Returns: `id`, `key`, `name`, `description`, `lead`, `type`, `style`, `components`, `versions`.

## Boards & Sprints

### list_boards

List all Scrum/Kanban boards.

```
jira list_boards --project_key "PROJ" --type scrum
```

| Argument      | Type   | Required | Description                          |
| ------------- | ------ | -------- | ------------------------------------ |
| `project_key` | string | no       | Filter by project key                |
| `type`        | string | no       | Board type: `scrum` or `kanban`      |
| `max_results` | number | no       | Max boards to return                 |

Returns: array of `{ id, name, type, project_key }`.

### list_sprints

List sprints for a board.

```
jira list_sprints --board_id 42 --state active
```

| Argument   | Type   | Required | Default | Description                             |
| ---------- | ------ | -------- | ------- | --------------------------------------- |
| `board_id` | number | yes      |         | Board ID                                |
| `state`    | string | no       |         | Sprint state: `active`, `future`, `closed` |

Returns: array of `{ id, name, state, start_date, end_date, goal }`.

### get_sprint

Get details of a sprint.

```
jira get_sprint --sprint_id 15
```

| Argument    | Type   | Required | Description |
| ----------- | ------ | -------- | ----------- |
| `sprint_id` | number | yes      | Sprint ID   |

Returns: `id`, `name`, `state`, `start_date`, `end_date`, `goal`, `board_id`.

## Search

### search_issues

Full JQL search with pagination.

```
jira search_issues --jql "assignee = currentUser() AND sprint in openSprints()" --max_results 20
```

| Argument      | Type   | Required | Default | Description               |
| ------------- | ------ | -------- | ------- | ------------------------- |
| `jql`         | string | yes      |         | JQL query                 |
| `max_results` | number | no       | 50      | Max results (1-100)       |
| `start_at`    | number | no       | 0       | Pagination offset         |
| `fields`      | string | no       |         | Comma-separated fields    |

Returns: `total`, `start_at`, `max_results`, `issues`.
