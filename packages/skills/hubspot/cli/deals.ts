import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { hsGet, hsPost, hsDelete } from "../core/client.ts";

export const deals: Record<string, ActionDefinition> = {
  list_deals: {
    description: "List CRM deals.",
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
      const data = await hsGet(ctx, "/crm/v3/objects/deals", q);
      return (data.results ?? []).map((d: any) => ({
        id: d.id,
        properties: d.properties ?? {},
        createdAt: d.createdAt,
        updatedAt: d.updatedAt,
      }));
    },
  },

  get_deal: {
    description: "Get a single CRM deal.",
    params: z.object({
      deal_id: z.string().describe("Deal ID"),
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
      const d = await hsGet(ctx, `/crm/v3/objects/deals/${params.deal_id}`, q);
      return {
        id: d.id,
        properties: d.properties ?? {},
        createdAt: d.createdAt,
        updatedAt: d.updatedAt,
        associations: d.associations ?? null,
      };
    },
  },

  create_deal: {
    description: "Create a new CRM deal.",
    params: z.object({
      dealname: z.string().describe("Deal name"),
      pipeline: z.string().optional().describe("Pipeline ID (default: default)"),
      dealstage: z.string().describe("Stage ID within the pipeline"),
      amount: z.string().optional().describe("Deal amount"),
      closedate: z.string().optional().describe("Expected close date (YYYY-MM-DD)"),
      properties: z.record(z.string()).optional().describe("Additional properties"),
    }),
    returns: z.object({
      id: z.string(),
      properties: z.record(z.string().nullable()),
      createdAt: z.string(),
    }),
    execute: async (params, ctx) => {
      const props: Record<string, string> = {
        dealname: params.dealname,
        dealstage: params.dealstage,
      };
      if (params.pipeline) props.pipeline = params.pipeline;
      if (params.amount) props.amount = params.amount;
      if (params.closedate) props.closedate = params.closedate;
      if (params.properties) Object.assign(props, params.properties);
      const d = await hsPost(ctx, "/crm/v3/objects/deals", { properties: props });
      return { id: d.id, properties: d.properties ?? {}, createdAt: d.createdAt };
    },
  },

  update_deal: {
    description: "Update an existing CRM deal.",
    params: z.object({
      deal_id: z.string().describe("Deal ID"),
      properties: z.record(z.string()).describe("Properties to update"),
    }),
    returns: z.object({
      id: z.string(),
      properties: z.record(z.string().nullable()),
      updatedAt: z.string(),
    }),
    execute: async (params, ctx) => {
      const d = await hsPost(ctx, `/crm/v3/objects/deals/${params.deal_id}`, { properties: params.properties }, "PATCH");
      return { id: d.id, properties: d.properties ?? {}, updatedAt: d.updatedAt };
    },
  },

  delete_deal: {
    description: "Delete a CRM deal (moved to recycling bin for 90 days).",
    params: z.object({
      deal_id: z.string().describe("Deal ID"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      await hsDelete(ctx, `/crm/v3/objects/deals/${params.deal_id}`);
      return { success: true };
    },
  },

  search_deals: {
    description: "Search deals with free-text query or filter groups.",
    params: z.object({
      query: z.string().optional().describe("Free-text search query"),
      filter_groups: z.array(z.any()).optional().describe("HubSpot filter groups JSON"),
      sorts: z.array(z.any()).optional().describe("Sort criteria JSON"),
      properties: z.array(z.string()).optional().describe("Properties to return"),
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
    }),
    returns: z.array(z.any()).describe("List of matching deal objects"),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = { limit: params.limit };
      if (params.query) body.query = params.query;
      if (params.filter_groups) body.filterGroups = params.filter_groups;
      if (params.sorts) body.sorts = params.sorts;
      if (params.properties) body.properties = params.properties;
      const data = await hsPost(ctx, "/crm/v3/objects/deals/search", body);
      return data.results ?? [];
    },
  },
};
