import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ddGet, ddPost, ddDelete, DEFAULT_SITE } from "../core/client.ts";

export const downtimes: Record<string, ActionDefinition> = {
  list_downtimes: {
    description: "List scheduled downtimes.",
    params: z.object({
      current_only: z.boolean().optional().describe("Return only currently active downtimes"),
    }),
    returns: z.array(z.object({
      id: z.number(),
      scope: z.string(),
      message: z.string().optional(),
      start: z.number().optional(),
      end: z.number().optional(),
      monitor_id: z.number().optional(),
      active: z.boolean().optional(),
      disabled: z.boolean().optional(),
    })),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const qp: Record<string, string> = {};
      if (params.current_only !== undefined) qp.current_only = String(params.current_only);
      const data = await ddGet(ctx.fetch, api_key, app_key, site, "/api/v1/downtime", qp);
      return (data ?? []).map((d: any) => ({
        id: d.id,
        scope: Array.isArray(d.scope) ? d.scope.join(",") : d.scope,
        message: d.message,
        start: d.start,
        end: d.end,
        monitor_id: d.monitor_id,
        active: d.active,
        disabled: d.disabled,
      }));
    },
  },

  create_downtime: {
    description: "Schedule a downtime to silence monitors.",
    params: z.object({
      scope: z.string().describe("Scope to silence (e.g. host:web-01, * for all)"),
      start: z.number().int().optional().describe("Start time (Unix epoch, defaults to now)"),
      end: z.number().int().optional().describe("End time (Unix epoch, omit for indefinite)"),
      message: z.string().optional().describe("Downtime message/reason"),
      monitor_id: z.number().int().optional().describe("Silence a specific monitor ID only"),
      monitor_tags: z.array(z.string()).optional().describe("Silence monitors matching these tags"),
      recurrence: z.record(z.any()).optional().describe("Recurrence spec object"),
    }),
    returns: z.object({
      id: z.number(),
      scope: z.string(),
      start: z.number().optional(),
      end: z.number().optional(),
      active: z.boolean().optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const body: Record<string, any> = { scope: params.scope };
      if (params.start) body.start = params.start;
      if (params.end) body.end = params.end;
      if (params.message) body.message = params.message;
      if (params.monitor_id) body.monitor_id = params.monitor_id;
      if (params.monitor_tags) body.monitor_tags = params.monitor_tags;
      if (params.recurrence) body.recurrence = params.recurrence;
      const d = await ddPost(ctx.fetch, api_key, app_key, site, "/api/v1/downtime", body);
      return {
        id: d.id,
        scope: Array.isArray(d.scope) ? d.scope.join(",") : d.scope,
        start: d.start,
        end: d.end,
        active: d.active,
      };
    },
  },

  cancel_downtime: {
    description: "Cancel a scheduled downtime.",
    params: z.object({
      downtime_id: z.number().int().describe("Downtime ID to cancel"),
    }),
    returns: z.record(z.any()),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      return ddDelete(ctx.fetch, api_key, app_key, site, `/api/v1/downtime/${params.downtime_id}`);
    },
  },
};
