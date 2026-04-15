import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ghFetch, enc, GITHUB_API } from "../core/client.ts";

export const orgs: Record<string, ActionDefinition> = {
  list_org_repos: {
    description: "List repositories for an organization.",
    params: z.object({
      org: z.string().describe("Organization name"),
      type: z.enum(["all", "public", "private", "forks", "sources", "member"]).default("all").describe("Repository type filter"),
      per_page: z.number().min(1).max(100).default(30).describe("Results per page"),
    }),
    returns: z.array(z.object({
      full_name: z.string().describe("Full repository name"),
      private: z.boolean().describe("Whether the repo is private"),
      description: z.string().nullable().describe("Repository description"),
      html_url: z.string().describe("Repository URL"),
      default_branch: z.string().describe("Default branch"),
    })),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${GITHUB_API}/orgs/${enc(params.org)}/repos?type=${params.type}&per_page=${params.per_page}`);
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
      per_page: z.number().min(1).max(100).default(30).describe("Results per page"),
    }),
    returns: z.array(z.object({
      login: z.string().describe("Member login"),
      avatar_url: z.string().describe("Avatar URL"),
      html_url: z.string().describe("Profile URL"),
    })),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${GITHUB_API}/orgs/${enc(params.org)}/members?per_page=${params.per_page}`);
      return data.map((m: any) => ({
        login: m.login,
        avatar_url: m.avatar_url,
        html_url: m.html_url,
      }));
    },
  },
};
