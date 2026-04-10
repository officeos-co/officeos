# GitHub

This skill gives you read access to GitHub repositories, issues, and pull requests.

## Tools

### `github.list_repos`

List repositories accessible to the authenticated user.

**Parameters:**
- `visibility` (string, optional): `"all"` | `"public"` | `"private"`. Default `"all"`.
- `per_page` (integer, optional): 1–100. Default 30.

**Example call:**
```json
{ "visibility": "all", "per_page": 10 }
```

**Returns:** Array of repos with `name`, `full_name`, `description`, `private`, `html_url`, `language`, `updated_at`.

---

### `github.list_issues`

List issues in a specific repository.

**Parameters (required marked with *):**
- `owner`* (string): Repository owner, e.g. `"HarKro753"`.
- `repo`* (string): Repository name, e.g. `"EnterpriseAgentOs"`.
- `state` (string, optional): `"open"` | `"closed"` | `"all"`. Default `"open"`.

**Example call:**
```json
{ "owner": "HarKro753", "repo": "EnterpriseAgentOs", "state": "open" }
```

**Returns:** Array of issues with `number`, `title`, `state`, `labels`, `assignee`, `created_at`, `html_url`.

---

### `github.list_prs`

List pull requests in a specific repository.

**Parameters (required marked with *):**
- `owner`* (string): Repository owner.
- `repo`* (string): Repository name.
- `state` (string, optional): `"open"` | `"closed"` | `"all"`. Default `"open"`.

**Example call:**
```json
{ "owner": "HarKro753", "repo": "EnterpriseAgentOs", "state": "open" }
```

**Returns:** Array of PRs with `number`, `title`, `state`, `head.ref`, `base.ref`, `user.login`, `created_at`, `html_url`.

## Workflow

1. Start with `github.list_repos` to discover available repositories.
2. Use `github.list_issues` or `github.list_prs` with the `owner` and `repo` from the results.
3. To get project status: list open issues + open PRs and summarize by labels, assignees, or age.

## Limitations

- This skill is **read-only**. You cannot create, update, or close issues or PRs.
- Results are paginated. If you need more than `per_page` allows, make multiple calls.
- Only repositories the configured token has access to are visible.
