import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { promGet } from "../core/client.ts";

export const targetsRules: Record<string, ActionDefinition> = {
  targets: {
    description: "List scrape targets and their health status.",
    params: z.object({
      state: z.enum(["active", "dropped", "any"]).default("any").describe("Filter targets by state"),
    }),
    returns: z.object({
      activeTargets: z.array(z.object({
        scrapePool: z.string(),
        scrapeUrl: z.string(),
        globalUrl: z.string().optional(),
        lastError: z.string(),
        lastScrape: z.string(),
        lastScrapeDuration: z.number(),
        health: z.string(),
        labels: z.record(z.string()),
      })),
      droppedTargets: z.array(z.any()),
    }),
    execute: async (params, ctx) => {
      const qp: Record<string, string> = {};
      if (params.state !== "any") qp.state = params.state;
      return promGet(ctx.fetch, ctx.credentials.url, "/targets", qp);
    },
  },

  rules: {
    description: "List all loaded alerting and recording rules.",
    params: z.object({
      type: z.enum(["alert", "record"]).optional().describe("Filter by rule type (all if omitted)"),
    }),
    returns: z.object({
      groups: z.array(z.object({
        name: z.string(),
        file: z.string(),
        interval: z.number(),
        rules: z.array(z.object({
          name: z.string(),
          query: z.string(),
          type: z.string(),
          health: z.string(),
          lastEvaluation: z.string().optional(),
          evaluationTime: z.number().optional(),
        })),
      })),
    }),
    execute: async (params, ctx) => {
      const qp: Record<string, string> = {};
      if (params.type) qp.type = params.type;
      return promGet(ctx.fetch, ctx.credentials.url, "/rules", qp);
    },
  },

  alerts: {
    description: "List all currently firing alerts.",
    params: z.object({}),
    returns: z.object({
      alerts: z.array(z.object({
        labels: z.record(z.string()),
        annotations: z.record(z.string()),
        state: z.string(),
        activeAt: z.string(),
        value: z.string(),
      })),
    }),
    execute: async (_params, ctx) => {
      return promGet(ctx.fetch, ctx.credentials.url, "/alerts");
    },
  },
};
