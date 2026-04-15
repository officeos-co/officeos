import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { phGet, phPost, DEFAULT_HOST, projectBase } from "../core/client.ts";

export const featureFlags: Record<string, ActionDefinition> = {
  list_feature_flags: {
    description: "List all feature flags for the project.",
    params: z.object({}),
    returns: z.array(z.object({
      id: z.number(),
      key: z.string(),
      name: z.string().optional(),
      active: z.boolean(),
      rollout_percentage: z.number().optional(),
      created_at: z.string().optional(),
    })),
    execute: async (_params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const data = await phGet(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/feature_flags/`);
      return (data.results ?? []).map((f: any) => ({
        id: f.id,
        key: f.key,
        name: f.name,
        active: f.active,
        rollout_percentage: f.filters?.rollout_percentage,
        created_at: f.created_at,
      }));
    },
  },

  get_feature_flag: {
    description: "Get details of a specific feature flag.",
    params: z.object({
      flag_id: z.number().int().describe("Feature flag ID"),
    }),
    returns: z.object({
      id: z.number(),
      key: z.string(),
      name: z.string().optional(),
      active: z.boolean(),
      rollout_percentage: z.number().optional(),
      filters: z.any().optional(),
      ensure_experience_continuity: z.boolean().optional(),
      created_at: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const f = await phGet(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/feature_flags/${params.flag_id}/`);
      return {
        id: f.id,
        key: f.key,
        name: f.name,
        active: f.active,
        rollout_percentage: f.filters?.rollout_percentage,
        filters: f.filters,
        ensure_experience_continuity: f.ensure_experience_continuity,
        created_at: f.created_at,
      };
    },
  },

  create_feature_flag: {
    description: "Create a new feature flag.",
    params: z.object({
      key: z.string().describe("Unique flag key in snake_case"),
      name: z.string().optional().describe("Human-readable flag name"),
      active: z.boolean().default(true).describe("Whether the flag is enabled"),
      rollout_percentage: z.number().int().min(0).max(100).default(100).describe("Percentage of users to enable for"),
      filters: z.record(z.any()).optional().describe("Advanced filter groups object"),
    }),
    returns: z.object({ id: z.number(), key: z.string(), name: z.string().optional(), active: z.boolean(), rollout_percentage: z.number().optional() }),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const body: Record<string, any> = {
        key: params.key,
        active: params.active,
        filters: params.filters ?? { rollout_percentage: params.rollout_percentage, groups: [{ rollout_percentage: params.rollout_percentage }] },
      };
      if (params.name) body.name = params.name;
      const f = await phPost(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/feature_flags/`, body);
      return { id: f.id, key: f.key, name: f.name, active: f.active, rollout_percentage: f.filters?.rollout_percentage };
    },
  },

  update_feature_flag: {
    description: "Update an existing feature flag.",
    params: z.object({
      flag_id: z.number().int().describe("Feature flag ID"),
      name: z.string().optional().describe("Updated name"),
      active: z.boolean().optional().describe("Enable or disable"),
      rollout_percentage: z.number().int().min(0).max(100).optional().describe("Updated rollout percentage"),
      filters: z.record(z.any()).optional().describe("Updated filter groups"),
    }),
    returns: z.object({ id: z.number(), key: z.string(), active: z.boolean(), rollout_percentage: z.number().optional() }),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const body: Record<string, any> = {};
      if (params.name !== undefined) body.name = params.name;
      if (params.active !== undefined) body.active = params.active;
      if (params.filters) body.filters = params.filters;
      else if (params.rollout_percentage !== undefined) {
        body.filters = { rollout_percentage: params.rollout_percentage, groups: [{ rollout_percentage: params.rollout_percentage }] };
      }
      const f = await phPost(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/feature_flags/${params.flag_id}/`, body, "PATCH");
      return { id: f.id, key: f.key, active: f.active, rollout_percentage: f.filters?.rollout_percentage };
    },
  },
};
