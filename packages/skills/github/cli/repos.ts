import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ghFetch, ghPost, enc, repoUrl, GITHUB_API } from "../core/client.ts";

const ownerRepo = {
  owner: z.string().describe("Repository owner"),
  repo: z.string().describe("Repository name"),
};

export const repos: Record<string, ActionDefinition> = {
  list_repos: {
    description: "List repositories accessible to the authenticated user.",
    params: z.object({
      visibility: z.enum(["all", "public", "private"]).default("all").describe("Filter by visibility"),
      per_page: z.number().min(1).max(100).default(30).describe("Results per page"),
    }),
    returns: z.array(z.object({
      full_name: z.string().describe("Full repository name"),
      private: z.boolean().describe("Whether the repo is private"),
      description: z.string().nullable().describe("Repository description"),
      html_url: z.string().describe("Repository URL"),
      default_branch: z.string().describe("Default branch name"),
    })),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${GITHUB_API}/user/repos?visibility=${params.visibility}&per_page=${params.per_page}`);
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
      full_name: z.string().describe("Full repository name"),
      private: z.boolean().describe("Whether the repo is private"),
      description: z.string().nullable().describe("Repository description"),
      html_url: z.string().describe("Repository URL"),
      default_branch: z.string().describe("Default branch"),
      language: z.string().nullable().describe("Primary language"),
      stargazers_count: z.number().describe("Star count"),
      forks_count: z.number().describe("Fork count"),
      open_issues_count: z.number().describe("Open issues count"),
      topics: z.array(z.string()).describe("Repository topics"),
      created_at: z.string().describe("Creation date"),
      updated_at: z.string().describe("Last update date"),
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
      full_name: z.string().describe("Full repository name"),
      html_url: z.string().describe("Repository URL"),
      clone_url: z.string().describe("Clone URL"),
      default_branch: z.string().describe("Default branch"),
    }),
    execute: async (params, ctx) => {
      const r = await ghPost(ctx, `${GITHUB_API}/user/repos`, {
        name: params.name,
        description: params.description,
        private: params.private,
        auto_init: params.auto_init,
      });
      return { full_name: r.full_name, html_url: r.html_url, clone_url: r.clone_url, default_branch: r.default_branch };
    },
  },

  clone_repo: {
    description: "Get the clone URL for a repository. The agent cannot clone directly but can use the URL.",
    params: z.object({ ...ownerRepo }),
    returns: z.object({
      clone_url: z.string().describe("HTTPS clone URL"),
      ssh_url: z.string().describe("SSH clone URL"),
      html_url: z.string().describe("Repository URL"),
    }),
    execute: async (params, ctx) => {
      const r = await ghFetch(ctx, repoUrl(params.owner, params.repo));
      return { clone_url: r.clone_url, ssh_url: r.ssh_url, html_url: r.html_url };
    },
  },

  list_repo_topics: {
    description: "List topics for a repository.",
    params: z.object({ ...ownerRepo }),
    returns: z.array(z.string().describe("Topic name")),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${repoUrl(params.owner, params.repo)}/topics`);
      return data.names ?? [];
    },
  },

  set_repo_topics: {
    description: "Replace all topics for a repository.",
    params: z.object({
      ...ownerRepo,
      topics: z.array(z.string()).describe("Array of topic names"),
    }),
    returns: z.array(z.string().describe("Topic name")),
    execute: async (params, ctx) => {
      const data = await ghPost(ctx, `${repoUrl(params.owner, params.repo)}/topics`, { names: params.topics }, "PUT");
      return data.names ?? [];
    },
  },
};
