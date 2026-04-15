import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { phGet, phDelete, DEFAULT_HOST, projectBase } from "../core/client.ts";

export const eventsPersons: Record<string, ActionDefinition> = {
  list_events: {
    description: "List captured events for the project.",
    params: z.object({
      event: z.string().optional().describe("Filter by event name (e.g. $pageview, $signup)"),
      distinct_id: z.string().optional().describe("Filter by person distinct ID"),
      limit: z.number().int().min(1).max(1000).default(100).describe("Max results"),
      after: z.string().optional().describe("ISO 8601 lower bound timestamp"),
      before: z.string().optional().describe("ISO 8601 upper bound timestamp"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      event: z.string(),
      distinct_id: z.string(),
      properties: z.record(z.any()).optional(),
      timestamp: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const qp: Record<string, string> = { limit: String(params.limit) };
      if (params.event) qp.event = params.event;
      if (params.distinct_id) qp.distinct_id = params.distinct_id;
      if (params.after) qp.after = params.after;
      if (params.before) qp.before = params.before;
      const data = await phGet(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/events/`, qp);
      return (data.results ?? []).map((e: any) => ({
        id: e.id,
        event: e.event,
        distinct_id: e.distinct_id,
        properties: e.properties,
        timestamp: e.timestamp,
      }));
    },
  },

  get_event: {
    description: "Get a single event by ID.",
    params: z.object({
      event_id: z.string().describe("Event UUID"),
    }),
    returns: z.object({
      id: z.string(),
      event: z.string(),
      distinct_id: z.string(),
      properties: z.record(z.any()).optional(),
      timestamp: z.string().optional(),
      elements: z.array(z.any()).optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const e = await phGet(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/events/${params.event_id}/`);
      return {
        id: e.id,
        event: e.event,
        distinct_id: e.distinct_id,
        properties: e.properties,
        timestamp: e.timestamp,
        elements: e.elements,
      };
    },
  },

  list_persons: {
    description: "List persons (users) tracked in the project.",
    params: z.object({
      search: z.string().optional().describe("Search by email, name, or distinct_id"),
      limit: z.number().int().min(1).max(1000).default(100).describe("Max results"),
    }),
    returns: z.array(z.object({
      id: z.number(),
      uuid: z.string(),
      distinct_ids: z.array(z.string()),
      properties: z.record(z.any()).optional(),
      created_at: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const qp: Record<string, string> = { limit: String(params.limit) };
      if (params.search) qp.search = params.search;
      const data = await phGet(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/persons/`, qp);
      return (data.results ?? []).map((p: any) => ({
        id: p.id,
        uuid: p.uuid,
        distinct_ids: p.distinct_ids,
        properties: p.properties,
        created_at: p.created_at,
      }));
    },
  },

  get_person: {
    description: "Get a person by their PostHog internal ID.",
    params: z.object({
      person_id: z.number().int().describe("Person ID"),
    }),
    returns: z.object({
      id: z.number(),
      uuid: z.string(),
      distinct_ids: z.array(z.string()),
      properties: z.record(z.any()).optional(),
      created_at: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      const p = await phGet(ctx.fetch, api_key, host, `${projectBase(host, project_id)}/persons/${params.person_id}/`);
      return { id: p.id, uuid: p.uuid, distinct_ids: p.distinct_ids, properties: p.properties, created_at: p.created_at };
    },
  },

  delete_person: {
    description: "Delete a person and optionally all their events.",
    params: z.object({
      person_id: z.number().int().describe("Person ID"),
      delete_events: z.boolean().default(false).describe("Also delete all events for this person"),
    }),
    returns: z.record(z.any()),
    execute: async (params, ctx) => {
      const { api_key, project_id, host = DEFAULT_HOST } = ctx.credentials;
      return phDelete(
        ctx.fetch, api_key, host,
        `${projectBase(host, project_id)}/persons/${params.person_id}/`,
        params.delete_events ? { delete_events: true } : undefined,
      );
    },
  },
};
