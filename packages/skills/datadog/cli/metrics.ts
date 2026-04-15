import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ddGet, ddPost, DEFAULT_SITE } from "../core/client.ts";

export const metrics: Record<string, ActionDefinition> = {
  query_metrics: {
    description: "Query timeseries metric data over a given time window.",
    params: z.object({
      query: z.string().describe("Datadog metrics query string (e.g. avg:system.cpu.user{host:web-01})"),
      from: z.number().int().describe("Start of timeframe (Unix epoch seconds)"),
      to: z.number().int().describe("End of timeframe (Unix epoch seconds)"),
    }),
    returns: z.object({
      status: z.string(),
      series: z.array(z.object({
        metric: z.string(),
        displayName: z.string().optional(),
        pointlist: z.array(z.tuple([z.number(), z.number()])),
        scope: z.string().optional(),
      })),
    }),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const data = await ddGet(ctx.fetch, api_key, app_key, site, "/api/v1/query", {
        query: params.query,
        from: String(params.from),
        to: String(params.to),
      });
      return {
        status: data.status,
        series: (data.series ?? []).map((s: any) => ({
          metric: s.metric,
          displayName: s.display_name,
          pointlist: s.pointlist,
          scope: s.scope,
        })),
      };
    },
  },

  submit_metrics: {
    description: "Submit custom metrics to Datadog.",
    params: z.object({
      series: z.array(z.object({
        metric: z.string().describe("Metric name"),
        points: z.array(z.tuple([z.number(), z.number()])).describe("Array of [timestamp, value] pairs"),
        tags: z.array(z.string()).optional().describe("Tags to apply"),
        host: z.string().optional().describe("Host name"),
        type: z.enum(["gauge", "rate", "count"]).optional().describe("Metric type"),
      })).describe("Array of metric series to submit"),
    }),
    returns: z.object({ status: z.string() }),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const data = await ddPost(ctx.fetch, api_key, app_key, site, "/api/v2/series", {
        series: params.series.map(s => ({
          metric: s.metric,
          points: s.points.map(([ts, val]) => ({ timestamp: ts, value: val })),
          tags: s.tags,
          resources: s.host ? [{ name: s.host, type: "host" }] : undefined,
          type: s.type === "count" ? 1 : s.type === "rate" ? 2 : 0,
        })),
      });
      return { status: data.errors?.length ? "error" : "ok" };
    },
  },

  list_metrics: {
    description: "List active metrics from the last hour.",
    params: z.object({
      from: z.number().int().describe("Seconds since POSIX epoch (start of active window)"),
      host: z.string().optional().describe("Filter metrics reported from this host"),
      tag_filter: z.string().optional().describe("Filter by tag family (e.g. env)"),
    }),
    returns: z.object({ metrics: z.array(z.string()) }),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const qp: Record<string, string> = { from: String(params.from) };
      if (params.host) qp.host = params.host;
      if (params.tag_filter) qp.tag_filter = params.tag_filter;
      const data = await ddGet(ctx.fetch, api_key, app_key, site, "/api/v1/metrics", qp);
      return { metrics: data.metrics ?? [] };
    },
  },
};
