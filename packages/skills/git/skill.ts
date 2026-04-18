import { defineSkill, z } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

async function gitExec(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  args: string[],
) {
  const res = await ctx.fetch(ctx.credentials.proxy_url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ command: "git", args }),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Git proxy ${res.status}: ${body}`);
  }
  return res.json();
}

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Status ──────────────────────────────────────────────────────────

    status: {
      description: "Show working tree status.",
      params: z.object({}),
      returns: z.object({
        staged: z.array(z.any()).describe("Staged files"),
        unstaged: z.array(z.any()).describe("Unstaged modified files"),
        untracked: z.array(z.string()).describe("Untracked files"),
      }),
      execute: async (_params, ctx) => {
        return gitExec(ctx, ["status", "--porcelain=v2", "-z"]);
      },
    },

    diff: {
      description: "Show unstaged diff.",
      params: z.object({
        file: z.string().optional().describe("Limit diff to a specific file"),
      }),
      returns: z.object({
        diff: z.string().describe("Unified diff output"),
      }),
      execute: async (params, ctx) => {
        const args = ["diff"];
        if (params.file) args.push("--", params.file);
        return gitExec(ctx, args);
      },
    },

    diff_staged: {
      description: "Show staged (index) diff.",
      params: z.object({
        file: z.string().optional().describe("Limit diff to a specific file"),
      }),
      returns: z.object({
        diff: z.string().describe("Unified diff output"),
      }),
      execute: async (params, ctx) => {
        const args = ["diff", "--cached"];
        if (params.file) args.push("--", params.file);
        return gitExec(ctx, args);
      },
    },

    // ── Commits ─────────────────────────────────────────────────────────

    log: {
      description: "View commit log.",
      params: z.object({
        max_count: z.number().default(20).describe("Maximum number of commits to return"),
        author: z.string().optional().describe("Filter by author name or email"),
        since: z.string().optional().describe("Show commits after date (ISO or relative)"),
        until: z.string().optional().describe("Show commits before date"),
        grep: z.string().optional().describe("Filter commits by message pattern"),
        oneline: z.boolean().default(false).describe("Compact single-line output"),
      }),
      returns: z.array(
        z.object({
          hash: z.string().describe("Commit SHA"),
          author: z.string().describe("Author name"),
          date: z.string().describe("Commit date"),
          message: z.string().describe("Commit message"),
        }),
      ),
      execute: async (params, ctx) => {
        const args = ["log", `--max-count=${params.max_count}`, "--format=json"];
        if (params.author) args.push(`--author=${params.author}`);
        if (params.since) args.push(`--since=${params.since}`);
        if (params.until) args.push(`--until=${params.until}`);
        if (params.grep) args.push(`--grep=${params.grep}`);
        if (params.oneline) args.push("--oneline");
        return gitExec(ctx, args);
      },
    },

    show: {
      description: "Show commit details and diff.",
      params: z.object({
        commit_hash: z.string().describe("Commit SHA to inspect"),
      }),
      returns: z.object({
        hash: z.string().describe("Commit SHA"),
        author: z.string().describe("Author"),
        date: z.string().describe("Date"),
        message: z.string().describe("Commit message"),
        diff: z.string().describe("Full diff"),
      }),
      execute: async (params, ctx) => {
        return gitExec(ctx, ["show", params.commit_hash]);
      },
    },

    commit: {
      description: "Create a commit.",
      params: z.object({
        message: z.string().describe("Commit message"),
        all: z.boolean().default(false).describe("Automatically stage modified files (-a)"),
        amend: z.boolean().default(false).describe("Amend the previous commit"),
      }),
      returns: z.object({
        hash: z.string().describe("Commit SHA"),
        message: z.string().describe("Commit message"),
        files_changed: z.number().describe("Number of files changed"),
        insertions: z.number().describe("Lines added"),
        deletions: z.number().describe("Lines removed"),
      }),
      execute: async (params, ctx) => {
        const args = ["commit", "-m", params.message];
        if (params.all) args.push("-a");
        if (params.amend) args.push("--amend");
        return gitExec(ctx, args);
      },
    },

    // ── Branches ────────────────────────────────────────────────────────

    list_branches: {
      description: "List branches.",
      params: z.object({
        remote: z.boolean().default(false).describe("Include remote branches (-r)"),
        all: z.boolean().default(false).describe("Show local and remote (-a)"),
      }),
      returns: z.array(
        z.object({
          name: z.string().describe("Branch name"),
          current: z.boolean().describe("Is current branch"),
        }),
      ),
      execute: async (params, ctx) => {
        const args = ["branch"];
        if (params.all) args.push("-a");
        else if (params.remote) args.push("-r");
        return gitExec(ctx, args);
      },
    },

    create_branch: {
      description: "Create a new branch.",
      params: z.object({
        name: z.string().describe("New branch name"),
        start_point: z.string().optional().describe("Commit or branch to start from"),
      }),
      returns: z.object({
        name: z.string().describe("Created branch name"),
      }),
      execute: async (params, ctx) => {
        const args = ["branch", params.name];
        if (params.start_point) args.push(params.start_point);
        return gitExec(ctx, args);
      },
    },

    delete_branch: {
      description: "Delete a branch.",
      params: z.object({
        name: z.string().describe("Branch to delete"),
        force: z.boolean().default(false).describe("Force delete unmerged branch (-D)"),
      }),
      returns: z.object({
        name: z.string().describe("Deleted branch name"),
      }),
      execute: async (params, ctx) => {
        const args = ["branch", params.force ? "-D" : "-d", params.name];
        return gitExec(ctx, args);
      },
    },

    switch_branch: {
      description: "Switch to a branch.",
      params: z.object({
        name: z.string().describe("Branch to switch to"),
        create: z.boolean().default(false).describe("Create the branch if it doesn't exist"),
      }),
      returns: z.object({
        name: z.string().describe("Current branch name"),
      }),
      execute: async (params, ctx) => {
        const args = ["checkout"];
        if (params.create) args.push("-b");
        args.push(params.name);
        return gitExec(ctx, args);
      },
    },

    rename_branch: {
      description: "Rename a branch.",
      params: z.object({
        old_name: z.string().optional().describe("Branch to rename (defaults to current)"),
        new_name: z.string().describe("New branch name"),
      }),
      returns: z.object({
        old_name: z.string().describe("Previous branch name"),
        new_name: z.string().describe("New branch name"),
      }),
      execute: async (params, ctx) => {
        const args = ["branch", "-m"];
        if (params.old_name) args.push(params.old_name);
        args.push(params.new_name);
        return gitExec(ctx, args);
      },
    },

    merge: {
      description: "Merge a branch into the current branch.",
      params: z.object({
        branch: z.string().describe("Branch to merge into current"),
        no_ff: z.boolean().default(false).describe("Force a merge commit (--no-ff)"),
        message: z.string().optional().describe("Custom merge commit message"),
      }),
      returns: z.object({
        status: z.string().describe("Merge result status"),
        commit_hash: z.string().optional().describe("Merge commit SHA"),
        conflicts: z.array(z.string()).optional().describe("Conflicting files"),
      }),
      execute: async (params, ctx) => {
        const args = ["merge"];
        if (params.no_ff) args.push("--no-ff");
        if (params.message) args.push("-m", params.message);
        args.push(params.branch);
        return gitExec(ctx, args);
      },
    },

    rebase: {
      description: "Rebase current branch onto another.",
      params: z.object({
        onto: z.string().describe("Branch or commit to rebase onto"),
        interactive: z.boolean().default(false).describe("Start interactive rebase"),
        abort: z.boolean().default(false).describe("Abort an in-progress rebase"),
        continue: z.boolean().default(false).describe("Continue after resolving conflicts"),
      }),
      returns: z.object({
        status: z.string().describe("Rebase result status"),
        current_commit: z.string().optional().describe("Current commit after rebase"),
      }),
      execute: async (params, ctx) => {
        if (params.abort) return gitExec(ctx, ["rebase", "--abort"]);
        if (params.continue) return gitExec(ctx, ["rebase", "--continue"]);
        const args = ["rebase"];
        if (params.interactive) args.push("-i");
        args.push(params.onto);
        return gitExec(ctx, args);
      },
    },

    // ── Staging ──────────────────────────────────────────────────────────

    add: {
      description: "Add files to staging.",
      params: z.object({
        files: z.array(z.string()).optional().describe("Specific files to stage"),
        all: z.boolean().default(false).describe("Stage all changes (tracked + untracked)"),
      }),
      returns: z.object({
        staged: z.array(z.string()).describe("List of staged files"),
      }),
      execute: async (params, ctx) => {
        const args = ["add"];
        if (params.all) args.push("-A");
        else if (params.files && params.files.length > 0) args.push(...params.files);
        return gitExec(ctx, args);
      },
    },

    reset: {
      description: "Reset staging or commits.",
      params: z.object({
        files: z.array(z.string()).optional().describe("Specific files to unstage"),
        hard: z.boolean().default(false).describe("Discard all changes (--hard)"),
        soft: z.boolean().default(false).describe("Keep changes staged (--soft)"),
        commit: z.string().optional().describe("Reset to specific commit (e.g. HEAD~1)"),
      }),
      returns: z.object({
        head: z.string().describe("New HEAD position"),
      }),
      execute: async (params, ctx) => {
        const args = ["reset"];
        if (params.hard) args.push("--hard");
        else if (params.soft) args.push("--soft");
        if (params.commit) args.push(params.commit);
        if (params.files && params.files.length > 0) args.push("--", ...params.files);
        return gitExec(ctx, args);
      },
    },

    restore: {
      description: "Restore files to last committed state.",
      params: z.object({
        files: z.array(z.string()).describe("Files to restore"),
        staged: z.boolean().default(false).describe("Restore from index (unstage)"),
        source: z.string().optional().describe("Restore from specific commit"),
      }),
      returns: z.object({
        restored: z.array(z.string()).describe("List of restored files"),
      }),
      execute: async (params, ctx) => {
        const args = ["restore"];
        if (params.staged) args.push("--staged");
        if (params.source) args.push(`--source=${params.source}`);
        args.push(...params.files);
        return gitExec(ctx, args);
      },
    },

    // ── Remote ──────────────────────────────────────────────────────────

    list_remotes: {
      description: "List configured remotes.",
      params: z.object({}),
      returns: z.array(
        z.object({
          name: z.string().describe("Remote name"),
          fetch_url: z.string().describe("Fetch URL"),
          push_url: z.string().describe("Push URL"),
        }),
      ),
      execute: async (_params, ctx) => {
        return gitExec(ctx, ["remote", "-v"]);
      },
    },

    add_remote: {
      description: "Add a new remote.",
      params: z.object({
        name: z.string().describe("Remote name"),
        url: z.string().describe("Remote URL"),
      }),
      returns: z.object({
        name: z.string().describe("Remote name"),
        url: z.string().describe("Remote URL"),
      }),
      execute: async (params, ctx) => {
        return gitExec(ctx, ["remote", "add", params.name, params.url]);
      },
    },

    fetch: {
      description: "Fetch from a remote.",
      params: z.object({
        remote: z.string().default("origin").describe("Remote to fetch from"),
        prune: z.boolean().default(false).describe("Remove deleted remote branches"),
      }),
      returns: z.object({
        summary: z.string().describe("Fetch summary"),
      }),
      execute: async (params, ctx) => {
        const args = ["fetch", params.remote];
        if (params.prune) args.push("--prune");
        return gitExec(ctx, args);
      },
    },

    pull: {
      description: "Pull from a remote.",
      params: z.object({
        remote: z.string().default("origin").describe("Remote to pull from"),
        branch: z.string().optional().describe("Branch to pull"),
        rebase: z.boolean().default(false).describe("Rebase instead of merge"),
      }),
      returns: z.object({
        status: z.string().describe("Pull result status"),
        commits_pulled: z.number().optional().describe("Number of commits pulled"),
        conflicts: z.array(z.string()).optional().describe("Conflicting files"),
      }),
      execute: async (params, ctx) => {
        const args = ["pull"];
        if (params.rebase) args.push("--rebase");
        args.push(params.remote);
        if (params.branch) args.push(params.branch);
        return gitExec(ctx, args);
      },
    },

    push: {
      description: "Push to a remote.",
      params: z.object({
        remote: z.string().default("origin").describe("Remote to push to"),
        branch: z.string().optional().describe("Branch to push"),
        force: z.boolean().default(false).describe("Force push (--force)"),
        set_upstream: z.boolean().default(false).describe("Set upstream tracking (-u)"),
      }),
      returns: z.object({
        status: z.string().describe("Push result status"),
        remote_url: z.string().optional().describe("Remote URL"),
      }),
      execute: async (params, ctx) => {
        const args = ["push"];
        if (params.force) args.push("--force");
        if (params.set_upstream) args.push("-u");
        args.push(params.remote);
        if (params.branch) args.push(params.branch);
        return gitExec(ctx, args);
      },
    },

    clone: {
      description: "Clone a repository.",
      params: z.object({
        url: z.string().describe("Repository URL to clone"),
        directory: z.string().optional().describe("Target directory"),
        depth: z.number().optional().describe("Shallow clone depth"),
        branch: z.string().optional().describe("Specific branch to clone"),
      }),
      returns: z.object({
        path: z.string().describe("Local path"),
        default_branch: z.string().describe("Default branch"),
        remote_url: z.string().describe("Remote URL"),
      }),
      execute: async (params, ctx) => {
        const args = ["clone"];
        if (params.depth) args.push(`--depth=${params.depth}`);
        if (params.branch) args.push(`--branch=${params.branch}`);
        args.push(params.url);
        if (params.directory) args.push(params.directory);
        return gitExec(ctx, args);
      },
    },

    // ── Stash ───────────────────────────────────────────────────────────

    stash: {
      description: "Stash current changes.",
      params: z.object({
        message: z.string().optional().describe("Stash description"),
        include_untracked: z.boolean().default(false).describe("Include untracked files"),
      }),
      returns: z.object({
        ref: z.string().describe("Stash reference (e.g. stash@{0})"),
      }),
      execute: async (params, ctx) => {
        const args = ["stash", "push"];
        if (params.message) args.push("-m", params.message);
        if (params.include_untracked) args.push("-u");
        return gitExec(ctx, args);
      },
    },

    stash_pop: {
      description: "Pop a stash entry.",
      params: z.object({
        index: z.number().default(0).describe("Stash index to pop"),
      }),
      returns: z.object({
        restored: z.array(z.string()).describe("List of restored files"),
      }),
      execute: async (params, ctx) => {
        return gitExec(ctx, ["stash", "pop", `stash@{${params.index}}`]);
      },
    },

    stash_list: {
      description: "List all stash entries.",
      params: z.object({}),
      returns: z.array(
        z.object({
          index: z.number().describe("Stash index"),
          branch: z.string().describe("Branch stashed from"),
          message: z.string().describe("Stash message"),
          date: z.string().describe("Stash date"),
        }),
      ),
      execute: async (_params, ctx) => {
        return gitExec(ctx, ["stash", "list"]);
      },
    },

    stash_drop: {
      description: "Drop a stash entry.",
      params: z.object({
        index: z.number().describe("Stash index to drop"),
      }),
      returns: z.object({
        dropped: z.string().describe("Dropped stash reference"),
      }),
      execute: async (params, ctx) => {
        return gitExec(ctx, ["stash", "drop", `stash@{${params.index}}`]);
      },
    },

    stash_apply: {
      description: "Apply a stash entry without removing it.",
      params: z.object({
        index: z.number().default(0).describe("Stash index to apply"),
      }),
      returns: z.object({
        applied: z.array(z.string()).describe("List of applied files"),
      }),
      execute: async (params, ctx) => {
        return gitExec(ctx, ["stash", "apply", `stash@{${params.index}}`]);
      },
    },

    // ── Tags ────────────────────────────────────────────────────────────

    list_tags: {
      description: "List tags.",
      params: z.object({
        pattern: z.string().optional().describe("Glob pattern to filter tags"),
        sort: z.string().optional().describe("Sort field (e.g. -creatordate)"),
      }),
      returns: z.array(
        z.object({
          name: z.string().describe("Tag name"),
          commit_hash: z.string().describe("Commit SHA"),
        }),
      ),
      execute: async (params, ctx) => {
        const args = ["tag", "-l"];
        if (params.sort) args.push(`--sort=${params.sort}`);
        if (params.pattern) args.push(params.pattern);
        return gitExec(ctx, args);
      },
    },

    create_tag: {
      description: "Create a tag.",
      params: z.object({
        name: z.string().describe("Tag name"),
        message: z.string().optional().describe("Annotation message (creates annotated tag)"),
        commit: z.string().optional().describe("Commit to tag (defaults to HEAD)"),
      }),
      returns: z.object({
        name: z.string().describe("Tag name"),
        commit_hash: z.string().describe("Commit SHA"),
        type: z.string().describe("Tag type (lightweight or annotated)"),
      }),
      execute: async (params, ctx) => {
        const args = ["tag"];
        if (params.message) args.push("-a", params.name, "-m", params.message);
        else args.push(params.name);
        if (params.commit) args.push(params.commit);
        return gitExec(ctx, args);
      },
    },

    delete_tag: {
      description: "Delete a tag.",
      params: z.object({
        name: z.string().describe("Tag to delete"),
      }),
      returns: z.object({
        name: z.string().describe("Deleted tag name"),
      }),
      execute: async (params, ctx) => {
        return gitExec(ctx, ["tag", "-d", params.name]);
      },
    },

    push_tags: {
      description: "Push all tags to remote.",
      params: z.object({
        remote: z.string().default("origin").describe("Remote to push tags to"),
      }),
      returns: z.object({
        pushed: z.array(z.string()).describe("List of pushed tags"),
      }),
      execute: async (params, ctx) => {
        return gitExec(ctx, ["push", params.remote, "--tags"]);
      },
    },

    // ── Blame ───────────────────────────────────────────────────────────

    blame: {
      description: "Annotate file lines with commit info.",
      params: z.object({
        file: z.string().describe("File to annotate"),
        line_start: z.number().optional().describe("Start line of range"),
        line_end: z.number().optional().describe("End line of range"),
      }),
      returns: z.array(
        z.object({
          commit_hash: z.string().describe("Commit SHA"),
          author: z.string().describe("Author"),
          date: z.string().describe("Date"),
          line_number: z.number().describe("Line number"),
          content: z.string().describe("Line content"),
        }),
      ),
      execute: async (params, ctx) => {
        const args = ["blame", "--porcelain"];
        if (params.line_start !== undefined && params.line_end !== undefined) {
          args.push(`-L${params.line_start},${params.line_end}`);
        }
        args.push(params.file);
        return gitExec(ctx, args);
      },
    },

    // ── Cherry-pick ─────────────────────────────────────────────────────

    cherry_pick: {
      description: "Cherry-pick a commit.",
      params: z.object({
        commit_hash: z.string().describe("Commit SHA to cherry-pick"),
        no_commit: z.boolean().default(false).describe("Apply changes without committing"),
      }),
      returns: z.object({
        status: z.string().describe("Cherry-pick result status"),
        commit_hash: z.string().optional().describe("New commit SHA"),
        conflicts: z.array(z.string()).optional().describe("Conflicting files"),
      }),
      execute: async (params, ctx) => {
        const args = ["cherry-pick"];
        if (params.no_commit) args.push("--no-commit");
        args.push(params.commit_hash);
        return gitExec(ctx, args);
      },
    },

    // ── Conflict Resolution ─────────────────────────────────────────────

    list_conflicts: {
      description: "List files with merge conflicts.",
      params: z.object({}),
      returns: z.array(
        z.object({
          file: z.string().describe("Conflicting file path"),
          status: z.string().describe("Conflict status"),
        }),
      ),
      execute: async (_params, ctx) => {
        return gitExec(ctx, ["diff", "--name-only", "--diff-filter=U"]);
      },
    },

    resolve_conflict: {
      description: "Resolve a merge conflict for a file.",
      params: z.object({
        file: z.string().describe("Conflicted file to resolve"),
        accept: z
          .enum(["ours", "theirs", "manual"])
          .describe("Resolution strategy: ours, theirs, or manual"),
      }),
      returns: z.object({
        file: z.string().describe("Resolved file path"),
      }),
      execute: async (params, ctx) => {
        if (params.accept === "manual") {
          return gitExec(ctx, ["add", params.file]);
        }
        await gitExec(ctx, ["checkout", `--${params.accept}`, "--", params.file]);
        return gitExec(ctx, ["add", params.file]);
      },
    },

    // ── Config ──────────────────────────────────────────────────────────

    get_config: {
      description: "Get a git config value.",
      params: z.object({
        key: z.string().describe("Config key (e.g. user.email)"),
        global: z.boolean().default(false).describe("Read from global config"),
      }),
      returns: z.object({
        key: z.string().describe("Config key"),
        value: z.string().describe("Config value"),
        scope: z.string().describe("Config scope (local or global)"),
      }),
      execute: async (params, ctx) => {
        const args = ["config"];
        if (params.global) args.push("--global");
        args.push("--get", params.key);
        return gitExec(ctx, args);
      },
    },

    set_config: {
      description: "Set a git config value.",
      params: z.object({
        key: z.string().describe("Config key to set"),
        value: z.string().describe("Value to set"),
        global: z.boolean().default(false).describe("Write to global config"),
      }),
      returns: z.object({
        key: z.string().describe("Config key"),
        value: z.string().describe("Config value"),
        scope: z.string().describe("Config scope (local or global)"),
      }),
      execute: async (params, ctx) => {
        const args = ["config"];
        if (params.global) args.push("--global");
        args.push(params.key, params.value);
        return gitExec(ctx, args);
      },
    },

    // ── Clean ───────────────────────────────────────────────────────────

    clean: {
      description: "Remove untracked files.",
      params: z.object({
        dry_run: z.boolean().default(false).describe("Preview files that would be removed"),
        force: z.boolean().default(false).describe("Actually remove files (-f)"),
        directories: z.boolean().default(false).describe("Also remove untracked directories (-d)"),
      }),
      returns: z.object({
        files: z.array(z.string()).describe("Removed or would-be-removed files"),
      }),
      execute: async (params, ctx) => {
        const args = ["clean"];
        if (params.dry_run) args.push("-n");
        if (params.force) args.push("-f");
        if (params.directories) args.push("-d");
        return gitExec(ctx, args);
      },
    },
  },
});
