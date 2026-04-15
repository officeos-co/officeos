import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ddGet, DEFAULT_SITE } from "../core/client.ts";

export const hosts: Record<string, ActionDefinition> = {
  list_hosts: {
    description: "List hosts reporting to Datadog.",
    params: z.object({
      filter: z.string().optional().describe("Filter string (e.g. env:prod)"),
      sort_field: z.string().optional().describe("Field to sort by"),
      sort_dir: z.enum(["asc", "desc"]).optional().describe("Sort direction"),
      start: z.number().int().default(0).describe("Pagination offset"),
      count: z.number().int().max(1000).default(100).describe("Max results"),
      include_muted_hosts_data: z.boolean().default(true).describe("Include muted hosts info"),
    }),
    returns: z.object({
      total_returned: z.number(),
      total_matching: z.number(),
      hosts: z.array(z.object({
        name: z.string(),
        id: z.number().optional(),
        aliases: z.array(z.string()).optional(),
        apps: z.array(z.string()).optional(),
        is_muted: z.boolean().optional(),
        up: z.boolean().optional(),
        last_reported_time: z.number().optional(),
      })),
    }),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const qp: Record<string, string> = {
        start: String(params.start),
        count: String(params.count),
        include_muted_hosts_data: String(params.include_muted_hosts_data),
      };
      if (params.filter) qp.filter = params.filter;
      if (params.sort_field) qp.sort_field = params.sort_field;
      if (params.sort_dir) qp.sort_dir = params.sort_dir;
      const data = await ddGet(ctx.fetch, api_key, app_key, site, "/api/v1/hosts", qp);
      return {
        total_returned: data.total_returned ?? 0,
        total_matching: data.total_matching ?? 0,
        hosts: (data.host_list ?? []).map((h: any) => ({
          name: h.name,
          id: h.id,
          aliases: h.aliases,
          apps: h.apps,
          is_muted: h.is_muted,
          up: h.up,
          last_reported_time: h.last_reported_time,
        })),
      };
    },
  },

  get_host_totals: {
    description: "Get the total number of active and up hosts.",
    params: z.object({}),
    returns: z.object({
      total_active: z.number(),
      total_up: z.number(),
    }),
    execute: async (_params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const data = await ddGet(ctx.fetch, api_key, app_key, site, "/api/v1/hosts/totals");
      return { total_active: data.total_active ?? 0, total_up: data.total_up ?? 0 };
    },
  },
};
