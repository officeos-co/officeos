# Git

Full Git CLI parity: manage repositories, branches, commits, staging, remotes, stashes, tags, and conflict resolution.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Status

### Show working tree status

```
git status
```

Returns: list of staged, unstaged, and untracked files with their statuses.

### Show unstaged diff

```
git diff --file path/to/file
```

| Argument | Type   | Required | Default | Description                       |
|----------|--------|----------|---------|-----------------------------------|
| `file`   | string | no       |         | Limit diff to a specific file     |

Returns: unified diff of unstaged changes.

### Show staged diff

```
git diff_staged --file path/to/file
```

| Argument | Type   | Required | Default | Description                       |
|----------|--------|----------|---------|-----------------------------------|
| `file`   | string | no       |         | Limit diff to a specific file     |

Returns: unified diff of staged (index) changes.

## Commits

### View commit log

```
git log --max_count 10 --author "Harro" --since "2025-01-01" --until "2025-12-31" --grep "fix" --oneline true
```

| Argument    | Type    | Required | Default | Description                              |
|-------------|---------|----------|---------|------------------------------------------|
| `max_count` | int     | no       | 20      | Maximum number of commits to return      |
| `author`    | string  | no       |         | Filter by author name or email           |
| `since`     | string  | no       |         | Show commits after date (ISO or relative)|
| `until`     | string  | no       |         | Show commits before date                 |
| `grep`      | string  | no       |         | Filter commits by message pattern        |
| `oneline`   | boolean | no       | false   | Compact single-line output               |

Returns: list of commits with `hash`, `author`, `date`, `message`.

### Show commit details

```
git show --commit_hash abc1234
```

| Argument      | Type   | Required | Default | Description          |
|---------------|--------|----------|---------|----------------------|
| `commit_hash` | string | yes      |         | Commit SHA to inspect|

Returns: commit metadata, message, and full diff.

### Create a commit

```
git commit --message "feat: add skill registry" --all true
```

| Argument  | Type    | Required | Default | Description                             |
|-----------|---------|----------|---------|-----------------------------------------|
| `message` | string  | yes      |         | Commit message                          |
| `all`     | boolean | no       | false   | Automatically stage modified files (-a) |
| `amend`   | boolean | no       | false   | Amend the previous commit               |

Returns: `hash`, `message`, `files_changed`, `insertions`, `deletions`.

## Branches

### List branches

```
git list_branches --remote true
```

| Argument | Type    | Required | Default | Description                  |
|----------|---------|----------|---------|------------------------------|
| `remote` | boolean | no       | false   | Include remote branches (-r) |
| `all`    | boolean | no       | false   | Show local and remote (-a)   |

Returns: list of branch names with current branch indicated.

### Create a branch

```
git create_branch --name feature/new-skill --start_point main
```

| Argument      | Type   | Required | Default       | Description                        |
|---------------|--------|----------|---------------|------------------------------------|
| `name`        | string | yes      |               | New branch name                    |
| `start_point` | string | no       | current HEAD  | Commit or branch to start from     |

Returns: confirmation with branch name.

### Delete a branch

```
git delete_branch --name feature/old --force true
```

| Argument | Type    | Required | Default | Description                      |
|----------|---------|----------|---------|----------------------------------|
| `name`   | string  | yes      |         | Branch to delete                 |
| `force`  | boolean | no       | false   | Force delete unmerged branch (-D)|

Returns: confirmation of deletion.

### Switch branch

```
git switch_branch --name feature/new-skill --create true
```

| Argument | Type    | Required | Default | Description                        |
|----------|---------|----------|---------|------------------------------------|
| `name`   | string  | yes      |         | Branch to switch to                |
| `create` | boolean | no       | false   | Create the branch if it doesn't exist |

Returns: confirmation with current branch name.

### Rename a branch

```
git rename_branch --old_name old-feature --new_name new-feature
```

