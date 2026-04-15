import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ddGet, ddPost, ddDelete, DEFAULT_SITE } from "../core/client.ts";

export const monitors: Record<string, ActionDefinition> = {
  list_monitors: {
    description: "List monitors in the organization.",
    params: z.object({
      tags: z.string().optional().describe("Comma-separated scope tags to filter by"),
      monitor_tags: z.string().optional().describe("Comma-separated monitor tags"),
      name: z.string().optional().describe("Filter by monitor name (substring)"),
    }),
    returns: z.array(z.object({
      id: z.number(),
      name: z.string(),
      type: z.string(),
      status: z.string().optional(),
      query: z.string(),
      tags: z.array(z.string()),
      created: z.string().optional(),
      modified: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const qp: Record<string, string> = {};
      if (params.tags) qp.tags = params.tags;
      if (params.monitor_tags) qp.monitor_tags = params.monitor_tags;
      if (params.name) qp.name = params.name;
      const data = await ddGet(ctx.fetch, api_key, app_key, site, "/api/v1/monitor", qp);
      return (data ?? []).map((m: any) => ({
        id: m.id,
        name: m.name,
        type: m.type,
        status: m.overall_state,
        query: m.query,
        tags: m.tags ?? [],
        created: m.created,
        modified: m.modified,
      }));
    },
  },

  get_monitor: {
    description: "Get details of a specific monitor.",
    params: z.object({
      monitor_id: z.number().int().describe("Monitor ID"),
    }),
    returns: z.object({
      id: z.number(),
      name: z.string(),
      type: z.string(),
      status: z.string().optional(),
      query: z.string(),
      message: z.string().optional(),
      tags: z.array(z.string()),
      options: z.record(z.any()).optional(),
      created: z.string().optional(),
      modified: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const m = await ddGet(ctx.fetch, api_key, app_key, site, `/api/v1/monitor/${params.monitor_id}`);
      return {
        id: m.id,
        name: m.name,
        type: m.type,
        status: m.overall_state,
        query: m.query,
        message: m.message,
        tags: m.tags ?? [],
        options: m.options,
        created: m.created,
        modified: m.modified,
      };
    },
  },

  create_monitor: {
    description: "Create a new monitor.",
    params: z.object({
      name: z.string().describe("Monitor name"),
      type: z.string().describe("Monitor type (metric alert, service check, event alert, etc.)"),
      query: z.string().describe("Monitor query"),
      message: z.string().optional().describe("Notification message"),
      tags: z.array(z.string()).optional().describe("Tags to apply"),
      options: z.record(z.any()).optional().describe("Monitor options object"),
    }),
    returns: z.object({ id: z.number(), name: z.string(), type: z.string(), status: z.string().optional() }),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const body: Record<string, any> = {
        name: params.name,
        type: params.type,
        query: params.query,
      };
      if (params.message) body.message = params.message;
      if (params.tags) body.tags = params.tags;
      if (params.options) body.options = params.options;
      const m = await ddPost(ctx.fetch, api_key, app_key, site, "/api/v1/monitor", body);
      return { id: m.id, name: m.name, type: m.type, status: m.overall_state };
    },
  },

  update_monitor: {
    description: "Update an existing monitor.",
    params: z.object({
      monitor_id: z.number().int().describe("Monitor ID"),
      name: z.string().optional().describe("Updated name"),
      query: z.string().optional().describe("Updated query"),
      message: z.string().optional().describe("Updated message"),
      tags: z.array(z.string()).optional().describe("Updated tags"),
      options: z.record(z.any()).optional().describe("Updated options"),
    }),
    returns: z.object({ id: z.number(), name: z.string(), status: z.string().optional() }),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const body: Record<string, any> = {};
      if (params.name) body.name = params.name;
      if (params.query) body.query = params.query;
      if (params.message) body.message = params.message;
      if (params.tags) body.tags = params.tags;
      if (params.options) body.options = params.options;
      const m = await ddPost(ctx.fetch, api_key, app_key, site, `/api/v1/monitor/${params.monitor_id}`, body, "PUT");
      return { id: m.id, name: m.name, status: m.overall_state };
    },
  },

  mute_monitor: {
    description: "Mute a monitor, optionally scoped and time-bounded.",
    params: z.object({
      monitor_id: z.number().int().describe("Monitor ID"),
      scope: z.string().optional().describe("Scope to mute (e.g. host:web-01)"),
      end: z.number().int().optional().describe("End of mute window (Unix epoch seconds)"),
    }),
    returns: z.object({ id: z.number(), name: z.string() }),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const body: Record<string, any> = {};
      if (params.scope) body.scope = params.scope;
      if (params.end) body.end = params.end;
      const m = await ddPost(ctx.fetch, api_key, app_key, site, `/api/v1/monitor/${params.monitor_id}/mute`, body);
      return { id: m.id, name: m.name };
    },
  },

  delete_monitor: {
    description: "Delete a monitor permanently.",
    params: z.object({
      monitor_id: z.number().int().describe("Monitor ID"),
    }),
    returns: z.object({ deleted_monitor_id: z.number() }),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const data = await ddDelete(ctx.fetch, api_key, app_key, site, `/api/v1/monitor/${params.monitor_id}`);
      return { deleted_monitor_id: data.deleted_monitor_id ?? params.monitor_id };
    },
  },
};
