import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { promGet } from "../core/client.ts";

export const seriesLabels: Record<string, ActionDefinition> = {
  series: {
    description: "Find time series that match label selectors.",
    params: z.object({
      match: z.array(z.string()).describe("Series selectors (PromQL label matchers, e.g. up or {job='node'})"),
      start: z.string().optional().describe("Start time (RFC 3339 or Unix seconds)"),
      end: z.string().optional().describe("End time (RFC 3339 or Unix seconds)"),
    }),
    returns: z.array(z.record(z.string())),
    execute: async (params, ctx) => {
      const qp: Record<string, string | string[]> = { "match[]": params.match };
      if (params.start) qp.start = params.start;
      if (params.end) qp.end = params.end;
      return promGet(ctx.fetch, ctx.credentials.url, "/series", qp);
    },
  },

  labels: {
    description: "List all label names.",
    params: z.object({
      start: z.string().optional().describe("Start time for active series window"),
      end: z.string().optional().describe("End time for active series window"),
      match: z.array(z.string()).optional().describe("Restrict to series matching these selectors"),
    }),
    returns: z.array(z.string()),
    execute: async (params, ctx) => {
      const qp: Record<string, string | string[]> = {};
      if (params.start) qp.start = params.start;
      if (params.end) qp.end = params.end;
      if (params.match) qp["match[]"] = params.match;
      return promGet(ctx.fetch, ctx.credentials.url, "/labels", qp);
    },
  },

  label_values: {
    description: "List all values for a specific label name.",
    params: z.object({
      label: z.string().describe("Label name to get values for"),
      start: z.string().optional().describe("Start time"),
      end: z.string().optional().describe("End time"),
      match: z.array(z.string()).optional().describe("Restrict to matching series"),
    }),
    returns: z.array(z.string()),
    execute: async (params, ctx) => {
      const qp: Record<string, string | string[]> = {};
      if (params.start) qp.start = params.start;
      if (params.end) qp.end = params.end;
      if (params.match) qp["match[]"] = params.match;
      return promGet(ctx.fetch, ctx.credentials.url, `/label/${encodeURIComponent(params.label)}/values`, qp);
    },
  },
};
