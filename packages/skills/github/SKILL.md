# GitHub

Full GitHub CLI parity: manage repositories, issues, pull requests, releases, workflows, gists, search, and organizations via the GitHub REST API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Repository operations

### List repositories

```
github list_repos --visibility all --per_page 10
```

| Argument     | Type   | Required | Default | Description                          |
|--------------|--------|----------|---------|--------------------------------------|
| `visibility` | string | no       | `all`   | `all`, `public`, or `private`        |
| `per_page`   | int    | no       | 30      | Results per page (1-100)             |

### Get repository details

```
github get_repo --owner HarKro753 --repo EnterpriseAgentOs
```

| Argument | Type   | Required | Description          |
|----------|--------|----------|----------------------|
| `owner`  | string | yes      | Repository owner     |
| `repo`   | string | yes      | Repository name      |

Returns: `full_name`, `private`, `description`, `html_url`, `default_branch`, `language`, `stargazers_count`, `forks_count`, `open_issues_count`, `topics`, `created_at`, `updated_at`.

### Create repository

```
github create_repo --name my-project --description "A new project" --private true
```

| Argument      | Type    | Required | Default | Description               |
|---------------|---------|----------|---------|---------------------------|
| `name`        | string  | yes      |         | Repository name           |
| `description` | string  | no       |         | Repository description    |
| `private`     | boolean | no       | false   | Whether repo is private   |
| `auto_init`   | boolean | no       | true    | Initialize with README    |

### Get clone URL

```
github clone_repo --owner HarKro753 --repo EnterpriseAgentOs
```

Returns `clone_url`, `ssh_url`, `html_url`.

### List / Set repository topics

```
github list_repo_topics --owner HarKro753 --repo EnterpriseAgentOs
github set_repo_topics --owner HarKro753 --repo EnterpriseAgentOs --topics '["ai","agents"]'
```

## Issues

### List issues

```
github list_issues --owner HarKro753 --repo EnterpriseAgentOs --state open --per_page 20
```

| Argument   | Type   | Required | Default | Description              |
|------------|--------|----------|---------|--------------------------|
| `owner`    | string | yes      |         | Repository owner         |
| `repo`     | string | yes      |         | Repository name          |
| `state`    | string | no       | `open`  | `open`, `closed`, `all`  |
| `per_page` | int    | no       | 30      | Results per page (1-100) |

Pull requests are excluded from the results.

### Get issue

```
github get_issue --owner HarKro753 --repo EnterpriseAgentOs --issue_number 42
```

Returns full issue details including `body`, `labels`, `assignees`, `comments_count`.

### Create issue

```
github create_issue --owner HarKro753 --repo EnterpriseAgentOs --title "Bug report" --body "Details here" --labels '["bug"]' --assignees '["HarKro753"]'
```

| Argument    | Type     | Required | Description                |
|-------------|----------|----------|----------------------------|
| `owner`     | string   | yes      | Repository owner           |
| `repo`      | string   | yes      | Repository name            |
| `title`     | string   | yes      | Issue title                |
| `body`      | string   | no       | Issue body (markdown)      |
| `labels`    | string[] | no       | Labels to apply            |
| `assignees` | string[] | no       | Usernames to assign        |

### Edit issue

```
github edit_issue --owner HarKro753 --repo EnterpriseAgentOs --issue_number 42 --title "Updated title" --labels '["bug","critical"]'
```

### Close / Reopen issue

```
github close_issue --owner HarKro753 --repo EnterpriseAgentOs --issue_number 42
github reopen_issue --owner HarKro753 --repo EnterpriseAgentOs --issue_number 42
```

### List issue comments

```
github list_issue_comments --owner HarKro753 --repo EnterpriseAgentOs --issue_number 42
```

### Add issue comment

```
github add_issue_comment --owner HarKro753 --repo EnterpriseAgentOs --issue_number 42 --body "Looks good"
```

## Pull Requests

### List pull requests

```
github list_prs --owner HarKro753 --repo EnterpriseAgentOs --state open
```

### Get PR details

```
github get_pr --owner HarKro753 --repo EnterpriseAgentOs --pr_number 10
```

Returns: `number`, `title`, `state`, `author`, `body`, `draft`, `merged`, `mergeable`, `head_ref`, `base_ref`, `additions`, `deletions`, `changed_files`.

### Create PR

```
github create_pr --owner HarKro753 --repo EnterpriseAgentOs --title "Add feature" --head feature-branch --base main --draft false
```

| Argument | Type    | Required | Default | Description               |
|----------|---------|----------|---------|---------------------------|
| `owner`  | string  | yes      |         | Repository owner          |
| `repo`   | string  | yes      |         | Repository name           |
| `title`  | string  | yes      |         | PR title                  |
| `body`   | string  | no       |         | PR body (markdown)        |
| `head`   | string  | yes      |         | Branch with changes       |
| `base`   | string  | yes      |         | Branch to merge into      |
| `draft`  | boolean | no       | false   | Create as draft           |

