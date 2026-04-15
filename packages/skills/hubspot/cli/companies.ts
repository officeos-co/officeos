import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { hsGet, hsPost, hsDelete } from "../core/client.ts";

export const companies: Record<string, ActionDefinition> = {
  list_companies: {
    description: "List CRM companies.",
    params: z.object({
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
      after: z.string().optional().describe("Pagination cursor"),
      properties: z.array(z.string()).optional().describe("Properties to include"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        properties: z.record(z.string().nullable()),
        createdAt: z.string(),
        updatedAt: z.string(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { limit: String(params.limit) };
      if (params.after) q.after = params.after;
      if (params.properties) q.properties = params.properties.join(",");
      const data = await hsGet(ctx, "/crm/v3/objects/companies", q);
      return (data.results ?? []).map((c: any) => ({
        id: c.id,
        properties: c.properties ?? {},
        createdAt: c.createdAt,
        updatedAt: c.updatedAt,
      }));
    },
  },

  get_company: {
    description: "Get a single CRM company.",
    params: z.object({
      company_id: z.string().describe("Company ID"),
      properties: z.array(z.string()).optional().describe("Properties to include"),
    }),
    returns: z.object({
      id: z.string(),
      properties: z.record(z.string().nullable()),
      createdAt: z.string(),
      updatedAt: z.string(),
      associations: z.any().nullable(),
    }),
    execute: async (params, ctx) => {
      const q: Record<string, string> = {};
      if (params.properties) q.properties = params.properties.join(",");
      const c = await hsGet(ctx, `/crm/v3/objects/companies/${params.company_id}`, q);
      return {
        id: c.id,
        properties: c.properties ?? {},
        createdAt: c.createdAt,
        updatedAt: c.updatedAt,
        associations: c.associations ?? null,
      };
    },
  },

  create_company: {
    description: "Create a new CRM company.",
    params: z.object({
      properties: z.record(z.string()).describe("Company properties (name required)"),
    }),
    returns: z.object({
      id: z.string(),
      properties: z.record(z.string().nullable()),
      createdAt: z.string(),
    }),
    execute: async (params, ctx) => {
      const c = await hsPost(ctx, "/crm/v3/objects/companies", { properties: params.properties });
      return { id: c.id, properties: c.properties ?? {}, createdAt: c.createdAt };
    },
  },

  update_company: {
    description: "Update an existing CRM company.",
    params: z.object({
      company_id: z.string().describe("Company ID"),
      properties: z.record(z.string()).describe("Properties to update"),
    }),
    returns: z.object({
      id: z.string(),
      properties: z.record(z.string().nullable()),
      updatedAt: z.string(),
    }),
    execute: async (params, ctx) => {
      const c = await hsPost(ctx, `/crm/v3/objects/companies/${params.company_id}`, { properties: params.properties }, "PATCH");
      return { id: c.id, properties: c.properties ?? {}, updatedAt: c.updatedAt };
    },
  },

  delete_company: {
    description: "Delete a CRM company (moved to recycling bin for 90 days).",
    params: z.object({
      company_id: z.string().describe("Company ID"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      await hsDelete(ctx, `/crm/v3/objects/companies/${params.company_id}`);
      return { success: true };
    },
  },

  search_companies: {
    description: "Search companies with free-text query or filter groups.",
    params: z.object({
      query: z.string().optional().describe("Free-text search query"),
      filter_groups: z.array(z.any()).optional().describe("HubSpot filter groups JSON"),
      sorts: z.array(z.any()).optional().describe("Sort criteria JSON"),
      properties: z.array(z.string()).optional().describe("Properties to return"),
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
    }),
    returns: z.array(z.any()).describe("List of matching company objects"),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = { limit: params.limit };
      if (params.query) body.query = params.query;
      if (params.filter_groups) body.filterGroups = params.filter_groups;
      if (params.sorts) body.sorts = params.sorts;
      if (params.properties) body.properties = params.properties;
      const data = await hsPost(ctx, "/crm/v3/objects/companies/search", body);
      return data.results ?? [];
    },
  },
};
