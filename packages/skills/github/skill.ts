import { defineSkill, z } from "@eaos/skill-sdk";

const GITHUB_API = "https://api.github.com";

function ghHeaders(token: string) {
  return {
    Authorization: `Bearer ${token}`,
    Accept: "application/vnd.github+json",
    "X-GitHub-Api-Version": "2022-11-28",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

export default defineSkill({
  name: "github",
  description:
    "List repositories, issues, and pull requests via the GitHub REST API.",

  credentials: {
    token: z.string().describe("Personal Access Token (github_pat_... or ghp_...)"),
  },

  actions: {
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
        })
      ),
      execute: async (params, ctx) => {
        const url = `${GITHUB_API}/user/repos?visibility=${params.visibility}&per_page=${params.per_page}`;
        const res = await ctx.fetch(url, {
          headers: ghHeaders(ctx.credentials.token),
        });
        if (!res.ok)
          throw new Error(`GitHub API ${res.status}: ${await res.text()}`);
        const data = await res.json();
        return data.map((r: any) => ({
          full_name: r.full_name,
          private: r.private,
          description: r.description,
          html_url: r.html_url,
          default_branch: r.default_branch,
        }));
      },
    },

    list_issues: {
      description: "List open issues in a single repository.",
      params: z.object({
        owner: z.string().describe("Repository owner"),
        repo: z.string().describe("Repository name"),
        state: z
          .enum(["open", "closed", "all"])
          .default("open")
          .describe("Issue state filter"),
      }),
      returns: z.array(
        z.object({
          number: z.number(),
          title: z.string(),
          state: z.string(),
          author: z.string().nullable(),
          html_url: z.string(),
        })
      ),
      execute: async (params, ctx) => {
        const url = `${GITHUB_API}/repos/${encodeURIComponent(params.owner)}/${encodeURIComponent(params.repo)}/issues?state=${params.state}`;
        const res = await ctx.fetch(url, {
          headers: ghHeaders(ctx.credentials.token),
        });
        if (!res.ok)
          throw new Error(`GitHub API ${res.status}: ${await res.text()}`);
        const data = await res.json();
        return data
          .filter((i: any) => !i.pull_request)
          .map((i: any) => ({
            number: i.number,
            title: i.title,
            state: i.state,
            author: i.user?.login ?? null,
            html_url: i.html_url,
          }));
      },
    },

    list_prs: {
      description: "List pull requests in a single repository.",
      params: z.object({
        owner: z.string().describe("Repository owner"),
        repo: z.string().describe("Repository name"),
        state: z
          .enum(["open", "closed", "all"])
          .default("open")
          .describe("PR state filter"),
      }),
      returns: z.array(
        z.object({
          number: z.number(),
          title: z.string(),
          state: z.string(),
          author: z.string().nullable(),
          html_url: z.string(),
          draft: z.boolean(),
        })
      ),
      execute: async (params, ctx) => {
        const url = `${GITHUB_API}/repos/${encodeURIComponent(params.owner)}/${encodeURIComponent(params.repo)}/pulls?state=${params.state}`;
        const res = await ctx.fetch(url, {
          headers: ghHeaders(ctx.credentials.token),
        });
        if (!res.ok)
          throw new Error(`GitHub API ${res.status}: ${await res.text()}`);
        const data = await res.json();
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
  },
});
