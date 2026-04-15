import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { promGet } from "../core/client.ts";

const promResultSchema = z.object({
  resultType: z.string(),
  result: z.array(z.any()),
});

export const query: Record<string, ActionDefinition> = {
  query: {
    description: "Execute an instant PromQL query at a single point in time.",
    params: z.object({
      expr: z.string().describe("PromQL expression to evaluate"),
      time: z.string().optional().describe("Evaluation timestamp (RFC 3339 or Unix seconds, defaults to now)"),
      timeout: z.string().optional().describe("Evaluation timeout (e.g. 30s)"),
    }),
    returns: promResultSchema,
    execute: async (params, ctx) => {
      const qp: Record<string, string> = { query: params.expr };
      if (params.time) qp.time = params.time;
      if (params.timeout) qp.timeout = params.timeout;
      return promGet(ctx.fetch, ctx.credentials.url, "/query", qp);
    },
  },

  query_range: {
    description: "Execute a PromQL query over a time range.",
    params: z.object({
      expr: z.string().describe("PromQL expression to evaluate"),
      start: z.string().describe("Range start (RFC 3339 or Unix seconds)"),
      end: z.string().describe("Range end (RFC 3339 or Unix seconds)"),
      step: z.string().describe("Query resolution step (e.g. 60s, 5m, 1h)"),
      timeout: z.string().optional().describe("Evaluation timeout"),
    }),
    returns: promResultSchema,
    execute: async (params, ctx) => {
      const qp: Record<string, string> = {
        query: params.expr,
        start: params.start,
        end: params.end,
        step: params.step,
      };
      if (params.timeout) qp.timeout = params.timeout;
      return promGet(ctx.fetch, ctx.credentials.url, "/query_range", qp);
    },
  },
};
