import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ghFetch, enc, GITHUB_API } from "../core/client.ts";

export const search: Record<string, ActionDefinition> = {
  search_repos: {
    description: "Search for repositories on GitHub.",
    params: z.object({
      query: z.string().describe("Search query (GitHub search syntax)"),
      sort: z.enum(["stars", "forks", "help-wanted-issues", "updated", "best-match"]).default("best-match").describe("Sort field"),
      per_page: z.number().min(1).max(100).default(10).describe("Results per page"),
    }),
    returns: z.object({
      total_count: z.number().describe("Total matching repositories"),
      items: z.array(z.object({
        full_name: z.string().describe("Full repository name"),
        description: z.string().nullable().describe("Repository description"),
        html_url: z.string().describe("Repository URL"),
        stargazers_count: z.number().describe("Star count"),
        language: z.string().nullable().describe("Primary language"),
      })).describe("Matching repositories"),
    }),
    execute: async (params, ctx) => {
      const sort = params.sort === "best-match" ? "" : `&sort=${params.sort}`;
      const data = await ghFetch(ctx, `${GITHUB_API}/search/repositories?q=${enc(params.query)}&per_page=${params.per_page}${sort}`);
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
      query: z.string().describe("Search query (GitHub search syntax, e.g. 'repo:owner/repo is:issue label:bug')"),
      sort: z.enum(["comments", "reactions", "created", "updated", "best-match"]).default("best-match").describe("Sort field"),
      per_page: z.number().min(1).max(100).default(10).describe("Results per page"),
    }),
    returns: z.object({
      total_count: z.number().describe("Total matching issues"),
      items: z.array(z.object({
        number: z.number().describe("Issue/PR number"),
        title: z.string().describe("Issue/PR title"),
        state: z.string().describe("Issue/PR state"),
        html_url: z.string().describe("Issue/PR URL"),
        repository_url: z.string().describe("Repository API URL"),
        is_pr: z.boolean().describe("Whether this is a pull request"),
      })).describe("Matching issues and PRs"),
    }),
    execute: async (params, ctx) => {
      const sort = params.sort === "best-match" ? "" : `&sort=${params.sort}`;
      const data = await ghFetch(ctx, `${GITHUB_API}/search/issues?q=${enc(params.query)}&per_page=${params.per_page}${sort}`);
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
      query: z.string().describe("Search query (GitHub code search syntax, e.g. 'defineSkill language:typescript')"),
      per_page: z.number().min(1).max(100).default(10).describe("Results per page"),
    }),
    returns: z.object({
      total_count: z.number().describe("Total matching files"),
      items: z.array(z.object({
        name: z.string().describe("File name"),
        path: z.string().describe("File path in repository"),
        html_url: z.string().describe("File URL"),
        repository: z.string().describe("Repository full name"),
      })).describe("Matching code files"),
    }),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${GITHUB_API}/search/code?q=${enc(params.query)}&per_page=${params.per_page}`);
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
};