### Merge PR

```
github merge_pr --owner HarKro753 --repo EnterpriseAgentOs --pr_number 10 --merge_method squash
```

| Argument         | Type   | Required | Default | Description                       |
|------------------|--------|----------|---------|-----------------------------------|
| `pr_number`      | int    | yes      |         | PR number                         |
| `merge_method`   | string | no       | `merge` | `merge`, `squash`, or `rebase`    |
| `commit_title`   | string | no       |         | Custom merge commit title         |
| `commit_message` | string | no       |         | Custom merge commit message       |

### Close / Reopen PR

```
github close_pr --owner HarKro753 --repo EnterpriseAgentOs --pr_number 10
github reopen_pr --owner HarKro753 --repo EnterpriseAgentOs --pr_number 10
```

### PR comments

```
github list_pr_comments --owner HarKro753 --repo EnterpriseAgentOs --pr_number 10
github add_pr_comment --owner HarKro753 --repo EnterpriseAgentOs --pr_number 10 --body "LGTM"
```

### PR reviews

```
github list_pr_reviews --owner HarKro753 --repo EnterpriseAgentOs --pr_number 10
github request_pr_review --owner HarKro753 --repo EnterpriseAgentOs --pr_number 10 --reviewers '["teammate"]'
```

### List PR changed files

```
github list_pr_files --owner HarKro753 --repo EnterpriseAgentOs --pr_number 10
```

Returns: `filename`, `status`, `additions`, `deletions`, `changes`, `patch`.

## Releases

### List releases

```
github list_releases --owner HarKro753 --repo EnterpriseAgentOs --per_page 5
```

### Get release

```
github get_release --owner HarKro753 --repo EnterpriseAgentOs --release_id 123
```

### Create release

```
github create_release --owner HarKro753 --repo EnterpriseAgentOs --tag_name v1.0.0 --name "v1.0.0" --body "Release notes"
```

| Argument           | Type    | Required | Default | Description                        |
|--------------------|---------|----------|---------|------------------------------------|
| `tag_name`         | string  | yes      |         | Tag name (e.g. v1.0.0)            |
| `name`             | string  | no       |         | Release title                      |
| `body`             | string  | no       |         | Release notes (markdown)           |
| `draft`            | boolean | no       | false   | Create as draft                    |
| `prerelease`       | boolean | no       | false   | Mark as prerelease                 |
| `target_commitish` | string  | no       |         | Branch or commit for the tag       |

## Workflows / Actions

### List workflows

```
github list_workflows --owner HarKro753 --repo EnterpriseAgentOs
```

### List workflow runs

```
github list_workflow_runs --owner HarKro753 --repo EnterpriseAgentOs --workflow_id ci.yml --status completed
```

### Get workflow run

```
github get_workflow_run --owner HarKro753 --repo EnterpriseAgentOs --run_id 12345
```

### Trigger workflow

```
github trigger_workflow --owner HarKro753 --repo EnterpriseAgentOs --workflow_id deploy.yml --ref main --inputs '{"environment":"production"}'
```

## Gists

### List gists

```
github list_gists --per_page 10
```

### Get gist

```
github get_gist --gist_id abc123
```

### Create gist

```
github create_gist --description "My snippet" --public false --files '{"hello.js":"console.log(\"hello\")"}'
```

## Search

### Search repositories

```
github search_repos --query "language:rust stars:>100" --sort stars --per_page 5
```

### Search issues

```
github search_issues --query "repo:HarKro753/EnterpriseAgentOs is:issue label:bug" --sort updated
```

### Search code

```
github search_code --query "defineSkill language:typescript" --per_page 5
```

## Organizations

### List org repositories

```
github list_org_repos --org my-org --type all --per_page 20
```

### List org members

```
github list_org_members --org my-org
```

## Workflow

1. Start with `github list_repos` or `github search_repos` to discover repositories.
2. Use `github get_repo` for detailed info about a specific repo.
3. Manage issues: create, edit, close, comment, and search.
4. Manage PRs: create, review, merge, and track changed files.
5. Monitor CI/CD: list workflows, check run status, trigger deployments.
6. Create releases to tag and publish versions.
7. Use gists for quick code sharing.
8. Use search actions to find repos, issues, and code across GitHub.

## Safety notes

- Write operations (create, edit, close, merge) require appropriate token scopes.
- Results are paginated. Maximum 100 items per request.
- Only repositories accessible to the configured token are visible.
- Search API has a rate limit of 30 requests per minute for authenticated users.
- The `trigger_workflow` action only works on workflows that have `workflow_dispatch` configured.
