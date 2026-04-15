import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { phGet, phPost, DEFAULT_HOST, projectBase } from "../core/client.ts";

export const cohortsAnnotations: Record<string, ActionDefinition> = {
  list_cohorts: {
    description: "List all cohorts for the project.",
    params: z.object({}),
    returns: z.array(z.object({
      id: z.number(),
      name: z.string(),
      description: z.string().optional(),
      count: z.number().optional(),
      is_calculating: z.boolean().optional(),
      created_at: z.string().optional(),
    })),
    execute: async (_params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const data = await phGet(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/cohorts/`);
      return (data.results ?? []).map((c: any) => ({
        id: c.id,
        name: c.name,
        description: c.description,
        count: c.count,
        is_calculating: c.is_calculating,
        created_at: c.created_at,
      }));
    },
  },

  get_cohort: {
    description: "Get a specific cohort by ID.",
    params: z.object({
      cohort_id: z.number().int().describe("Cohort ID"),
    }),
    returns: z.object({
      id: z.number(),
      name: z.string(),
      description: z.string().optional(),
      count: z.number().optional(),
      filters: z.any().optional(),
      created_at: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const c = await phGet(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/cohorts/${params.cohort_id}/`);
      return { id: c.id, name: c.name, description: c.description, count: c.count, filters: c.filters, created_at: c.created_at };
    },
  },

  create_cohort: {
    description: "Create a new cohort for user segmentation.",
    params: z.object({
      name: z.string().describe("Cohort name"),
      description: z.string().optional().describe("Cohort description"),
      filters: z.record(z.any()).optional().describe("Filter conditions defining cohort membership"),
    }),
    returns: z.object({ id: z.number(), name: z.string(), is_calculating: z.boolean().optional() }),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const body: Record<string, any> = { name: params.name };
      if (params.description) body.description = params.description;
      if (params.filters) body.filters = params.filters;
      const c = await phPost(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/cohorts/`, body);
      return { id: c.id, name: c.name, is_calculating: c.is_calculating };
    },
  },

  list_annotations: {
    description: "List annotations for the project.",
    params: z.object({
      limit: z.number().int().max(300).default(100).describe("Max results"),
    }),
    returns: z.array(z.object({
      id: z.number(),
      content: z.string(),
      date_marker: z.string().optional(),
      scope: z.string().optional(),
      created_at: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const data = await phGet(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/annotations/`, { limit: String(params.limit) });
      return (data.results ?? []).map((a: any) => ({
        id: a.id,
        content: a.content,
        date_marker: a.date_marker,
        scope: a.scope,
        created_at: a.created_at,
      }));
    },
  },

  create_annotation: {
    description: "Create an annotation to mark a point in time on graphs.",
    params: z.object({
      content: z.string().describe("Annotation text"),
      date_marker: z.string().optional().describe("ISO 8601 timestamp for the marker (defaults to now)"),
      scope: z.enum(["project", "organization"]).default("project").describe("Annotation scope"),
      dashboard_id: z.number().int().optional().describe("Associate with a specific dashboard"),
    }),
    returns: z.object({ id: z.number(), content: z.string(), date_marker: z.string().optional(), scope: z.string().optional(), created_at: z.string().optional() }),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const body: Record<string, any> = { content: params.content, scope: params.scope };
      if (params.date_marker) body.date_marker = params.date_marker;
      if (params.dashboard_id) body.dashboard_id = params.dashboard_id;
      const a = await phPost(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/annotations/`, body);
      return { id: a.id, content: a.content, date_marker: a.date_marker, scope: a.scope, created_at: a.created_at };
    },
  },
};
