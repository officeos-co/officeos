import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sentryFetch, enc } from "../core/client.ts";

export const performance: Record<string, ActionDefinition> = {
  list_transactions: {
    description: "List transactions for a project using the Discover API.",
    params: z.object({
      project: z.string().describe("Project slug"),
      query: z.string().optional().describe("Search query"),
      per_page: z.number().min(1).max(100).default(25).describe("Results per page"),
      sort: z.string().optional().describe("Sort field (e.g. p50, count)"),
    }),
    returns: z.array(z.object({
      transaction: z.string().describe("Transaction name"),
      count: z.number().describe("Event count"),
      p50: z.number().describe("50th percentile duration (ms)"),
      p75: z.number().describe("75th percentile duration (ms)"),
      p95: z.number().describe("95th percentile duration (ms)"),
      failure_rate: z.number().describe("Failure rate"),
      apdex: z.number().describe("Apdex score"),
    })),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      const fields = ["transaction", "count()", "p50()", "p75()", "p95()", "failure_rate()", "apdex()"];
      let url = `/organizations/${enc(org)}/events/?field=${fields.join("&field=")}&project=${enc(params.project)}&per_page=${params.per_page}&dataset=metricsEnhanced`;
      if (params.query) url += `&query=${enc(params.query)}`;
      if (params.sort) url += `&sort=-${enc(params.sort)}`;
      const data = await sentryFetch(ctx, url);
      return (data.data ?? []).map((t: any) => ({
        transaction: t.transaction,
        count: t["count()"] ?? 0,
        p50: t["p50()"] ?? 0,
        p75: t["p75()"] ?? 0,
        p95: t["p95()"] ?? 0,
        failure_rate: t["failure_rate()"] ?? 0,
        apdex: t["apdex()"] ?? 0,
      }));
    },
  },

  get_transaction_summary: {
    description: "Get performance summary for a specific transaction.",
    params: z.object({
      project: z.string().describe("Project slug"),
      transaction: z.string().describe("Transaction name"),
      period: z.string().default("24h").describe("Time period (e.g. 1h, 7d)"),
    }),
    returns: z.object({
      transaction: z.string().describe("Transaction name"),
      count: z.number().describe("Event count"),
      p50: z.number().describe("50th percentile duration (ms)"),
      p75: z.number().describe("75th percentile duration (ms)"),
      p95: z.number().describe("95th percentile duration (ms)"),
      p99: z.number().describe("99th percentile duration (ms)"),
      failure_rate: z.number().describe("Failure rate"),
      apdex: z.number().describe("Apdex score"),
      throughput: z.number().describe("Events per minute"),
    }),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      const fields = ["transaction", "count()", "p50()", "p75()", "p95()", "p99()", "failure_rate()", "apdex()", "epm()"];
      const url = `/organizations/${enc(org)}/events/?field=${fields.join("&field=")}&project=${enc(params.project)}&query=transaction:${enc(params.transaction)}&statsPeriod=${enc(params.period)}&dataset=metricsEnhanced`;
      const data = await sentryFetch(ctx, url);
      const t = data.data?.[0] ?? {};
      return {
        transaction: t.transaction ?? params.transaction,
        count: t["count()"] ?? 0,
        p50: t["p50()"] ?? 0,
        p75: t["p75()"] ?? 0,
        p95: t["p95()"] ?? 0,
        p99: t["p99()"] ?? 0,
        failure_rate: t["failure_rate()"] ?? 0,
        apdex: t["apdex()"] ?? 0,
        throughput: t["epm()"] ?? 0,
      };
    },
  },
};