| Argument   | Type   | Required | Default        | Description           |
|------------|--------|----------|----------------|-----------------------|
| `old_name` | string | no       | current branch | Branch to rename      |
| `new_name` | string | yes      |                | New branch name       |

Returns: confirmation with old and new branch names.

### Merge a branch

```
git merge --branch feature/new-skill --no_ff true --message "Merge feature"
```

| Argument  | Type    | Required | Default | Description                        |
|-----------|---------|----------|---------|------------------------------------|
| `branch`  | string  | yes      |         | Branch to merge into current       |
| `no_ff`   | boolean | no       | false   | Force a merge commit (--no-ff)     |
| `message` | string  | no       |         | Custom merge commit message        |

Returns: merge result with `status`, `commit_hash`, `conflicts` (if any).

### Rebase onto branch

```
git rebase --onto main --interactive false
```

| Argument      | Type    | Required | Default | Description                       |
|---------------|---------|----------|---------|-----------------------------------|
| `onto`        | string  | yes      |         | Branch or commit to rebase onto   |
| `interactive` | boolean | no       | false   | Start interactive rebase          |
| `abort`       | boolean | no       | false   | Abort an in-progress rebase       |
| `continue`    | boolean | no       | false   | Continue after resolving conflicts|

Returns: rebase result with `status` and `current_commit`.

## Staging

### Add files to staging

```
git add --files '["src/index.ts","src/utils.ts"]'
git add --all true
```

| Argument | Type     | Required | Default | Description                          |
|----------|----------|----------|---------|--------------------------------------|
| `files`  | string[] | no       |         | Specific files to stage              |
| `all`    | boolean  | no       | false   | Stage all changes (tracked + untracked) |

Returns: list of staged files.

### Reset staging / commits

```
git reset --files '["src/index.ts"]'
git reset --hard true --commit HEAD~1
git reset --soft true --commit HEAD~1
```

| Argument | Type     | Required | Default | Description                                    |
|----------|----------|----------|---------|------------------------------------------------|
| `files`  | string[] | no       |         | Specific files to unstage                      |
| `hard`   | boolean  | no       | false   | Discard all changes (--hard)                   |
| `soft`   | boolean  | no       | false   | Keep changes staged (--soft)                   |
| `commit` | string   | no       |         | Reset to specific commit (e.g. HEAD~1, sha)   |

Returns: result with new HEAD position.

### Restore file to last committed state

```
git restore --files '["src/index.ts"]' --staged true
```

| Argument | Type     | Required | Default | Description                           |
|----------|----------|----------|---------|---------------------------------------|
| `files`  | string[] | yes      |         | Files to restore                      |
| `staged` | boolean  | no       | false   | Restore from index (unstage)          |
| `source` | string   | no       |         | Restore from specific commit          |

Returns: list of restored files.

## Remote

### List remotes

```
git list_remotes
```

Returns: list of remotes with `name`, `fetch_url`, `push_url`.

### Add a remote

```
git add_remote --name upstream --url https://github.com/org/repo.git
```

| Argument | Type   | Required | Description        |
|----------|--------|----------|--------------------|
| `name`   | string | yes      | Remote name        |
| `url`    | string | yes      | Remote URL         |

Returns: confirmation with remote name and URL.

### Fetch from remote

```
git fetch --remote origin --prune true
```

| Argument | Type    | Required | Default  | Description                        |
|----------|---------|----------|----------|------------------------------------|
| `remote` | string  | no       | `origin` | Remote to fetch from               |
| `prune`  | boolean | no       | false    | Remove deleted remote branches     |

Returns: summary of fetched refs.

### Pull from remote

```
git pull --remote origin --branch main --rebase true
```

| Argument | Type    | Required | Default  | Description                      |
|----------|---------|----------|----------|----------------------------------|
| `remote` | string  | no       | `origin` | Remote to pull from              |
| `branch` | string  | no       |          | Branch to pull                   |
| `rebase` | boolean | no       | false    | Rebase instead of merge          |

