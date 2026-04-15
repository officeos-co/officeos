import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { hsGet, hsPost, hsDelete } from "../core/client.ts";

export const tickets: Record<string, ActionDefinition> = {
  list_tickets: {
    description: "List CRM tickets.",
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
      const data = await hsGet(ctx, "/crm/v3/objects/tickets", q);
      return (data.results ?? []).map((t: any) => ({
        id: t.id,
        properties: t.properties ?? {},
        createdAt: t.createdAt,
        updatedAt: t.updatedAt,
      }));
    },
  },

  get_ticket: {
    description: "Get a single CRM ticket.",
    params: z.object({
      ticket_id: z.string().describe("Ticket ID"),
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
      const t = await hsGet(ctx, `/crm/v3/objects/tickets/${params.ticket_id}`, q);
      return {
        id: t.id,
        properties: t.properties ?? {},
        createdAt: t.createdAt,
        updatedAt: t.updatedAt,
        associations: t.associations ?? null,
      };
    },
  },

  create_ticket: {
    description: "Create a new CRM ticket.",
    params: z.object({
      properties: z.record(z.string()).describe("Ticket properties (subject required)"),
    }),
    returns: z.object({
      id: z.string(),
      properties: z.record(z.string().nullable()),
      createdAt: z.string(),
    }),
    execute: async (params, ctx) => {
      const t = await hsPost(ctx, "/crm/v3/objects/tickets", { properties: params.properties });
      return { id: t.id, properties: t.properties ?? {}, createdAt: t.createdAt };
    },
  },

  update_ticket: {
    description: "Update an existing CRM ticket.",
    params: z.object({
      ticket_id: z.string().describe("Ticket ID"),
      properties: z.record(z.string()).describe("Properties to update"),
    }),
    returns: z.object({
      id: z.string(),
      properties: z.record(z.string().nullable()),
      updatedAt: z.string(),
    }),
    execute: async (params, ctx) => {
      const t = await hsPost(ctx, `/crm/v3/objects/tickets/${params.ticket_id}`, { properties: params.properties }, "PATCH");
      return { id: t.id, properties: t.properties ?? {}, updatedAt: t.updatedAt };
    },
  },

  delete_ticket: {
    description: "Delete a CRM ticket (moved to recycling bin for 90 days).",
    params: z.object({
      ticket_id: z.string().describe("Ticket ID"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      await hsDelete(ctx, `/crm/v3/objects/tickets/${params.ticket_id}`);
      return { success: true };
    },
  },
};
