import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ghFetch, ghPost, repoUrl } from "../core/client.ts";

const ownerRepo = {
  owner: z.string().describe("Repository owner"),
  repo: z.string().describe("Repository name"),
};

export const releases: Record<string, ActionDefinition> = {
  list_releases: {
    description: "List releases for a repository.",
    params: z.object({
      ...ownerRepo,
      per_page: z.number().min(1).max(100).default(10).describe("Results per page"),
    }),
    returns: z.array(z.object({
      id: z.number().describe("Release ID"),
      tag_name: z.string().describe("Tag name"),
      name: z.string().nullable().describe("Release title"),
      draft: z.boolean().describe("Whether the release is a draft"),
      prerelease: z.boolean().describe("Whether the release is a prerelease"),
      html_url: z.string().describe("Release URL"),
      published_at: z.string().nullable().describe("Publication date"),
    })),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${repoUrl(params.owner, params.repo)}/releases?per_page=${params.per_page}`);
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
      id: z.number().describe("Release ID"),
      tag_name: z.string().describe("Tag name"),
      name: z.string().nullable().describe("Release title"),
      body: z.string().nullable().describe("Release notes"),
      draft: z.boolean().describe("Whether the release is a draft"),
      prerelease: z.boolean().describe("Whether the release is a prerelease"),
      html_url: z.string().describe("Release URL"),
      published_at: z.string().nullable().describe("Publication date"),
      assets: z.array(z.object({
        name: z.string().describe("Asset filename"),
        download_url: z.string().describe("Download URL"),
        size: z.number().describe("Asset size in bytes"),
      })).describe("Release assets"),
    }),
    execute: async (params, ctx) => {
      const r = await ghFetch(ctx, `${repoUrl(params.owner, params.repo)}/releases/${params.release_id}`);
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
      draft: z.boolean().default(false).describe("Create as draft"),
      prerelease: z.boolean().default(false).describe("Mark as prerelease"),
      target_commitish: z.string().optional().describe("Branch or commit SHA for the tag (defaults to default branch)"),
    }),
    returns: z.object({
      id: z.number().describe("Release ID"),
      tag_name: z.string().describe("Tag name"),
      html_url: z.string().describe("Release URL"),
    }),
    execute: async (params, ctx) => {
      const r = await ghPost(ctx, `${repoUrl(params.owner, params.repo)}/releases`, {
        tag_name: params.tag_name,
        name: params.name,
        body: params.body,
        draft: params.draft,
        prerelease: params.prerelease,
        target_commitish: params.target_commitish,
      });
      return { id: r.id, tag_name: r.tag_name, html_url: r.html_url };
    },
  },
};
