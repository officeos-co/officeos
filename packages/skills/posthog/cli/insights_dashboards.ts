import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { phGet, phPost, DEFAULT_HOST, projectBase } from "../core/client.ts";

export const insightsDashboards: Record<string, ActionDefinition> = {
  list_insights: {
    description: "List saved insights for the project.",
    params: z.object({
      saved: z.boolean().optional().describe("Return only saved insights"),
      limit: z.number().int().max(300).default(100).describe("Max results"),
    }),
    returns: z.array(z.object({
      id: z.number(),
      name: z.string().optional(),
      filters: z.any().optional(),
      created_at: z.string().optional(),
      last_modified_at: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const qp: Record<string, string> = { limit: String(params.limit) };
      if (params.saved !== undefined) qp.saved = String(params.saved);
      const data = await phGet(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/insights/`, qp);
      return (data.results ?? []).map((i: any) => ({
        id: i.id,
        name: i.name,
        filters: i.filters,
        created_at: i.created_at,
        last_modified_at: i.last_modified_at,
      }));
    },
  },

  get_insight: {
    description: "Get a specific insight including its result data.",
    params: z.object({
      insight_id: z.number().int().describe("Insight ID"),
    }),
    returns: z.object({
      id: z.number(),
      name: z.string().optional(),
      description: z.string().optional(),
      filters: z.any().optional(),
      result: z.any().optional(),
      created_at: z.string().optional(),
      last_modified_at: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const i = await phGet(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/insights/${params.insight_id}/`);
      return {
        id: i.id,
        name: i.name,
        description: i.description,
        filters: i.filters,
        result: i.result,
        created_at: i.created_at,
        last_modified_at: i.last_modified_at,
      };
    },
  },

  create_insight: {
    description: "Create a new insight.",
    params: z.object({
      name: z.string().optional().describe("Insight name"),
      description: z.string().optional().describe("Insight description"),
      filters: z.record(z.any()).describe("PostHog filters object defining the insight query"),
    }),
    returns: z.object({ id: z.number(), name: z.string().optional(), filters: z.any(), created_at: z.string().optional() }),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const body: Record<string, any> = { filters: params.filters };
      if (params.name) body.name = params.name;
      if (params.description) body.description = params.description;
      const i = await phPost(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/insights/`, body);
      return { id: i.id, name: i.name, filters: i.filters, created_at: i.created_at };
    },
  },

  list_dashboards: {
    description: "List all dashboards for the project.",
    params: z.object({}),
    returns: z.array(z.object({
      id: z.number(),
      name: z.string(),
      description: z.string().optional(),
      pinned: z.boolean().optional(),
      created_at: z.string().optional(),
    })),
    execute: async (_params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const data = await phGet(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/dashboards/`);
      return (data.results ?? []).map((d: any) => ({
        id: d.id,
        name: d.name,
        description: d.description,
        pinned: d.pinned,
        created_at: d.created_at,
      }));
    },
  },

  get_dashboard: {
    description: "Get a specific dashboard including its insight tiles.",
    params: z.object({
      dashboard_id: z.number().int().describe("Dashboard ID"),
    }),
    returns: z.object({
      id: z.number(),
      name: z.string(),
      description: z.string().optional(),
      pinned: z.boolean().optional(),
      tiles: z.array(z.any()).optional(),
      created_at: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const d = await phGet(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/dashboards/${params.dashboard_id}/`);
      return {
        id: d.id,
        name: d.name,
        description: d.description,
        pinned: d.pinned,
        tiles: d.tiles,
        created_at: d.created_at,
      };
    },
  },
};
