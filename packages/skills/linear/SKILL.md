# Linear

Full Linear project management: manage issues, projects, cycles, teams, labels, roadmaps, and users via the Linear GraphQL API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Issues

### List issues

```
linear list_issues --team_id "TEAM-ID" --state "In Progress" --assignee "user-id" --label "bug" --priority 1 --project "project-id"
```

| Argument   | Type   | Required | Default | Description                                  |
|------------|--------|----------|---------|----------------------------------------------|
| `team_id`  | string | no       |         | Filter by team UUID                          |
| `state`    | string | no       |         | Filter by workflow state name                |
| `assignee` | string | no       |         | Filter by assignee user ID                   |
| `label`    | string | no       |         | Filter by label name                         |
| `priority` | int    | no       |         | Filter by priority (0=none, 1=urgent, 4=low) |
| `project`  | string | no       |         | Filter by project ID                         |
| `first`    | int    | no       | 50      | Number of results to return                  |

Returns: list of issues with `id`, `identifier`, `title`, `state`, `priority`, `assignee`, `labels`, `created_at`.

### Get issue

```
linear get_issue --issue_id "ISSUE-UUID"
```

| Argument   | Type   | Required | Description             |
|------------|--------|----------|-------------------------|
| `issue_id` | string | yes      | Issue UUID or identifier|

Returns: full issue details including `id`, `identifier`, `title`, `description`, `state`, `priority`, `assignee`, `labels`, `project`, `cycle`, `estimate`, `created_at`, `updated_at`.

### Create issue

```
linear create_issue --title "Fix login bug" --description "Users cannot log in with SSO" --team_id "TEAM-ID" --assignee_id "USER-ID" --priority 1 --state_id "STATE-ID" --label_ids '["LABEL-ID"]' --project_id "PROJECT-ID" --estimate 3
```

| Argument      | Type     | Required | Default | Description                                  |
|---------------|----------|----------|---------|----------------------------------------------|
| `title`       | string   | yes      |         | Issue title                                  |
| `description` | string   | no       |         | Issue description (markdown)                 |
| `team_id`     | string   | yes      |         | Team UUID to create issue in                 |
| `assignee_id` | string   | no       |         | User UUID to assign                          |
| `priority`    | int      | no       | 0       | Priority (0=none, 1=urgent, 2=high, 3=medium, 4=low) |
| `state_id`    | string   | no       |         | Workflow state UUID                          |
| `label_ids`   | string[] | no       |         | Label UUIDs to apply                         |
| `project_id`  | string   | no       |         | Project UUID to associate                    |
| `estimate`    | int      | no       |         | Estimate points                              |

Returns: `id`, `identifier`, `title`, `url`.

### Update issue

```
linear update_issue --issue_id "ISSUE-UUID" --title "Updated title" --priority 2 --state_id "STATE-ID"
```

| Argument      | Type     | Required | Description                      |
|---------------|----------|----------|----------------------------------|
| `issue_id`    | string   | yes      | Issue UUID to update             |
| `title`       | string   | no       | New title                        |
| `description` | string   | no       | New description                  |
| `assignee_id` | string   | no       | New assignee UUID                |
| `priority`    | int      | no       | New priority level               |
| `state_id`    | string   | no       | New workflow state UUID          |
| `label_ids`   | string[] | no       | Replace label UUIDs              |
| `project_id`  | string   | no       | New project UUID                 |
| `estimate`    | int      | no       | New estimate points              |

Returns: updated issue with `id`, `identifier`, `title`, `state`.

### Delete issue

```
linear delete_issue --issue_id "ISSUE-UUID"
```

| Argument   | Type   | Required | Description          |
|------------|--------|----------|----------------------|
| `issue_id` | string | yes      | Issue UUID to delete |

Returns: confirmation of deletion.

### Archive issue

```
linear archive_issue --issue_id "ISSUE-UUID"
```

| Argument   | Type   | Required | Description           |
|------------|--------|----------|-----------------------|
| `issue_id` | string | yes      | Issue UUID to archive |

Returns: confirmation with `id`, `identifier`, `archived_at`.

## Search

### Search issues

```
linear search_issues --query "login bug SSO"
```

| Argument | Type   | Required | Default | Description                    |
|----------|--------|----------|---------|--------------------------------|
| `query`  | string | yes      |         | Free-text search query         |
| `first`  | int    | no       | 20      | Number of results to return    |

Returns: list of matching issues with `id`, `identifier`, `title`, `state`, `priority`.

## Comments

### List comments

```
linear list_comments --issue_id "ISSUE-UUID"
```

| Argument   | Type   | Required | Description                   |
|------------|--------|----------|-------------------------------|
| `issue_id` | string | yes      | Issue UUID to list comments for|

Returns: list of comments with `id`, `body`, `user`, `created_at`.

### Create comment

```
linear create_comment --issue_id "ISSUE-UUID" --body "Investigating this now"
```

