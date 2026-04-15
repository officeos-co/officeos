import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sentryFetch, enc } from "../core/client.ts";

const eventShape = {
  event_id: z.string().describe("Event ID"),
  title: z.string().describe("Event title"),
  message: z.string().nullable().describe("Event message"),
  level: z.string().describe("Event level"),
  platform: z.string().nullable().describe("Platform"),
  timestamp: z.string().describe("Event timestamp"),
  exception: z.any().nullable().describe("Exception data with stacktrace"),
  breadcrumbs: z.any().nullable().describe("Breadcrumb trail"),
  contexts: z.any().nullable().describe("Context data"),
  tags: z.array(z.any()).describe("Event tags"),
  user: z.any().nullable().describe("User context"),
  request: z.any().nullable().describe("HTTP request context"),
};

function mapEvent(e: any) {
  return {
    event_id: e.eventID,
    title: e.title,
    message: e.message ?? null,
    level: e.level ?? "error",
    platform: e.platform ?? null,
    timestamp: e.dateCreated,
    exception: e.entries?.find((en: any) => en.type === "exception")?.data ?? null,
    breadcrumbs: e.entries?.find((en: any) => en.type === "breadcrumbs")?.data ?? null,
    contexts: e.contexts ?? null,
    tags: e.tags ?? [],
    user: e.user ?? null,
    request: e.entries?.find((en: any) => en.type === "request")?.data ?? null,
  };
}

export const events: Record<string, ActionDefinition> = {
  list_events: {
    description: "List events for a project.",
    params: z.object({
      project: z.string().describe("Project slug"),
      per_page: z.number().min(1).max(100).default(25).describe("Results per page"),
      cursor: z.string().optional().describe("Pagination cursor"),
    }),
    returns: z.array(z.object({
      event_id: z.string().describe("Event ID"),
      title: z.string().describe("Event title"),
      message: z.string().nullable().describe("Event message"),
      level: z.string().describe("Event level"),
      platform: z.string().nullable().describe("Platform"),
      timestamp: z.string().describe("Event timestamp"),
      tags: z.array(z.any()).describe("Event tags"),
    })),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      let url = `/projects/${enc(org)}/${enc(params.project)}/events/?limit=${params.per_page}`;
      if (params.cursor) url += `&cursor=${enc(params.cursor)}`;
      const data = await sentryFetch(ctx, url);
      return data.map((e: any) => ({
        event_id: e.eventID,
        title: e.title,
        message: e.message ?? null,
        level: e.level ?? "error",
        platform: e.platform ?? null,
        timestamp: e.dateCreated,
        tags: e.tags ?? [],
      }));
    },
  },

  get_event: {
    description: "Get full detail for a specific event.",
    params: z.object({
      project: z.string().describe("Project slug"),
      event_id: z.string().describe("Event ID"),
    }),
    returns: z.object(eventShape),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      const e = await sentryFetch(ctx, `/projects/${enc(org)}/${enc(params.project)}/events/${enc(params.event_id)}/`);
      return mapEvent(e);
    },
  },

  get_latest_event: {
    description: "Get the most recent event for an issue.",
    params: z.object({
      issue_id: z.string().describe("Issue ID"),
    }),
    returns: z.object(eventShape),
    execute: async (params, ctx) => {
      const e = await sentryFetch(ctx, `/issues/${enc(params.issue_id)}/events/latest/`);
      return mapEvent(e);
    },
  },
};
