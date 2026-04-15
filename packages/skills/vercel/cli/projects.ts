import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { VERCEL_API, vFetch, vPost, vDelete, vPatch, enc } from "../core/client.ts";

export const projects: Record<string, ActionDefinition> = {
  list_projects: {
    description: "List projects accessible to the authenticated user.",
    params: z.object({
      team_id: z.string().optional().describe("Team ID to scope the request"),
      limit: z.number().min(1).max(100).default(20).describe("Results per page"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        name: z.string(),
        framework: z.string().nullable(),
        updatedAt: z.number(),
      }),
    ),
    execute: async (params, ctx) => {
      const url = `${VERCEL_API}/v9/projects?limit=${params.limit}${params.team_id ? `&teamId=${enc(params.team_id)}` : ""}`;
      const data = await vFetch(ctx, url);
      return (data.projects ?? []).map((p: any) => ({
        id: p.id,
        name: p.name,
        framework: p.framework ?? null,
        updatedAt: p.updatedAt,
      }));
    },
  },

  get_project: {
    description: "Get detailed information about a single project.",
    params: z.object({
      project_id: z.string().describe("Project ID or name"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      framework: z.string().nullable(),
      nodeVersion: z.string().nullable(),
      link: z.any().nullable(),
      createdAt: z.number(),
      updatedAt: z.number(),
    }),
    execute: async (params, ctx) => {
      const r = await vFetch(ctx, `${VERCEL_API}/v9/projects/${enc(params.project_id)}`);
      return {
        id: r.id,
        name: r.name,
        framework: r.framework ?? null,
        nodeVersion: r.nodeVersion ?? null,
        link: r.link ?? null,
        createdAt: r.createdAt,
        updatedAt: r.updatedAt,
      };
    },
  },

  create_project: {
    description: "Create a new Vercel project.",
    params: z.object({
      name: z.string().describe("Project name"),
      framework: z.string().optional().describe("Framework preset (nextjs, vite, etc.)"),
      git_repository: z
        .object({
          type: z.string().describe("Git provider (github, gitlab, bitbucket)"),
          repo: z.string().describe("Repository slug (owner/repo)"),
        })
        .optional()
        .describe("Git repository to connect"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      framework: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const body: any = { name: params.name };
      if (params.framework) body.framework = params.framework;
      if (params.git_repository) body.gitRepository = params.git_repository;
      const r = await vPost(ctx, `${VERCEL_API}/v10/projects`, body);
      return { id: r.id, name: r.name, framework: r.framework ?? null };
    },
  },

  update_project: {
    description: "Update an existing project.",
    params: z.object({
      project_id: z.string().describe("Project ID or name"),
      name: z.string().optional().describe("New project name"),
      framework: z.string().optional().describe("New framework preset"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      framework: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const body: any = {};
      if (params.name) body.name = params.name;
      if (params.framework) body.framework = params.framework;
      const r = await vPatch(ctx, `${VERCEL_API}/v9/projects/${enc(params.project_id)}`, body);
      return { id: r.id, name: r.name, framework: r.framework ?? null };
    },
  },

  delete_project: {
    description: "Delete a project and all its deployments.",
    params: z.object({
      project_id: z.string().describe("Project ID or name"),
    }),
    returns: z.object({ deleted: z.boolean() }),
    execute: async (params, ctx) => {
      await vDelete(ctx, `${VERCEL_API}/v9/projects/${enc(params.project_id)}`);
      return { deleted: true };
    },
  },
};