| Argument   | Type   | Required | Description                |
|------------|--------|----------|----------------------------|
| `issue_id` | string | yes      | Issue UUID to comment on   |
| `body`     | string | yes      | Comment body (markdown)    |

Returns: `id`, `body`, `created_at`.

### Update comment

```
linear update_comment --comment_id "COMMENT-UUID" --body "Updated analysis"
```

| Argument     | Type   | Required | Description              |
|--------------|--------|----------|--------------------------|
| `comment_id` | string | yes      | Comment UUID to update   |
| `body`       | string | yes      | New comment body         |

Returns: updated comment with `id`, `body`, `updated_at`.

### Delete comment

```
linear delete_comment --comment_id "COMMENT-UUID"
```

| Argument     | Type   | Required | Description              |
|--------------|--------|----------|--------------------------|
| `comment_id` | string | yes      | Comment UUID to delete   |

Returns: confirmation of deletion.

## Projects

### List projects

```
linear list_projects --first 20
```

| Argument | Type | Required | Default | Description                 |
|----------|------|----------|---------|-----------------------------|
| `first`  | int  | no       | 50      | Number of results to return |

Returns: list of projects with `id`, `name`, `description`, `state`, `progress`, `lead`, `start_date`, `target_date`.

### Get project

```
linear get_project --project_id "PROJECT-UUID"
```

| Argument     | Type   | Required | Description            |
|--------------|--------|----------|------------------------|
| `project_id` | string | yes      | Project UUID           |

Returns: full project details including `id`, `name`, `description`, `state`, `progress`, `lead`, `members`, `teams`, `issues_count`, `start_date`, `target_date`.

### Create project

```
linear create_project --name "Q2 Launch" --description "Product launch for Q2" --team_ids '["TEAM-ID"]' --lead_id "USER-ID" --start_date "2025-04-01" --target_date "2025-06-30"
```

| Argument      | Type     | Required | Description                     |
|---------------|----------|----------|---------------------------------|
| `name`        | string   | yes      | Project name                    |
| `description` | string   | no       | Project description             |
| `team_ids`    | string[] | yes      | Team UUIDs to associate         |
| `lead_id`     | string   | no       | Lead user UUID                  |
| `start_date`  | string   | no       | Start date (ISO format)         |
| `target_date` | string   | no       | Target completion date          |

Returns: `id`, `name`, `url`.

### Update project

```
linear update_project --project_id "PROJECT-UUID" --name "Q2 Launch v2" --state "started"
```

| Argument      | Type   | Required | Description                                     |
|---------------|--------|----------|-------------------------------------------------|
| `project_id`  | string | yes      | Project UUID to update                          |
| `name`        | string | no       | New project name                                |
| `description` | string | no       | New description                                 |
| `state`       | string | no       | State: `planned`, `started`, `paused`, `completed`, `canceled` |
| `lead_id`     | string | no       | New lead user UUID                              |
| `start_date`  | string | no       | New start date                                  |
| `target_date` | string | no       | New target date                                 |

Returns: updated project with `id`, `name`, `state`.

### Archive project

```
linear archive_project --project_id "PROJECT-UUID"
```

| Argument     | Type   | Required | Description              |
|--------------|--------|----------|--------------------------|
| `project_id` | string | yes      | Project UUID to archive  |

Returns: confirmation with `id`, `name`, `archived_at`.

## Teams

### List teams

```
linear list_teams
```

Returns: list of teams with `id`, `name`, `key`, `description`, `members_count`.

### Get team

```
linear get_team --team_id "TEAM-UUID"
```

| Argument  | Type   | Required | Description     |
|-----------|--------|----------|-----------------|
| `team_id` | string | yes      | Team UUID       |

Returns: full team details including `id`, `name`, `key`, `description`, `members`, `states`, `labels`, `cycles_enabled`.

## Cycles

### List cycles

```
linear list_cycles --team_id "TEAM-UUID" --first 10
```

| Argument  | Type   | Required | Default | Description                 |
|-----------|--------|----------|---------|-----------------------------|
| `team_id` | string | yes      |         | Team UUID                   |
| `first`   | int    | no       | 20      | Number of results to return |

Returns: list of cycles with `id`, `name`, `number`, `starts_at`, `ends_at`, `progress`, `scope`.

### Get cycle

```
linear get_cycle --cycle_id "CYCLE-UUID"
```

| Argument   | Type   | Required | Description      |
|------------|--------|----------|------------------|
| `cycle_id` | string | yes      | Cycle UUID       |

Returns: full cycle details including `id`, `name`, `number`, `starts_at`, `ends_at`, `progress`, `scope`, `completed_scope`, `issues`.

### Get active cycle

```
linear get_active_cycle --team_id "TEAM-UUID"
```

| Argument  | Type   | Required | Description        |
|-----------|--------|----------|--------------------|
| `team_id` | string | yes      | Team UUID          |

Returns: active cycle details or null if no cycle is active.

### Add issue to cycle

```
linear add_issue_to_cycle --issue_id "ISSUE-UUID" --cycle_id "CYCLE-UUID"
```

