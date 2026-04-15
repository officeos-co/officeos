import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ddGet, ddPost, DEFAULT_SITE } from "../core/client.ts";

export const events: Record<string, ActionDefinition> = {
  list_events: {
    description: "List events from the Datadog event stream.",
    params: z.object({
      start: z.number().int().describe("Start of timeframe (Unix epoch seconds)"),
      end: z.number().int().describe("End of timeframe (Unix epoch seconds)"),
      priority: z.enum(["normal", "low"]).optional().describe("Filter by priority"),
      sources: z.string().optional().describe("Comma-separated event sources"),
      tags: z.string().optional().describe("Comma-separated tags"),
    }),
    returns: z.array(z.object({
      id: z.number().optional(),
      title: z.string(),
      text: z.string().optional(),
      date_happened: z.number().optional(),
      host: z.string().optional(),
      tags: z.array(z.string()).optional(),
      alert_type: z.string().optional(),
      source: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const qp: Record<string, string> = {
        start: String(params.start),
        end: String(params.end),
      };
      if (params.priority) qp.priority = params.priority;
      if (params.sources) qp.sources = params.sources;
      if (params.tags) qp.tags = params.tags;
      const data = await ddGet(ctx.fetch, api_key, app_key, site, "/api/v1/events", qp);
      return (data.events ?? []).map((e: any) => ({
        id: e.id,
        title: e.title,
        text: e.text,
        date_happened: e.date_happened,
        host: e.host,
        tags: e.tags,
        alert_type: e.alert_type,
        source: e.source,
      }));
    },
  },

  create_event: {
    description: "Post an event to the Datadog event stream.",
    params: z.object({
      title: z.string().describe("Event title"),
      text: z.string().describe("Event body text"),
      date_happened: z.number().int().optional().describe("POSIX timestamp (defaults to now)"),
      host: z.string().optional().describe("Host name to associate with event"),
      tags: z.array(z.string()).optional().describe("Tags to apply"),
      alert_type: z.enum(["error", "warning", "info", "success"]).default("info").describe("Alert type"),
      priority: z.enum(["normal", "low"]).default("normal").describe("Event priority"),
      source_type_name: z.string().optional().describe("Source type name"),
    }),
    returns: z.object({
      status: z.string(),
      event_id: z.number().optional(),
      event_url: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const body: Record<string, any> = {
        title: params.title,
        text: params.text,
        alert_type: params.alert_type,
        priority: params.priority,
      };
      if (params.date_happened) body.date_happened = params.date_happened;
      if (params.host) body.host = params.host;
      if (params.tags) body.tags = params.tags;
      if (params.source_type_name) body.source_type_name = params.source_type_name;
      const data = await ddPost(ctx.fetch, api_key, app_key, site, "/api/v1/events", body);
      return {
        status: data.status,
        event_id: data.event?.id,
        event_url: data.event?.url,
      };
    },
  },
};
