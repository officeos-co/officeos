# GitHub Skill

List repositories, issues, and pull requests via the GitHub REST API.

## Actions

- **list_repos** — List repositories accessible to the authenticated user.
- **list_issues** — List open issues in a single repository.
- **list_prs** — List pull requests in a single repository.

## Credentials

- `token` — Personal Access Token. Fine-grained PAT recommended with read access to repos/issues/PRs. Create one at https://github.com/settings/tokens.

## Limitations

- Read-only. Cannot create issues, PRs, or push code.
- Pagination limited to 100 items per request.
- Token scope determines accessible repositories.