Returns: pull result with `status`, `commits_pulled`, `conflicts`.

### Push to remote

```
git push --remote origin --branch main --force false --set_upstream true
```

| Argument        | Type    | Required | Default  | Description                           |
|-----------------|---------|----------|----------|---------------------------------------|
| `remote`        | string  | no       | `origin` | Remote to push to                     |
| `branch`        | string  | no       |          | Branch to push                        |
| `force`         | boolean | no       | false    | Force push (--force)                  |
| `set_upstream`  | boolean | no       | false    | Set upstream tracking (-u)            |

Returns: push result with `status` and `remote_url`.

### Clone a repository

```
git clone --url https://github.com/org/repo.git --directory ./repo --depth 1
```

| Argument    | Type   | Required | Default | Description                          |
|-------------|--------|----------|---------|--------------------------------------|
| `url`       | string | yes      |         | Repository URL to clone              |
| `directory` | string | no       |         | Target directory                     |
| `depth`     | int    | no       |         | Shallow clone depth                  |
| `branch`    | string | no       |         | Specific branch to clone             |

Returns: `path`, `default_branch`, `remote_url`.

## Stash

### Stash current changes

```
git stash --message "WIP: skill registry" --include_untracked true
```

| Argument            | Type    | Required | Default | Description                       |
|---------------------|---------|----------|---------|-----------------------------------|
| `message`           | string  | no       |         | Stash description                 |
| `include_untracked` | boolean | no       | false   | Include untracked files           |

Returns: stash reference (e.g. `stash@{0}`).

### Pop stash

```
git stash_pop --index 0
```

| Argument | Type | Required | Default | Description              |
|----------|------|----------|---------|--------------------------|
| `index`  | int  | no       | 0       | Stash index to pop       |

Returns: list of restored files.

### List stashes

```
git stash_list
```

Returns: list of stashes with `index`, `branch`, `message`, `date`.

### Drop a stash

```
git stash_drop --index 0
```

| Argument | Type | Required | Default | Description              |
|----------|------|----------|---------|--------------------------|
| `index`  | int  | yes      |         | Stash index to drop      |

Returns: confirmation of dropped stash.

### Apply stash without removing

```
git stash_apply --index 0
```

| Argument | Type | Required | Default | Description              |
|----------|------|----------|---------|--------------------------|
| `index`  | int  | no       | 0       | Stash index to apply     |

Returns: list of applied files.

## Tags

### List tags

```
git list_tags --pattern "v*" --sort "-creatordate"
```

| Argument  | Type   | Required | Default | Description                       |
|-----------|--------|----------|---------|-----------------------------------|
| `pattern` | string | no       |         | Glob pattern to filter tags       |
| `sort`    | string | no       |         | Sort field (e.g. `-creatordate`)  |

Returns: list of tags with `name` and `commit_hash`.

### Create a tag

```
git create_tag --name v1.0.0 --message "First release" --commit HEAD
```

| Argument  | Type   | Required | Default | Description                      |
|-----------|--------|----------|---------|----------------------------------|
| `name`    | string | yes      |         | Tag name                         |
| `message` | string | no       |         | Annotation message (creates annotated tag) |
| `commit`  | string | no       | HEAD    | Commit to tag                    |

Returns: `name`, `commit_hash`, `type` (lightweight or annotated).

### Delete a tag

```
git delete_tag --name v0.9.0
```

| Argument | Type   | Required | Description       |
|----------|--------|----------|-------------------|
| `name`   | string | yes      | Tag to delete     |

Returns: confirmation of deletion.

### Push tags to remote

```
git push_tags --remote origin
```

| Argument | Type   | Required | Default  | Description              |
|----------|--------|----------|----------|--------------------------|
| `remote` | string | no       | `origin` | Remote to push tags to   |

Returns: list of pushed tags.

## Blame

### Annotate file lines

```
git blame --file src/index.ts --line_start 10 --line_end 20
```

