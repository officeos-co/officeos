# GitHub

This skill gives you read access to GitHub repositories, issues, and pull requests.

## Commands

- `github list_repos --visibility all --per_page 10` — List repositories.
- `github list_issues --owner HarKro753 --repo EnterpriseAgentOs --state open` — List issues in a repo.
- `github list_prs --owner HarKro753 --repo EnterpriseAgentOs --state open` — List pull requests.

## Workflow

1. Start with `github list_repos` to discover available repositories.
2. Use `github list_issues` or `github list_prs` with the `owner` and `repo` from the results.
3. To get project status: list open issues + open PRs and summarize by labels, assignees, or age.

## Limitations

- This skill is **read-only**. You cannot create, update, or close issues or PRs.
- Results are paginated. Maximum 100 items per request.
- Only repositories the configured token has access to are visible.
