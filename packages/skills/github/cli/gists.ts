import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ghFetch, ghPost, GITHUB_API } from "../core/client.ts";

export const gists: Record<string, ActionDefinition> = {
  list_gists: {
    description: "List gists for the authenticated user.",
    params: z.object({
      per_page: z.number().min(1).max(100).default(10).describe("Results per page"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Gist ID"),
      description: z.string().nullable().describe("Gist description"),
      html_url: z.string().describe("Gist URL"),
      public: z.boolean().describe("Whether the gist is public"),
      files: z.array(z.string()).describe("File names"),
      created_at: z.string().describe("Creation date"),
    })),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${GITHUB_API}/gists?per_page=${params.per_page}`);
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
      id: z.string().describe("Gist ID"),
      description: z.string().nullable().describe("Gist description"),
      html_url: z.string().describe("Gist URL"),
      files: z.array(z.object({
        filename: z.string().describe("File name"),
        language: z.string().nullable().describe("Programming language"),
        content: z.string().describe("File content"),
      })).describe("Gist files"),
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
      files: z.record(z.string()).describe("Map of filename to file content"),
    }),
    returns: z.object({
      id: z.string().describe("Gist ID"),
      html_url: z.string().describe("Gist URL"),
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
};