| Argument     | Type   | Required | Default | Description                    |
|--------------|--------|----------|---------|--------------------------------|
| `file`       | string | yes      |         | File to annotate               |
| `line_start` | int    | no       |         | Start line of range            |
| `line_end`   | int    | no       |         | End line of range              |

Returns: list of lines with `commit_hash`, `author`, `date`, `line_number`, `content`.

## Cherry-pick

### Cherry-pick a commit

```
git cherry_pick --commit_hash abc1234 --no_commit true
```

| Argument      | Type    | Required | Default | Description                              |
|---------------|---------|----------|---------|------------------------------------------|
| `commit_hash` | string  | yes      |         | Commit SHA to cherry-pick                |
| `no_commit`   | boolean | no       | false   | Apply changes without committing         |

Returns: result with `status`, `commit_hash`, `conflicts` (if any).

## Conflict Resolution

### List merge conflicts

```
git list_conflicts
```

Returns: list of files with conflicts and their conflict status.

### Resolve a conflict

```
git resolve_conflict --file src/index.ts --accept ours
git resolve_conflict --file src/index.ts --accept theirs
git resolve_conflict --file src/index.ts --accept manual
```

| Argument | Type   | Required | Description                                             |
|----------|--------|----------|---------------------------------------------------------|
| `file`   | string | yes      | Conflicted file to resolve                              |
| `accept` | string | yes      | Resolution strategy: `ours`, `theirs`, or `manual`      |

Returns: confirmation with resolved file path. When `manual` is used, the file is marked as resolved and must have been edited beforehand.

## Config

### Get config value

```
git get_config --key user.name --global true
```

| Argument | Type    | Required | Default | Description                    |
|----------|---------|----------|---------|--------------------------------|
| `key`    | string  | yes      |         | Config key (e.g. user.email)   |
| `global` | boolean | no       | false   | Read from global config        |

Returns: `key`, `value`, `scope`.

### Set config value

```
git set_config --key user.email --value "dev@example.com" --global true
```

| Argument | Type    | Required | Default | Description                    |
|----------|---------|----------|---------|--------------------------------|
| `key`    | string  | yes      |         | Config key to set              |
| `value`  | string  | yes      |         | Value to set                   |
| `global` | boolean | no       | false   | Write to global config         |

Returns: confirmation with `key`, `value`, `scope`.

## Clean

### Remove untracked files

```
git clean --dry_run true
git clean --force true --directories true
```

| Argument      | Type    | Required | Default | Description                           |
|---------------|---------|----------|---------|---------------------------------------|
| `dry_run`     | boolean | no       | false   | Preview files that would be removed   |
| `force`       | boolean | no       | false   | Actually remove files (-f)            |
| `directories` | boolean | no       | false   | Also remove untracked directories (-d)|

Returns: list of removed (or would-be-removed) files and directories.

## Workflow

1. Start with `git status` to understand the current working tree state.
2. Stage changes with `git add` (specific files or `--all`).
3. Review staged changes with `git diff_staged` before committing.
4. Commit with `git commit --message "descriptive message"`.
5. For feature work: `git create_branch` -> make changes -> `git push --set_upstream true` -> create PR via GitHub skill.
6. Use `git stash` to temporarily shelve work when switching contexts.
7. Resolve conflicts: `git list_conflicts` -> `git resolve_conflict` per file -> `git commit`.
8. Use `git log` and `git blame` to investigate history and authorship.

## Safety notes

- `git reset --hard` and `git clean --force` are destructive. Always preview with `git status` or `git clean --dry_run` first.
- `git push --force` rewrites remote history. Use only on feature branches, never on shared branches like `main`.
- `git delete_branch --force` permanently deletes unmerged work. Verify the branch is no longer needed.
- `git amend` rewrites the last commit. Do not amend commits already pushed to a shared branch.
- All operations run in the agent's workspace directory. Paths are relative to the repository root.
- The agent must have appropriate SSH keys or token credentials configured for remote operations.
