import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ghFetch, ghPost, repoUrl } from "../core/client.ts";

const ownerRepo = {
  owner: z.string().describe("Repository owner"),
  repo: z.string().describe("Repository name"),
};

export const reviews: Record<string, ActionDefinition> = {
  list_pr_comments: {
    description: "List review comments on a pull request.",
    params: z.object({
      ...ownerRepo,
      pr_number: z.number().describe("PR number"),
      per_page: z.number().min(1).max(100).default(30).describe("Results per page"),
    }),
    returns: z.array(z.object({
      id: z.number().describe("Comment ID"),
      author: z.string().nullable().describe("Comment author"),
      body: z.string().describe("Comment body"),
      path: z.string().nullable().describe("File path"),
      created_at: z.string().describe("Creation date"),
    })),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}/comments?per_page=${params.per_page}`);
      return data.map((c: any) => ({
        id: c.id, author: c.user?.login ?? null, body: c.body,
        path: c.path ?? null, created_at: c.created_at,
      }));
    },
  },

  add_pr_comment: {
    description: "Add a general comment to a pull request (uses the issue comment endpoint).",
    params: z.object({
      ...ownerRepo,
      pr_number: z.number().describe("PR number"),
      body: z.string().describe("Comment body (markdown)"),
    }),
    returns: z.object({
      id: z.number().describe("Comment ID"),
      html_url: z.string().describe("Comment URL"),
    }),
    execute: async (params, ctx) => {
      const c = await ghPost(ctx, `${repoUrl(params.owner, params.repo)}/issues/${params.pr_number}/comments`, { body: params.body });
      return { id: c.id, html_url: c.html_url };
    },
  },

  list_pr_reviews: {
    description: "List reviews on a pull request.",
    params: z.object({
      ...ownerRepo,
      pr_number: z.number().describe("PR number"),
    }),
    returns: z.array(z.object({
      id: z.number().describe("Review ID"),
      author: z.string().nullable().describe("Reviewer login"),
      state: z.string().describe("Review state"),
      body: z.string().nullable().describe("Review body"),
      submitted_at: z.string().nullable().describe("Submission date"),
    })),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}/reviews`);
      return data.map((r: any) => ({
        id: r.id, author: r.user?.login ?? null, state: r.state,
        body: r.body ?? null, submitted_at: r.submitted_at ?? null,
      }));
    },
  },

  request_pr_review: {
    description: "Request reviews on a pull request.",
    params: z.object({
      ...ownerRepo,
      pr_number: z.number().describe("PR number"),
      reviewers: z.array(z.string()).optional().describe("Individual GitHub usernames"),
      team_reviewers: z.array(z.string()).optional().describe("Team slugs"),
    }),
    returns: z.object({
      requested_reviewers: z.array(z.string()).describe("Requested reviewer logins"),
      requested_teams: z.array(z.string()).describe("Requested team slugs"),
    }),
    execute: async (params, ctx) => {
      const payload: any = {};
      if (params.reviewers) payload.reviewers = params.reviewers;
      if (params.team_reviewers) payload.team_reviewers = params.team_reviewers;
      const r = await ghPost(ctx, `${repoUrl(params.owner, params.repo)}/pulls/${params.pr_number}/requested_reviewers`, payload);
      return {
        requested_reviewers: (r.requested_reviewers ?? []).map((u: any) => u.login),
        requested_teams: (r.requested_teams ?? []).map((t: any) => t.slug),
      };
    },
  },
};
