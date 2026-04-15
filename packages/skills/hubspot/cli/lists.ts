import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { hsGet, hsPost } from "../core/client.ts";

export const lists: Record<string, ActionDefinition> = {
  list_contact_lists: {
    description: "List contact lists.",
    params: z.object({
      limit: z.number().default(20).describe("Results to return"),
      offset: z.number().default(0).describe("Pagination offset"),
    }),
    returns: z.array(
      z.object({
        listId: z.number(),
        name: z.string(),
        listType: z.string().describe("STATIC or DYNAMIC"),
        metaData: z.any().nullable(),
        createdAt: z.string().nullable(),
        updatedAt: z.string().nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await hsGet(ctx, "/contacts/v1/lists", {
        count: String(params.limit),
        offset: String(params.offset),
      });
      return (data.lists ?? []).map((l: any) => ({
        listId: l.listId,
        name: l.name,
        listType: l.listType,
        metaData: l.metaData ?? null,
        createdAt: l.createdAt ? String(l.createdAt) : null,
        updatedAt: l.updatedAt ? String(l.updatedAt) : null,
      }));
    },
  },

  get_list: {
    description: "Get a single contact list.",
    params: z.object({
      list_id: z.string().describe("List ID"),
    }),
    returns: z.object({
      listId: z.number(),
      name: z.string(),
      listType: z.string(),
      filters: z.any().nullable(),
      metaData: z.any().nullable(),
      createdAt: z.string().nullable(),
      updatedAt: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const l = await hsGet(ctx, `/contacts/v1/lists/${params.list_id}`);
      return {
        listId: l.listId,
        name: l.name,
        listType: l.listType,
        filters: l.filters ?? null,
        metaData: l.metaData ?? null,
        createdAt: l.createdAt ? String(l.createdAt) : null,
        updatedAt: l.updatedAt ? String(l.updatedAt) : null,
      };
    },
  },

  create_list: {
    description: "Create a new contact list.",
    params: z.object({
      name: z.string().describe("List name"),
      list_type: z.enum(["STATIC", "DYNAMIC"]).default("STATIC").describe("List type"),
      filters: z.any().optional().describe("Filter JSON (required for DYNAMIC)"),
    }),
    returns: z.object({
      listId: z.number(),
      name: z.string(),
      listType: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {
        name: params.name,
        dynamic: params.list_type === "DYNAMIC",
      };
      if (params.filters) body.filters = params.filters;
      const l = await hsPost(ctx, "/contacts/v1/lists", body);
      return { listId: l.listId, name: l.name, listType: l.listType };
    },
  },

  add_to_list: {
    description: "Add contacts to a static list.",
    params: z.object({
      list_id: z.string().describe("List ID (must be STATIC)"),
      contact_ids: z.array(z.string()).describe("Contact IDs to add"),
    }),
    returns: z.object({
      updated: z.number().describe("Number of contacts added"),
      discarded: z.number().describe("Number of contacts discarded"),
      invalidVids: z.array(z.number()).describe("Invalid contact IDs"),
    }),
    execute: async (params, ctx) => {
      const r = await hsPost(ctx, `/contacts/v1/lists/${params.list_id}/add`, {
        vids: params.contact_ids.map(Number),
      });
      return {
        updated: r.updated ?? 0,
        discarded: r.discarded ?? 0,
        invalidVids: r.invalidVids ?? [],
      };
    },
  },

  remove_from_list: {
    description: "Remove contacts from a static list.",
    params: z.object({
      list_id: z.string().describe("List ID (must be STATIC)"),
      contact_ids: z.array(z.string()).describe("Contact IDs to remove"),
    }),
    returns: z.object({ updated: z.number().describe("Number of contacts removed") }),
    execute: async (params, ctx) => {
      const r = await hsPost(ctx, `/contacts/v1/lists/${params.list_id}/remove`, {
        vids: params.contact_ids.map(Number),
      });
      return { updated: r.updated ?? 0 };
    },
  },

  list_marketing_emails: {
    description: "List marketing emails.",
    params: z.object({
      limit: z.number().default(20).describe("Results to return"),
      offset: z.number().default(0).describe("Pagination offset"),
    }),
    returns: z.array(
      z.object({
        id: z.number(),
        name: z.string(),
        subject: z.string().nullable(),
        state: z.string().describe("DRAFT, PUBLISHED, or SENT"),
        publishDate: z.string().nullable(),
        stats: z.any().nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await hsGet(ctx, "/marketing-emails/v1/emails", {
        limit: String(params.limit),
        offset: String(params.offset),
      });
      return (data.objects ?? []).map((e: any) => ({
        id: e.id,
        name: e.name,
        subject: e.subject ?? null,
        state: e.state,
        publishDate: e.publishDate ? String(e.publishDate) : null,
        stats: e.stats ?? null,
      }));
    },
  },

  get_email_stats: {
    description: "Get statistics for a marketing email.",
    params: z.object({
      email_id: z.string().describe("Marketing email ID"),
    }),
    returns: z.object({
      sent: z.number().nullable(),
      delivered: z.number().nullable(),
      opens: z.number().nullable(),
      clicks: z.number().nullable(),
      unsubscribes: z.number().nullable(),
      bounces: z.number().nullable(),
      open_rate: z.number().nullable(),
      click_rate: z.number().nullable(),
      click_through_rate: z.number().nullable(),
    }),
    execute: async (params, ctx) => {
      const e = await hsGet(ctx, `/marketing-emails/v1/emails/${params.email_id}`);
      const s = e.stats?.counters ?? {};
      const sent = s.sent ?? 0;
      const opens = s.open ?? 0;
      const clicks = s.click ?? 0;
      return {
        sent: s.sent ?? null,
        delivered: s.delivered ?? null,
        opens: opens ?? null,
        clicks: clicks ?? null,
        unsubscribes: s.unsubscribed ?? null,
        bounces: s.bounce ?? null,
        open_rate: sent > 0 ? opens / sent : null,
        click_rate: sent > 0 ? clicks / sent : null,
        click_through_rate: opens > 0 ? clicks / opens : null,
      };
    },
  },
};
