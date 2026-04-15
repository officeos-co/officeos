import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ghFetch, ghPost, repoUrl } from "../core/client.ts";

const ownerRepo = {
  owner: z.string().describe("Repository owner"),
  repo: z.string().describe("Repository name"),
};

export const issues: Record<string, ActionDefinition> = {
  list_issues: {
    description: "List issues in a repository (excludes pull requests).",
    params: z.object({
      ...ownerRepo,
      state: z.enum(["open", "closed", "all"]).default("open").describe("Issue state filter"),
      per_page: z.number().min(1).max(100).default(30).describe("Results per page"),
    }),
    returns: z.array(z.object({
      number: z.number().describe("Issue number"),
      title: z.string().describe("Issue title"),
      state: z.string().describe("Issue state"),
      author: z.string().nullable().describe("Issue author login"),
      labels: z.array(z.string()).describe("Label names"),
      html_url: z.string().describe("Issue URL"),
    })),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${repoUrl(params.owner, params.repo)}/issues?state=${params.state}&per_page=${params.per_page}`);
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
      number: z.number().describe("Issue number"),
      title: z.string().describe("Issue title"),
      state: z.string().describe("Issue state"),
      author: z.string().nullable().describe("Issue author"),
      body: z.string().nullable().describe("Issue body"),
      labels: z.array(z.string()).describe("Label names"),
      assignees: z.array(z.string()).describe("Assignee logins"),
      html_url: z.string().describe("Issue URL"),
      created_at: z.string().describe("Creation date"),
      updated_at: z.string().describe("Last update date"),
      comments_count: z.number().describe("Number of comments"),
    }),
    execute: async (params, ctx) => {
      const i = await ghFetch(ctx, `${repoUrl(params.owner, params.repo)}/issues/${params.issue_number}`);
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
      number: z.number().describe("Issue number"),
      title: z.string().describe("Issue title"),
      html_url: z.string().describe("Issue URL"),
    }),
    execute: async (params, ctx) => {
      const i = await ghPost(ctx, `${repoUrl(params.owner, params.repo)}/issues`, {
        title: params.title,
        body: params.body,
        labels: params.labels,
        assignees: params.assignees,
      });
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
      number: z.number().describe("Issue number"),
      title: z.string().describe("Issue title"),
      html_url: z.string().describe("Issue URL"),
    }),
    execute: async (params, ctx) => {
      const payload: any = {};
      if (params.title !== undefined) payload.title = params.title;
      if (params.body !== undefined) payload.body = params.body;
      if (params.labels !== undefined) payload.labels = params.labels;
      if (params.assignees !== undefined) payload.assignees = params.assignees;
      const i = await ghPost(ctx, `${repoUrl(params.owner, params.repo)}/issues/${params.issue_number}`, payload, "PATCH");
      return { number: i.number, title: i.title, html_url: i.html_url };
    },
  },

  close_issue: {
    description: "Close an issue.",
    params: z.object({
      ...ownerRepo,
      issue_number: z.number().describe("Issue number"),
    }),
    returns: z.object({ number: z.number().describe("Issue number"), state: z.string().describe("New state") }),
    execute: async (params, ctx) => {
      const i = await ghPost(ctx, `${repoUrl(params.owner, params.repo)}/issues/${params.issue_number}`, { state: "closed" }, "PATCH");
      return { number: i.number, state: i.state };
    },
  },

  reopen_issue: {
    description: "Reopen a closed issue.",
    params: z.object({
      ...ownerRepo,
      issue_number: z.number().describe("Issue number"),
    }),
    returns: z.object({ number: z.number().describe("Issue number"), state: z.string().describe("New state") }),
    execute: async (params, ctx) => {
      const i = await ghPost(ctx, `${repoUrl(params.owner, params.repo)}/issues/${params.issue_number}`, { state: "open" }, "PATCH");
      return { number: i.number, state: i.state };
    },
  },

  list_issue_comments: {
    description: "List comments on an issue.",
    params: z.object({
      ...ownerRepo,
      issue_number: z.number().describe("Issue number"),
      per_page: z.number().min(1).max(100).default(30).describe("Results per page"),
    }),
    returns: z.array(z.object({
      id: z.number().describe("Comment ID"),
      author: z.string().nullable().describe("Comment author login"),
      body: z.string().describe("Comment body"),
      created_at: z.string().describe("Creation date"),
    })),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${repoUrl(params.owner, params.repo)}/issues/${params.issue_number}/comments?per_page=${params.per_page}`);
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
      id: z.number().describe("Comment ID"),
      html_url: z.string().describe("Comment URL"),
    }),
    execute: async (params, ctx) => {
      const c = await ghPost(ctx, `${repoUrl(params.owner, params.repo)}/issues/${params.issue_number}/comments`, { body: params.body });
      return { id: c.id, html_url: c.html_url };
    },
  },
};
