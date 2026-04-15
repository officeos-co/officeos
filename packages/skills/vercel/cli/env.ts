import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { VERCEL_API, vFetch, vPost, vDelete, vPatch, enc } from "../core/client.ts";

export const env: Record<string, ActionDefinition> = {
  list_env_vars: {
    description: "List environment variables for a project.",
    params: z.object({
      project_id: z.string().describe("Project ID or name"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        key: z.string(),
        value: z.string().nullable(),
        target: z.array(z.string()),
        type: z.string(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await vFetch(ctx, `${VERCEL_API}/v9/projects/${enc(params.project_id)}/env`);
      return (data.envs ?? []).map((e: any) => ({
        id: e.id,
        key: e.key,
        value: e.value ?? null,
        target: e.target ?? [],
        type: e.type,
      }));
    },
  },

  get_env_var: {
    description: "Get a single environment variable.",
    params: z.object({
      project_id: z.string().describe("Project ID or name"),
      env_id: z.string().describe("Environment variable ID"),
    }),
    returns: z.object({
      id: z.string(),
      key: z.string(),
      value: z.string().nullable(),
      target: z.array(z.string()),
      type: z.string(),
    }),
    execute: async (params, ctx) => {
      const e = await vFetch(
        ctx,
        `${VERCEL_API}/v9/projects/${enc(params.project_id)}/env/${enc(params.env_id)}`,
      );
      return {
        id: e.id,
        key: e.key,
        value: e.value ?? null,
        target: e.target ?? [],
        type: e.type,
      };
    },
  },

  create_env_var: {
    description: "Create an environment variable for a project.",
    params: z.object({
      project_id: z.string().describe("Project ID or name"),
      key: z.string().describe("Variable name"),
      value: z.string().describe("Variable value"),
      target: z
        .array(z.enum(["production", "preview", "development"]))
        .default(["production", "preview", "development"])
        .describe("Target environments"),
      type: z
        .enum(["plain", "encrypted", "secret", "sensitive"])
        .default("encrypted")
        .describe("Variable type"),
    }),
    returns: z.object({
      id: z.string(),
      key: z.string(),
      target: z.array(z.string()),
      type: z.string(),
    }),
    execute: async (params, ctx) => {
      const r = await vPost(
        ctx,
        `${VERCEL_API}/v10/projects/${enc(params.project_id)}/env`,
        { key: params.key, value: params.value, target: params.target, type: params.type },
      );
      return { id: r.id, key: r.key, target: r.target ?? [], type: r.type };
    },
  },

  update_env_var: {
    description: "Update an environment variable.",
    params: z.object({
      project_id: z.string().describe("Project ID or name"),
      env_id: z.string().describe("Environment variable ID"),
      value: z.string().optional().describe("New value"),
      target: z
        .array(z.enum(["production", "preview", "development"]))
        .optional()
        .describe("New target environments"),
      type: z
        .enum(["plain", "encrypted", "secret", "sensitive"])
        .optional()
        .describe("New type"),
    }),
    returns: z.object({
      id: z.string(),
      key: z.string(),
      target: z.array(z.string()),
      type: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: any = {};
      if (params.value !== undefined) body.value = params.value;
      if (params.target) body.target = params.target;
      if (params.type) body.type = params.type;
      const r = await vPatch(
        ctx,
        `${VERCEL_API}/v9/projects/${enc(params.project_id)}/env/${enc(params.env_id)}`,
        body,
      );
      return { id: r.id, key: r.key, target: r.target ?? [], type: r.type };
    },
  },

  delete_env_var: {
    description: "Delete an environment variable from a project.",
    params: z.object({
      project_id: z.string().describe("Project ID or name"),
      env_id: z.string().describe("Environment variable ID"),
    }),
    returns: z.object({ deleted: z.boolean() }),
    execute: async (params, ctx) => {
      await vDelete(
        ctx,
        `${VERCEL_API}/v9/projects/${enc(params.project_id)}/env/${enc(params.env_id)}`,
      );
      return { deleted: true };
    },
  },
};
