import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { promGet } from "../core/client.ts";

export const configMetadata: Record<string, ActionDefinition> = {
  config: {
    description: "Get the current Prometheus configuration as YAML.",
    params: z.object({}),
    returns: z.object({ yaml: z.string() }),
    execute: async (_params, ctx) => {
      return promGet(ctx.fetch, ctx.credentials.url, "/status/config");
    },
  },

  flags: {
    description: "Get the runtime flag values Prometheus was started with.",
    params: z.object({}),
    returns: z.record(z.string()),
    execute: async (_params, ctx) => {
      return promGet(ctx.fetch, ctx.credentials.url, "/status/flags");
    },
  },

  metadata: {
    description: "Get metric metadata (type, help text, unit).",
    params: z.object({
      metric: z.string().optional().describe("Metric name to filter by (omit for all metrics)"),
      limit: z.number().int().optional().describe("Max number of metrics to return"),
    }),
    returns: z.record(z.array(z.object({
      type: z.string(),
      help: z.string(),
      unit: z.string(),
    }))),
    execute: async (params, ctx) => {
      const qp: Record<string, string> = {};
      if (params.metric) qp.metric = params.metric;
      if (params.limit) qp.limit = String(params.limit);
      return promGet(ctx.fetch, ctx.credentials.url, "/metadata", qp);
    },
  },

  tsdb_stats: {
    description: "Get TSDB (time series database) statistics.",
    params: z.object({}),
    returns: z.object({
      headStats: z.object({
        numSeries: z.number(),
        chunkCount: z.number(),
        minTime: z.number(),
        maxTime: z.number(),
      }).optional(),
      seriesCountByMetricName: z.array(z.any()).optional(),
      labelValueCountByLabelName: z.array(z.any()).optional(),
      memoryInBytesByLabelName: z.array(z.any()).optional(),
      seriesCountByLabelValuePair: z.array(z.any()).optional(),
    }),
    execute: async (_params, ctx) => {
      return promGet(ctx.fetch, ctx.credentials.url, "/status/tsdb");
    },
  },
};
