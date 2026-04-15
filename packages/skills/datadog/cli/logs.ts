import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ddPost, DEFAULT_SITE } from "../core/client.ts";

export const logs: Record<string, ActionDefinition> = {
  search_logs: {
    description: "Search log events with a Datadog query.",
    params: z.object({
      query: z.string().describe("Datadog log search query (e.g. service:api status:error)"),
      from: z.string().describe("Start time (ISO 8601, e.g. 2024-01-15T00:00:00Z)"),
      to: z.string().describe("End time (ISO 8601)"),
      limit: z.number().int().min(1).max(1000).default(25).describe("Max results"),
      sort: z.enum(["timestamp", "-timestamp"]).default("-timestamp").describe("Sort order"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      attributes: z.object({
        timestamp: z.string().optional(),
        host: z.string().optional(),
        service: z.string().optional(),
        status: z.string().optional(),
        message: z.string().optional(),
        tags: z.array(z.string()).optional(),
      }),
    })),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const body = {
        filter: {
          query: params.query,
          from: params.from,
          to: params.to,
        },
        sort: params.sort,
        page: { limit: params.limit },
      };
      const data = await ddPost(ctx.fetch, api_key, app_key, site, "/api/v2/logs/events/search", body);
      return (data.data ?? []).map((e: any) => ({
        id: e.id,
        attributes: {
          timestamp: e.attributes?.timestamp,
          host: e.attributes?.host,
          service: e.attributes?.service,
          status: e.attributes?.status,
          message: e.attributes?.message,
          tags: e.attributes?.tags,
        },
      }));
    },
  },

  aggregate_logs: {
    description: "Compute aggregations on log events.",
    params: z.object({
      query: z.string().describe("Filter query"),
      from: z.string().describe("Start time (ISO 8601)"),
      to: z.string().describe("End time (ISO 8601)"),
      group_by: z.array(z.string()).optional().describe("Fields to group results by"),
      compute: z.array(z.object({
        aggregation: z.string().describe("Aggregation type (count, sum, avg, min, max, pc90, pc95, pc99)"),
        type: z.string().optional().describe("total or timeseries"),
        metric: z.string().optional().describe("Metric to aggregate (for sum/avg/min/max)"),
      })).optional().describe("Compute specs"),
    }),
    returns: z.object({
      buckets: z.array(z.any()),
    }),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const body: Record<string, any> = {
        filter: { query: params.query, from: params.from, to: params.to },
      };
      if (params.group_by) {
        body.group_by = params.group_by.map(f => ({ facet: f }));
      }
      if (params.compute) {
        body.compute = params.compute;
      }
      const data = await ddPost(ctx.fetch, api_key, app_key, site, "/api/v2/logs/analytics/aggregate", body);
      return { buckets: data.data?.buckets ?? [] };
    },
  },
};