| Argument   | Type   | Required | Description          |
|------------|--------|----------|----------------------|
| `issue_id` | string | yes      | Issue UUID to add    |
| `cycle_id` | string | yes      | Target cycle UUID    |

Returns: confirmation with `issue_identifier`, `cycle_name`.

### Remove issue from cycle

```
linear remove_issue_from_cycle --issue_id "ISSUE-UUID"
```

| Argument   | Type   | Required | Description              |
|------------|--------|----------|--------------------------|
| `issue_id` | string | yes      | Issue UUID to remove     |

Returns: confirmation of removal.

## Labels

### List labels

```
linear list_labels --team_id "TEAM-UUID"
```

| Argument  | Type   | Required | Default | Description                        |
|-----------|--------|----------|---------|------------------------------------|
| `team_id` | string | no       |         | Filter by team (omit for workspace)|

Returns: list of labels with `id`, `name`, `color`, `description`.

### Create label

```
linear create_label --name "critical" --color "#FF0000" --team_id "TEAM-UUID" --description "Critical priority items"
```

| Argument      | Type   | Required | Default | Description                          |
|---------------|--------|----------|---------|--------------------------------------|
| `name`        | string | yes      |         | Label name                           |
| `color`       | string | no       |         | Hex color code                       |
| `team_id`     | string | no       |         | Team UUID (omit for workspace label) |
| `description` | string | no       |         | Label description                    |

Returns: `id`, `name`, `color`.

## States

### List workflow states

```
linear list_workflow_states --team_id "TEAM-UUID"
```

| Argument  | Type   | Required | Description           |
|-----------|--------|----------|-----------------------|
| `team_id` | string | yes      | Team UUID             |

Returns: list of workflow states with `id`, `name`, `type` (`triage`, `backlog`, `unstarted`, `started`, `completed`, `canceled`), `color`, `position`.

## Users

### List users

```
linear list_users --first 50
```

| Argument | Type | Required | Default | Description                 |
|----------|------|----------|---------|-----------------------------|
| `first`  | int  | no       | 50      | Number of results to return |

Returns: list of users with `id`, `name`, `email`, `display_name`, `active`.

### Get user

```
linear get_user --user_id "USER-UUID"
```

| Argument  | Type   | Required | Description      |
|-----------|--------|----------|------------------|
| `user_id` | string | yes      | User UUID        |

Returns: `id`, `name`, `email`, `display_name`, `active`, `admin`, `created_at`.

### Get current user

```
linear me
```

Returns: authenticated user details with `id`, `name`, `email`, `display_name`, `active`, `teams`.

## Attachments

### Create attachment

```
linear create_attachment --issue_id "ISSUE-UUID" --url "https://example.com/doc" --title "Design spec"
```

| Argument   | Type   | Required | Description                 |
|------------|--------|----------|-----------------------------|
| `issue_id` | string | yes      | Issue UUID to attach to     |
| `url`      | string | yes      | URL of the attachment       |
| `title`    | string | yes      | Display title               |

Returns: `id`, `url`, `title`, `created_at`.

## Roadmap

### List roadmaps

```
linear list_roadmaps --first 10
```

| Argument | Type | Required | Default | Description                 |
|----------|------|----------|---------|-----------------------------|
| `first`  | int  | no       | 20      | Number of results to return |

Returns: list of roadmaps with `id`, `name`, `description`, `slug`.

### Get roadmap

```
linear get_roadmap --roadmap_id "ROADMAP-UUID"
```

| Argument     | Type   | Required | Description         |
|--------------|--------|----------|---------------------|
| `roadmap_id` | string | yes      | Roadmap UUID        |

Returns: full roadmap details including `id`, `name`, `description`, `slug`, `projects`.

## Workflow

1. Start with `linear me` to confirm authenticated user and `linear list_teams` to discover teams.
2. Use `linear list_workflow_states --team_id` to understand available states before creating or updating issues.
3. Create issues with `linear create_issue` -- always provide `team_id`.
4. Use `linear search_issues` for free-text discovery across all issues.
5. Use `linear list_issues` with filters for structured queries (by state, assignee, label, priority).
6. Manage project progress: `linear list_projects` -> `linear get_project` -> update issues within the project.
7. Track cycles: `linear get_active_cycle` -> `linear add_issue_to_cycle` to assign sprint work.
8. Add context to issues with `linear create_comment` and `linear create_attachment`.

## Safety notes

- All IDs are UUIDs. **Never fabricate IDs** -- always discover them via list or search actions.
- `linear delete_issue` and `linear delete_comment` are permanent. Prefer `linear archive_issue` for issues.
- Linear API is rate-limited. Avoid rapid repeated queries in tight loops.
- Priority values: 0 = No priority, 1 = Urgent, 2 = High, 3 = Medium, 4 = Low. Lower number = higher priority.
- Only resources accessible to the configured API key are visible.
- Issue identifiers (e.g. `ENG-123`) can be used interchangeably with UUIDs in `get_issue`.
