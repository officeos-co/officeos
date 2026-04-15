import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ghFetch, ghPost, repoUrl } from "../core/client.ts";

const ownerRepo = {
  owner: z.string().describe("Repository owner"),
  repo: z.string().describe("Repository name"),
};

export const pulls: Record<string, ActionDefinition> = {
  list_prs: {
    description: "List pull requests in a repository.",
    params: z.object({
      ...ownerRepo,
      state: z.enum(["open", "closed", "all"]).default("open").describe("PR state filter"),
      per_page: z.number().min(1).max(100).default(30).describe("Results per page"),
    }),
    returns: z.array(z.object({
      number: z.number().describe("PR number"),
      title: z.string().describe("PR title"),
      state: z.string().describe("PR state"),
      author: z.string().nullable().describe("PR author login"),
      html_url: z.string().describe("PR URL"),
      draft: z.boolean().describe("Whether the PR is a draft"),
    })),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${repoUrl(params.owner, params.repo)}/pulls?state=${params.state}&per_page=${params.per_page}`);
      return data.map((pr: any) => ({
        number: pr.number, title: pr.title, state: pr.state,
        author: pr.user?.login ?? null, html_url: pr.html_url, draft: pr.draft,
      }));
    },
  },

  get_pr: {
    description: "Get detailed information about a pull request.",
    params: z.object({ ...ownerRepo, pr_number: z.number().describe("PR number") }),
    returns: z.object({
      number: z.number().describe("PR number"),
      title: z.string().describe("PR title"),
      state: z.string().describe("PR state"),
      author: z.string().nullable().describe("PR author"),
      body: z.string().nullable().describe("PR body"),
      draft: z.boolean().describe("Whether the PR is a draft"),
      merged: z.boolean().describe("Whether the PR is merged"),
      mergeable: z.boolean().nullable().describe("Whether the PR is mergeable"),
      head_ref: z.string().describe("Head branch"),
      base_ref: z.string().describe("Base branch"),
      html_url: z.string().describe("PR URL"),
      additions: z.number().describe("Lines added"),
      deletions: z.number().describe("Lines deleted"),
      changed_files: z.number().describe("Files changed"),
      created_at: z.string().describe("Creation date"),
      updated_at: z.string().describe("Last update date"),
    }),
    execute: async (params, ctx) => {
      const pr = await ghFetch(ctx, `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}`);
      return {
        number: pr.number, title: pr.title, state: pr.state,
        author: pr.user?.login ?? null, body: pr.body, draft: pr.draft,
        merged: pr.merged, mergeable: pr.mergeable,
        head_ref: pr.head?.ref, base_ref: pr.base?.ref,
        html_url: pr.html_url, additions: pr.additions,
        deletions: pr.deletions, changed_files: pr.changed_files,
        created_at: pr.created_at, updated_at: pr.updated_at,
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
      number: z.number().describe("PR number"),
      title: z.string().describe("PR title"),
      html_url: z.string().describe("PR URL"),
    }),
    execute: async (params, ctx) => {
      const pr = await ghPost(ctx, `${repoUrl(params.owner, params.repo)}/pulls`, {
        title: params.title, body: params.body, head: params.head, base: params.base, draft: params.draft,
      });
      return { number: pr.number, title: pr.title, html_url: pr.html_url };
    },
  },

  merge_pr: {
    description: "Merge a pull request.",
    params: z.object({
      ...ownerRepo,
      pr_number: z.number().describe("PR number"),
      merge_method: z.enum(["merge", "squash", "rebase"]).default("merge").describe("Merge strategy"),
      commit_title: z.string().optional().describe("Custom merge commit title"),
      commit_message: z.string().optional().describe("Custom merge commit message"),
    }),
    returns: z.object({
      merged: z.boolean().describe("Whether the merge succeeded"),
      message: z.string().describe("Result message"),
      sha: z.string().describe("Merge commit SHA"),
    }),
    execute: async (params, ctx) => {
      const payload: any = { merge_method: params.merge_method };
      if (params.commit_title) payload.commit_title = params.commit_title;
      if (params.commit_message) payload.commit_message = params.commit_message;
      const r = await ghPost(ctx, `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}/merge`, payload, "PUT");
      return { merged: r.merged, message: r.message, sha: r.sha };
    },
  },

  close_pr: {
    description: "Close a pull request without merging.",
    params: z.object({ ...ownerRepo, pr_number: z.number().describe("PR number") }),
    returns: z.object({ number: z.number().describe("PR number"), state: z.string().describe("New state") }),
    execute: async (params, ctx) => {
      const pr = await ghPost(ctx, `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}`, { state: "closed" }, "PATCH");
      return { number: pr.number, state: pr.state };
    },
  },

  reopen_pr: {
    description: "Reopen a closed pull request.",
    params: z.object({ ...ownerRepo, pr_number: z.number().describe("PR number") }),
    returns: z.object({ number: z.number().describe("PR number"), state: z.string().describe("New state") }),
    execute: async (params, ctx) => {
      const pr = await ghPost(ctx, `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}`, { state: "open" }, "PATCH");
      return { number: pr.number, state: pr.state };
    },
  },

  list_pr_files: {
    description: "List files changed in a pull request.",
    params: z.object({
      ...ownerRepo,
      pr_number: z.number().describe("PR number"),
      per_page: z.number().min(1).max(100).default(30).describe("Results per page"),
    }),
    returns: z.array(z.object({
      filename: z.string().describe("File path"),
      status: z.string().describe("Change status"),
      additions: z.number().describe("Lines added"),
      deletions: z.number().describe("Lines deleted"),
      changes: z.number().describe("Total changes"),
      patch: z.string().nullable().describe("Diff patch"),
    })),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}/files?per_page=${params.per_page}`);
      return data.map((f: any) => ({
        filename: f.filename, status: f.status, additions: f.additions,
        deletions: f.deletions, changes: f.changes, patch: f.patch ?? null,
      }));
    },
  },
};
