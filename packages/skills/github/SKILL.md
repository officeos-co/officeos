# GitHub

Read-only access to GitHub repositories, issues, and pull requests.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Commands

### List repositories

```
github list_repos --visibility all --per_page 10
```

| Argument     | Type   | Required | Default | Description                          |
|--------------|--------|----------|---------|--------------------------------------|
| `visibility` | string | no       | `all`   | `all`, `public`, or `private`        |
| `per_page`   | int    | no       | 30      | Results per page (1–100)             |

Returns array of repos: `full_name`, `private`, `description`, `html_url`, `default_branch`.

### List issues

```
github list_issues --owner HarKro753 --repo EnterpriseAgentOs --state open
```

| Argument | Type   | Required | Default | Description                      |
|----------|--------|----------|---------|----------------------------------|
| `owner`  | string | yes      |         | Repository owner (user or org)   |
| `repo`   | string | yes      |         | Repository name                  |
| `state`  | string | no       | `open`  | `open`, `closed`, or `all`       |

Returns array of issues: `number`, `title`, `state`, `author`, `html_url`.
Pull requests are excluded from the results.

### List pull requests

```
github list_prs --owner HarKro753 --repo EnterpriseAgentOs --state open
```

| Argument | Type   | Required | Default | Description                      |
|----------|--------|----------|---------|----------------------------------|
| `owner`  | string | yes      |         | Repository owner (user or org)   |
| `repo`   | string | yes      |         | Repository name                  |
| `state`  | string | no       | `open`  | `open`, `closed`, or `all`       |

Returns array of PRs: `number`, `title`, `state`, `author`, `html_url`, `draft`.

## Workflow

1. Start with `github list_repos` to discover available repositories.
2. Use `github list_issues` or `github list_prs` with the `owner` and `repo` from the results.
3. To get project status: list open issues + open PRs and summarize by labels, assignees, or age.
4. To check a specific repo, you can skip `list_repos` if you already know the owner and name.

## Safety notes

- This skill is **read-only**. You cannot create, update, or close issues or PRs.
- Results are paginated. Maximum 100 items per request. If you need more, make multiple calls.
- Only repositories accessible to the configured token are visible.
