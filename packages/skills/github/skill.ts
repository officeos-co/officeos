import { defineSkill, z } from "@harro/skill-sdk";

const GITHUB_API = "https://api.github.com";

function ghHeaders(token: string) {
  return {
    Authorization: `Bearer ${token}`,
    Accept: "application/vnd.github+json",
    "X-GitHub-Api-Version": "2022-11-28",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

function ghJsonHeaders(token: string) {
  return {
    ...ghHeaders(token),
    "Content-Type": "application/json",
  };
}

async function ghFetch(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  url: string,
  init?: RequestInit,
) {
  const res = await ctx.fetch(url, {
    ...init,
    headers: { ...ghHeaders(ctx.credentials.token), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`GitHub API ${res.status}: ${body}`);
  }
  return res.json();
}

async function ghPost(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  url: string,
  body: unknown,
  method = "POST",
) {
  const res = await ctx.fetch(url, {
    method,
    headers: ghJsonHeaders(ctx.credentials.token),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`GitHub API ${res.status}: ${text}`);
  }
  return res.json();
}

function enc(s: string) {
  return encodeURIComponent(s);
}

function repoUrl(owner: string, repo: string) {
  return `${GITHUB_API}/repos/${enc(owner)}/${enc(repo)}`;
}

// Common param fragments
const ownerRepo = {
  owner: z.string().describe("Repository owner"),
  repo: z.string().describe("Repository name"),
};

import doc from "./SKILL.md";

export default defineSkill({
  name: "github",
  title: "GitHub",
  emoji: "\ud83d\udc19",
  description:
    "Full GitHub CLI parity: repositories, issues, PRs, releases, workflows, gists, search, and org management via the GitHub REST API.",
  doc,

  credentials: {
    token: {
      label: "Personal Access Token",
      kind: "password",
      placeholder: "github_pat_\u2026 or ghp_\u2026",
      help: "Fine-grained PAT recommended. Needs appropriate scopes for the actions you want to use. Create one at https://github.com/settings/tokens.",
    },
  },

  actions: {
    // ── Repository operations ──────────────────────────────────────────

    list_repos: {
      description: "List repositories accessible to the authenticated user.",
      params: z.object({
        visibility: z
          .enum(["all", "public", "private"])
          .default("all")
          .describe("Filter by visibility"),
        per_page: z.number().min(1).max(100).default(30),
      }),
      returns: z.array(
        z.object({
          full_name: z.string(),
          private: z.boolean(),
          description: z.string().nullable(),
          html_url: z.string(),
          default_branch: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${GITHUB_API}/user/repos?visibility=${params.visibility}&per_page=${params.per_page}`,
        );
        return data.map((r: any) => ({
          full_name: r.full_name,
          private: r.private,
          description: r.description,
          html_url: r.html_url,
          default_branch: r.default_branch,
        }));
      },
    },

    get_repo: {
      description: "Get detailed information about a single repository.",
      params: z.object({ ...ownerRepo }),
      returns: z.object({
        full_name: z.string(),
        private: z.boolean(),
        description: z.string().nullable(),
        html_url: z.string(),
        default_branch: z.string(),
        language: z.string().nullable(),
        stargazers_count: z.number(),
        forks_count: z.number(),
        open_issues_count: z.number(),
        topics: z.array(z.string()),
        created_at: z.string(),
        updated_at: z.string(),
      }),
      execute: async (params, ctx) => {
        const r = await ghFetch(ctx, repoUrl(params.owner, params.repo));
        return {
          full_name: r.full_name,
          private: r.private,
          description: r.description,
          html_url: r.html_url,
          default_branch: r.default_branch,
          language: r.language,
          stargazers_count: r.stargazers_count,
          forks_count: r.forks_count,
          open_issues_count: r.open_issues_count,
          topics: r.topics ?? [],
          created_at: r.created_at,
          updated_at: r.updated_at,
        };
      },
    },

    create_repo: {
      description: "Create a new repository for the authenticated user.",
      params: z.object({
        name: z.string().describe("Repository name"),
        description: z.string().optional().describe("Repository description"),
        private: z.boolean().default(false).describe("Whether the repo is private"),
        auto_init: z.boolean().default(true).describe("Initialize with a README"),
      }),
      returns: z.object({
        full_name: z.string(),
        html_url: z.string(),
        clone_url: z.string(),
        default_branch: z.string(),
      }),
      execute: async (params, ctx) => {
        const r = await ghPost(ctx, `${GITHUB_API}/user/repos`, {
          name: params.name,
          description: params.description,
          private: params.private,
          auto_init: params.auto_init,
        });
        return {
          full_name: r.full_name,
          html_url: r.html_url,
          clone_url: r.clone_url,
          default_branch: r.default_branch,
        };
      },
    },

    clone_repo: {
      description:
        "Get the clone URL for a repository. The agent cannot clone directly but can use the URL.",
      params: z.object({ ...ownerRepo }),
      returns: z.object({
        clone_url: z.string(),
        ssh_url: z.string(),
        html_url: z.string(),
      }),
      execute: async (params, ctx) => {
        const r = await ghFetch(ctx, repoUrl(params.owner, params.repo));
        return {
          clone_url: r.clone_url,
          ssh_url: r.ssh_url,
          html_url: r.html_url,
        };
      },
    },

    list_repo_topics: {
      description: "List topics for a repository.",
      params: z.object({ ...ownerRepo }),
      returns: z.array(z.string()),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${repoUrl(params.owner, params.repo)}/topics`,
        );
        return data.names ?? [];
      },
    },

    set_repo_topics: {
      description: "Replace all topics for a repository.",
      params: z.object({
        ...ownerRepo,
        topics: z.array(z.string()).describe("Array of topic names"),
      }),
      returns: z.array(z.string()),
      execute: async (params, ctx) => {
        const data = await ghPost(
          ctx,
          `${repoUrl(params.owner, params.repo)}/topics`,
          { names: params.topics },
          "PUT",
        );
        return data.names ?? [];
      },
    },

    // ── Issues ─────────────────────────────────────────────────────────

    list_issues: {
      description: "List issues in a repository (excludes pull requests).",
      params: z.object({
        ...ownerRepo,
        state: z
          .enum(["open", "closed", "all"])
          .default("open")
          .describe("Issue state filter"),
        per_page: z.number().min(1).max(100).default(30),
      }),
      returns: z.array(
        z.object({
          number: z.number(),
          title: z.string(),
          state: z.string(),
          author: z.string().nullable(),
          labels: z.array(z.string()),
          html_url: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${repoUrl(params.owner, params.repo)}/issues?state=${params.state}&per_page=${params.per_page}`,
        );
        return data
          .filter((i: any) => !i.pull_request)
          .map((i: any) => ({
            number: i.number,
            title: i.title,
            state: i.state,
            author: i.user?.login ?? null,
            labels: (i.labels ?? []).map((l: any) => l.name),
            html_url: i.html_url,
          }));
      },
    },

    get_issue: {
      description: "Get a single issue with its body and comments.",
      params: z.object({
        ...ownerRepo,
        issue_number: z.number().describe("Issue number"),
      }),
      returns: z.object({
        number: z.number(),
        title: z.string(),
        state: z.string(),
        author: z.string().nullable(),
        body: z.string().nullable(),
        labels: z.array(z.string()),
        assignees: z.array(z.string()),
        html_url: z.string(),
        created_at: z.string(),
        updated_at: z.string(),
        comments_count: z.number(),
      }),
      execute: async (params, ctx) => {
        const i = await ghFetch(
          ctx,
          `${repoUrl(params.owner, params.repo)}/issues/${params.issue_number}`,
        );
        return {
          number: i.number,
          title: i.title,
          state: i.state,
          author: i.user?.login ?? null,
          body: i.body,
          labels: (i.labels ?? []).map((l: any) => l.name),
          assignees: (i.assignees ?? []).map((a: any) => a.login),
          html_url: i.html_url,
          created_at: i.created_at,
          updated_at: i.updated_at,
          comments_count: i.comments,
        };
      },
    },

    create_issue: {
      description: "Create a new issue in a repository.",
      params: z.object({
        ...ownerRepo,
        title: z.string().describe("Issue title"),
        body: z.string().optional().describe("Issue body (markdown)"),
        labels: z.array(z.string()).optional().describe("Labels to apply"),
        assignees: z.array(z.string()).optional().describe("Usernames to assign"),
      }),
      returns: z.object({
        number: z.number(),
        title: z.string(),
        html_url: z.string(),
      }),
      execute: async (params, ctx) => {
        const i = await ghPost(
          ctx,
          `${repoUrl(params.owner, params.repo)}/issues`,
          {
            title: params.title,
            body: params.body,
            labels: params.labels,
            assignees: params.assignees,
          },
        );
        return { number: i.number, title: i.title, html_url: i.html_url };
      },
    },

    edit_issue: {
      description: "Update an issue's title, body, labels, or assignees.",
      params: z.object({
        ...ownerRepo,
        issue_number: z.number().describe("Issue number"),
        title: z.string().optional().describe("New title"),
        body: z.string().optional().describe("New body"),
        labels: z.array(z.string()).optional().describe("Replace labels"),
        assignees: z.array(z.string()).optional().describe("Replace assignees"),
      }),
      returns: z.object({
        number: z.number(),
        title: z.string(),
        html_url: z.string(),
      }),
      execute: async (params, ctx) => {
        const payload: any = {};
        if (params.title !== undefined) payload.title = params.title;
        if (params.body !== undefined) payload.body = params.body;
        if (params.labels !== undefined) payload.labels = params.labels;
        if (params.assignees !== undefined) payload.assignees = params.assignees;
        const i = await ghPost(
          ctx,
          `${repoUrl(params.owner, params.repo)}/issues/${params.issue_number}`,
          payload,
          "PATCH",
        );
        return { number: i.number, title: i.title, html_url: i.html_url };
      },
    },

    close_issue: {
      description: "Close an issue.",
      params: z.object({
        ...ownerRepo,
        issue_number: z.number().describe("Issue number"),
      }),
      returns: z.object({ number: z.number(), state: z.string() }),
      execute: async (params, ctx) => {
        const i = await ghPost(
          ctx,
          `${repoUrl(params.owner, params.repo)}/issues/${params.issue_number}`,
          { state: "closed" },
          "PATCH",
        );
        return { number: i.number, state: i.state };
      },
    },

    reopen_issue: {
      description: "Reopen a closed issue.",
      params: z.object({
        ...ownerRepo,
        issue_number: z.number().describe("Issue number"),
      }),
      returns: z.object({ number: z.number(), state: z.string() }),
      execute: async (params, ctx) => {
        const i = await ghPost(
          ctx,
          `${repoUrl(params.owner, params.repo)}/issues/${params.issue_number}`,
          { state: "open" },
          "PATCH",
        );
        return { number: i.number, state: i.state };
      },
    },

    list_issue_comments: {
      description: "List comments on an issue.",
      params: z.object({
        ...ownerRepo,
        issue_number: z.number().describe("Issue number"),
        per_page: z.number().min(1).max(100).default(30),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          author: z.string().nullable(),
          body: z.string(),
          created_at: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${repoUrl(params.owner, params.repo)}/issues/${params.issue_number}/comments?per_page=${params.per_page}`,
        );
        return data.map((c: any) => ({
          id: c.id,
          author: c.user?.login ?? null,
          body: c.body,
          created_at: c.created_at,
        }));
      },
    },

    add_issue_comment: {
      description: "Add a comment to an issue.",
      params: z.object({
        ...ownerRepo,
        issue_number: z.number().describe("Issue number"),
        body: z.string().describe("Comment body (markdown)"),
      }),
      returns: z.object({
        id: z.number(),
        html_url: z.string(),
      }),
      execute: async (params, ctx) => {
        const c = await ghPost(
          ctx,
          `${repoUrl(params.owner, params.repo)}/issues/${params.issue_number}/comments`,
          { body: params.body },
        );
        return { id: c.id, html_url: c.html_url };
      },
    },

    // ── Pull Requests ──────────────────────────────────────────────────

    list_prs: {
      description: "List pull requests in a repository.",
      params: z.object({
        ...ownerRepo,
        state: z
          .enum(["open", "closed", "all"])
          .default("open")
          .describe("PR state filter"),
        per_page: z.number().min(1).max(100).default(30),
      }),
      returns: z.array(
        z.object({
          number: z.number(),
          title: z.string(),
          state: z.string(),
          author: z.string().nullable(),
          html_url: z.string(),
          draft: z.boolean(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${repoUrl(params.owner, params.repo)}/pulls?state=${params.state}&per_page=${params.per_page}`,
        );
        return data.map((pr: any) => ({
          number: pr.number,
          title: pr.title,
          state: pr.state,
          author: pr.user?.login ?? null,
          html_url: pr.html_url,
          draft: pr.draft,
        }));
      },
    },

    get_pr: {
      description: "Get detailed information about a pull request.",
      params: z.object({
        ...ownerRepo,
        pr_number: z.number().describe("PR number"),
      }),
      returns: z.object({
        number: z.number(),
        title: z.string(),
        state: z.string(),
        author: z.string().nullable(),
        body: z.string().nullable(),
        draft: z.boolean(),
        merged: z.boolean(),
        mergeable: z.boolean().nullable(),
        head_ref: z.string(),
        base_ref: z.string(),
        html_url: z.string(),
        additions: z.number(),
        deletions: z.number(),
        changed_files: z.number(),
        created_at: z.string(),
        updated_at: z.string(),
      }),
      execute: async (params, ctx) => {
        const pr = await ghFetch(
          ctx,
          `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}`,
        );
        return {
          number: pr.number,
          title: pr.title,
          state: pr.state,
          author: pr.user?.login ?? null,
          body: pr.body,
          draft: pr.draft,
          merged: pr.merged,
          mergeable: pr.mergeable,
          head_ref: pr.head?.ref,
          base_ref: pr.base?.ref,
          html_url: pr.html_url,
          additions: pr.additions,
          deletions: pr.deletions,
          changed_files: pr.changed_files,
          created_at: pr.created_at,
          updated_at: pr.updated_at,
        };
      },
    },

    create_pr: {
      description: "Create a new pull request.",
      params: z.object({
        ...ownerRepo,
        title: z.string().describe("PR title"),
        body: z.string().optional().describe("PR body (markdown)"),
        head: z.string().describe("Branch containing changes"),
        base: z.string().describe("Branch to merge into"),
        draft: z.boolean().default(false).describe("Create as draft PR"),
      }),
      returns: z.object({
        number: z.number(),
        title: z.string(),
        html_url: z.string(),
      }),
      execute: async (params, ctx) => {
        const pr = await ghPost(
          ctx,
          `${repoUrl(params.owner, params.repo)}/pulls`,
          {
            title: params.title,
            body: params.body,
            head: params.head,
            base: params.base,
            draft: params.draft,
          },
        );
        return { number: pr.number, title: pr.title, html_url: pr.html_url };
      },
    },

    merge_pr: {
      description: "Merge a pull request.",
      params: z.object({
        ...ownerRepo,
        pr_number: z.number().describe("PR number"),
        merge_method: z
          .enum(["merge", "squash", "rebase"])
          .default("merge")
          .describe("Merge strategy"),
        commit_title: z.string().optional().describe("Custom merge commit title"),
        commit_message: z.string().optional().describe("Custom merge commit message"),
      }),
      returns: z.object({
        merged: z.boolean(),
        message: z.string(),
        sha: z.string(),
      }),
      execute: async (params, ctx) => {
        const payload: any = { merge_method: params.merge_method };
        if (params.commit_title) payload.commit_title = params.commit_title;
        if (params.commit_message) payload.commit_message = params.commit_message;
        const r = await ghPost(
          ctx,
          `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}/merge`,
          payload,
          "PUT",
        );
        return { merged: r.merged, message: r.message, sha: r.sha };
      },
    },

    close_pr: {
      description: "Close a pull request without merging.",
      params: z.object({
        ...ownerRepo,
        pr_number: z.number().describe("PR number"),
      }),
      returns: z.object({ number: z.number(), state: z.string() }),
      execute: async (params, ctx) => {
        const pr = await ghPost(
          ctx,
          `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}`,
          { state: "closed" },
          "PATCH",
        );
        return { number: pr.number, state: pr.state };
      },
    },

    reopen_pr: {
      description: "Reopen a closed pull request.",
      params: z.object({
        ...ownerRepo,
        pr_number: z.number().describe("PR number"),
      }),
      returns: z.object({ number: z.number(), state: z.string() }),
      execute: async (params, ctx) => {
        const pr = await ghPost(
          ctx,
          `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}`,
          { state: "open" },
          "PATCH",
        );
        return { number: pr.number, state: pr.state };
      },
    },

    list_pr_comments: {
      description: "List review comments on a pull request.",
      params: z.object({
        ...ownerRepo,
        pr_number: z.number().describe("PR number"),
        per_page: z.number().min(1).max(100).default(30),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          author: z.string().nullable(),
          body: z.string(),
          path: z.string().nullable(),
          created_at: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}/comments?per_page=${params.per_page}`,
        );
        return data.map((c: any) => ({
          id: c.id,
          author: c.user?.login ?? null,
          body: c.body,
          path: c.path ?? null,
          created_at: c.created_at,
        }));
      },
    },

    add_pr_comment: {
      description:
        "Add a general comment to a pull request (uses the issue comment endpoint).",
      params: z.object({
        ...ownerRepo,
        pr_number: z.number().describe("PR number"),
        body: z.string().describe("Comment body (markdown)"),
      }),
      returns: z.object({
        id: z.number(),
        html_url: z.string(),
      }),
      execute: async (params, ctx) => {
        const c = await ghPost(
          ctx,
          `${repoUrl(params.owner, params.repo)}/issues/${params.pr_number}/comments`,
          { body: params.body },
        );
        return { id: c.id, html_url: c.html_url };
      },
    },

    list_pr_reviews: {
      description: "List reviews on a pull request.",
      params: z.object({
        ...ownerRepo,
        pr_number: z.number().describe("PR number"),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          author: z.string().nullable(),
          state: z.string(),
          body: z.string().nullable(),
          submitted_at: z.string().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}/reviews`,
        );
        return data.map((r: any) => ({
          id: r.id,
          author: r.user?.login ?? null,
          state: r.state,
          body: r.body ?? null,
          submitted_at: r.submitted_at ?? null,
        }));
      },
    },

    request_pr_review: {
      description: "Request reviews on a pull request.",
      params: z.object({
        ...ownerRepo,
        pr_number: z.number().describe("PR number"),
        reviewers: z
          .array(z.string())
          .optional()
          .describe("Individual GitHub usernames"),
        team_reviewers: z
          .array(z.string())
          .optional()
          .describe("Team slugs"),
      }),
      returns: z.object({
        requested_reviewers: z.array(z.string()),
        requested_teams: z.array(z.string()),
      }),
      execute: async (params, ctx) => {
        const payload: any = {};
        if (params.reviewers) payload.reviewers = params.reviewers;
        if (params.team_reviewers) payload.team_reviewers = params.team_reviewers;
        const r = await ghPost(
          ctx,
          `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}/requested_reviewers`,
          payload,
        );
        return {
          requested_reviewers: (r.requested_reviewers ?? []).map(
            (u: any) => u.login,
          ),
          requested_teams: (r.requested_teams ?? []).map((t: any) => t.slug),
        };
      },
    },

    list_pr_files: {
      description: "List files changed in a pull request.",
      params: z.object({
        ...ownerRepo,
        pr_number: z.number().describe("PR number"),
        per_page: z.number().min(1).max(100).default(30),
      }),
      returns: z.array(
        z.object({
          filename: z.string(),
          status: z.string(),
          additions: z.number(),
          deletions: z.number(),
          changes: z.number(),
          patch: z.string().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}/files?per_page=${params.per_page}`,
        );
        return data.map((f: any) => ({
          filename: f.filename,
          status: f.status,
          additions: f.additions,
          deletions: f.deletions,
          changes: f.changes,
          patch: f.patch ?? null,
        }));
      },
    },

    // ── Releases ───────────────────────────────────────────────────────

    list_releases: {
      description: "List releases for a repository.",
      params: z.object({
        ...ownerRepo,
        per_page: z.number().min(1).max(100).default(10),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          tag_name: z.string(),
          name: z.string().nullable(),
          draft: z.boolean(),
          prerelease: z.boolean(),
          html_url: z.string(),
          published_at: z.string().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${repoUrl(params.owner, params.repo)}/releases?per_page=${params.per_page}`,
        );
        return data.map((r: any) => ({
          id: r.id,
          tag_name: r.tag_name,
          name: r.name,
          draft: r.draft,
          prerelease: r.prerelease,
          html_url: r.html_url,
          published_at: r.published_at,
        }));
      },
    },

    get_release: {
      description: "Get a specific release by ID.",
      params: z.object({
        ...ownerRepo,
        release_id: z.number().describe("Release ID"),
      }),
      returns: z.object({
        id: z.number(),
        tag_name: z.string(),
        name: z.string().nullable(),
        body: z.string().nullable(),
        draft: z.boolean(),
        prerelease: z.boolean(),
        html_url: z.string(),
        published_at: z.string().nullable(),
        assets: z.array(
          z.object({
            name: z.string(),
            download_url: z.string(),
            size: z.number(),
          }),
        ),
      }),
      execute: async (params, ctx) => {
        const r = await ghFetch(
          ctx,
          `${repoUrl(params.owner, params.repo)}/releases/${params.release_id}`,
        );
        return {
          id: r.id,
          tag_name: r.tag_name,
          name: r.name,
          body: r.body,
          draft: r.draft,
          prerelease: r.prerelease,
          html_url: r.html_url,
          published_at: r.published_at,
          assets: (r.assets ?? []).map((a: any) => ({
            name: a.name,
            download_url: a.browser_download_url,
            size: a.size,
          })),
        };
      },
    },

    create_release: {
      description: "Create a new release with a tag.",
      params: z.object({
        ...ownerRepo,
        tag_name: z.string().describe("Tag name (e.g. v1.0.0)"),
        name: z.string().optional().describe("Release title"),
        body: z.string().optional().describe("Release notes (markdown)"),
        draft: z.boolean().default(false),
        prerelease: z.boolean().default(false),
        target_commitish: z
          .string()
          .optional()
          .describe("Branch or commit SHA for the tag (defaults to default branch)"),
      }),
      returns: z.object({
        id: z.number(),
        tag_name: z.string(),
        html_url: z.string(),
      }),
      execute: async (params, ctx) => {
        const r = await ghPost(
          ctx,
          `${repoUrl(params.owner, params.repo)}/releases`,
          {
            tag_name: params.tag_name,
            name: params.name,
            body: params.body,
            draft: params.draft,
            prerelease: params.prerelease,
            target_commitish: params.target_commitish,
          },
        );
        return { id: r.id, tag_name: r.tag_name, html_url: r.html_url };
      },
    },

    // ── Workflows / Actions ────────────────────────────────────────────

    list_workflows: {
      description: "List GitHub Actions workflows in a repository.",
      params: z.object({ ...ownerRepo }),
      returns: z.array(
        z.object({
          id: z.number(),
          name: z.string(),
          path: z.string(),
          state: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${repoUrl(params.owner, params.repo)}/actions/workflows`,
        );
        return (data.workflows ?? []).map((w: any) => ({
          id: w.id,
          name: w.name,
          path: w.path,
          state: w.state,
        }));
      },
    },

    list_workflow_runs: {
      description: "List recent runs for a workflow.",
      params: z.object({
        ...ownerRepo,
        workflow_id: z
          .union([z.number(), z.string()])
          .describe("Workflow ID or filename (e.g. ci.yml)"),
        status: z
          .enum([
            "completed",
            "action_required",
            "cancelled",
            "failure",
            "neutral",
            "skipped",
            "stale",
            "success",
            "timed_out",
            "in_progress",
            "queued",
            "requested",
            "waiting",
            "pending",
          ])
          .optional()
          .describe("Filter by status"),
        per_page: z.number().min(1).max(100).default(10),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          name: z.string(),
          status: z.string().nullable(),
          conclusion: z.string().nullable(),
          head_branch: z.string().nullable(),
          html_url: z.string(),
          created_at: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        let url = `${repoUrl(params.owner, params.repo)}/actions/workflows/${enc(String(params.workflow_id))}/runs?per_page=${params.per_page}`;
        if (params.status) url += `&status=${params.status}`;
        const data = await ghFetch(ctx, url);
        return (data.workflow_runs ?? []).map((r: any) => ({
          id: r.id,
          name: r.name,
          status: r.status,
          conclusion: r.conclusion,
          head_branch: r.head_branch,
          html_url: r.html_url,
          created_at: r.created_at,
        }));
      },
    },

    get_workflow_run: {
      description: "Get details of a specific workflow run.",
      params: z.object({
        ...ownerRepo,
        run_id: z.number().describe("Workflow run ID"),
      }),
      returns: z.object({
        id: z.number(),
        name: z.string(),
        status: z.string().nullable(),
        conclusion: z.string().nullable(),
        head_branch: z.string().nullable(),
        head_sha: z.string(),
        html_url: z.string(),
        created_at: z.string(),
        updated_at: z.string(),
        run_attempt: z.number(),
      }),
      execute: async (params, ctx) => {
        const r = await ghFetch(
          ctx,
          `${repoUrl(params.owner, params.repo)}/actions/runs/${params.run_id}`,
        );
        return {
          id: r.id,
          name: r.name,
          status: r.status,
          conclusion: r.conclusion,
          head_branch: r.head_branch,
          head_sha: r.head_sha,
          html_url: r.html_url,
          created_at: r.created_at,
          updated_at: r.updated_at,
          run_attempt: r.run_attempt,
        };
      },
    },

    trigger_workflow: {
      description:
        "Trigger a workflow_dispatch event to run a workflow manually.",
      params: z.object({
        ...ownerRepo,
        workflow_id: z
          .union([z.number(), z.string()])
          .describe("Workflow ID or filename (e.g. deploy.yml)"),
        ref: z.string().describe("Branch or tag to run the workflow on"),
        inputs: z
          .record(z.string())
          .optional()
          .describe("Workflow input parameters"),
      }),
      returns: z.object({ triggered: z.boolean() }),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(
          `${repoUrl(params.owner, params.repo)}/actions/workflows/${enc(String(params.workflow_id))}/dispatches`,
          {
            method: "POST",
            headers: ghJsonHeaders(ctx.credentials.token),
            body: JSON.stringify({
              ref: params.ref,
              inputs: params.inputs,
            }),
          },
        );
        if (!res.ok) {
          const text = await res.text();
          throw new Error(`GitHub API ${res.status}: ${text}`);
        }
        // 204 No Content on success
        return { triggered: true };
      },
    },

    // ── Gists ──────────────────────────────────────────────────────────

    list_gists: {
      description: "List gists for the authenticated user.",
      params: z.object({
        per_page: z.number().min(1).max(100).default(10),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          description: z.string().nullable(),
          html_url: z.string(),
          public: z.boolean(),
          files: z.array(z.string()),
          created_at: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${GITHUB_API}/gists?per_page=${params.per_page}`,
        );
        return data.map((g: any) => ({
          id: g.id,
          description: g.description,
          html_url: g.html_url,
          public: g.public,
          files: Object.keys(g.files ?? {}),
          created_at: g.created_at,
        }));
      },
    },

    get_gist: {
      description: "Get a gist by ID with file contents.",
      params: z.object({
        gist_id: z.string().describe("Gist ID"),
      }),
      returns: z.object({
        id: z.string(),
        description: z.string().nullable(),
        html_url: z.string(),
        files: z.array(
          z.object({
            filename: z.string(),
            language: z.string().nullable(),
            content: z.string(),
          }),
        ),
      }),
      execute: async (params, ctx) => {
        const g = await ghFetch(ctx, `${GITHUB_API}/gists/${params.gist_id}`);
        return {
          id: g.id,
          description: g.description,
          html_url: g.html_url,
          files: Object.values(g.files ?? {}).map((f: any) => ({
            filename: f.filename,
            language: f.language ?? null,
            content: f.content ?? "",
          })),
        };
      },
    },

    create_gist: {
      description: "Create a new gist.",
      params: z.object({
        description: z.string().optional().describe("Gist description"),
        public: z.boolean().default(false).describe("Whether the gist is public"),
        files: z
          .record(z.string())
          .describe("Map of filename to file content"),
      }),
      returns: z.object({
        id: z.string(),
        html_url: z.string(),
      }),
      execute: async (params, ctx) => {
        const filesPayload: Record<string, { content: string }> = {};
        for (const [name, content] of Object.entries(params.files)) {
          filesPayload[name] = { content };
        }
        const g = await ghPost(ctx, `${GITHUB_API}/gists`, {
          description: params.description,
          public: params.public,
          files: filesPayload,
        });
        return { id: g.id, html_url: g.html_url };
      },
    },

    // ── Search ─────────────────────────────────────────────────────────

    search_repos: {
      description: "Search for repositories on GitHub.",
      params: z.object({
        query: z.string().describe("Search query (GitHub search syntax)"),
        sort: z
          .enum(["stars", "forks", "help-wanted-issues", "updated", "best-match"])
          .default("best-match")
          .describe("Sort field"),
        per_page: z.number().min(1).max(100).default(10),
      }),
      returns: z.object({
        total_count: z.number(),
        items: z.array(
          z.object({
            full_name: z.string(),
            description: z.string().nullable(),
            html_url: z.string(),
            stargazers_count: z.number(),
            language: z.string().nullable(),
          }),
        ),
      }),
      execute: async (params, ctx) => {
        const sort = params.sort === "best-match" ? "" : `&sort=${params.sort}`;
        const data = await ghFetch(
          ctx,
          `${GITHUB_API}/search/repositories?q=${enc(params.query)}&per_page=${params.per_page}${sort}`,
        );
        return {
          total_count: data.total_count,
          items: (data.items ?? []).map((r: any) => ({
            full_name: r.full_name,
            description: r.description,
            html_url: r.html_url,
            stargazers_count: r.stargazers_count,
            language: r.language,
          })),
        };
      },
    },

    search_issues: {
      description: "Search for issues and pull requests across GitHub.",
      params: z.object({
        query: z
          .string()
          .describe(
            "Search query (GitHub search syntax, e.g. 'repo:owner/repo is:issue label:bug')",
          ),
        sort: z
          .enum(["comments", "reactions", "created", "updated", "best-match"])
          .default("best-match"),
        per_page: z.number().min(1).max(100).default(10),
      }),
      returns: z.object({
        total_count: z.number(),
        items: z.array(
          z.object({
            number: z.number(),
            title: z.string(),
            state: z.string(),
            html_url: z.string(),
            repository_url: z.string(),
            is_pr: z.boolean(),
          }),
        ),
      }),
      execute: async (params, ctx) => {
        const sort = params.sort === "best-match" ? "" : `&sort=${params.sort}`;
        const data = await ghFetch(
          ctx,
          `${GITHUB_API}/search/issues?q=${enc(params.query)}&per_page=${params.per_page}${sort}`,
        );
        return {
          total_count: data.total_count,
          items: (data.items ?? []).map((i: any) => ({
            number: i.number,
            title: i.title,
            state: i.state,
            html_url: i.html_url,
            repository_url: i.repository_url,
            is_pr: !!i.pull_request,
          })),
        };
      },
    },

    search_code: {
      description: "Search for code across GitHub repositories.",
      params: z.object({
        query: z
          .string()
          .describe(
            "Search query (GitHub code search syntax, e.g. 'defineSkill language:typescript')",
          ),
        per_page: z.number().min(1).max(100).default(10),
      }),
      returns: z.object({
        total_count: z.number(),
        items: z.array(
          z.object({
            name: z.string(),
            path: z.string(),
            html_url: z.string(),
            repository: z.string(),
          }),
        ),
      }),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${GITHUB_API}/search/code?q=${enc(params.query)}&per_page=${params.per_page}`,
        );
        return {
          total_count: data.total_count,
          items: (data.items ?? []).map((c: any) => ({
            name: c.name,
            path: c.path,
            html_url: c.html_url,
            repository: c.repository?.full_name ?? "",
          })),
        };
      },
    },

    // ── Organizations ──────────────────────────────────────────────────

    list_org_repos: {
      description: "List repositories for an organization.",
      params: z.object({
        org: z.string().describe("Organization name"),
        type: z
          .enum(["all", "public", "private", "forks", "sources", "member"])
          .default("all")
          .describe("Repository type filter"),
        per_page: z.number().min(1).max(100).default(30),
      }),
      returns: z.array(
        z.object({
          full_name: z.string(),
          private: z.boolean(),
          description: z.string().nullable(),
          html_url: z.string(),
          default_branch: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${GITHUB_API}/orgs/${enc(params.org)}/repos?type=${params.type}&per_page=${params.per_page}`,
        );
        return data.map((r: any) => ({
          full_name: r.full_name,
          private: r.private,
          description: r.description,
          html_url: r.html_url,
          default_branch: r.default_branch,
        }));
      },
    },

    list_org_members: {
      description: "List members of an organization.",
      params: z.object({
        org: z.string().describe("Organization name"),
        per_page: z.number().min(1).max(100).default(30),
      }),
      returns: z.array(
        z.object({
          login: z.string(),
          avatar_url: z.string(),
          html_url: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${GITHUB_API}/orgs/${enc(params.org)}/members?per_page=${params.per_page}`,
        );
        return data.map((m: any) => ({
          login: m.login,
          avatar_url: m.avatar_url,
          html_url: m.html_url,
        }));
      },
    },
  },
});
