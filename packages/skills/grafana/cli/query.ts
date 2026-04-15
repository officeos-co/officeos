import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gfPost } from "../core/client.ts";

export const query: Record<string, ActionDefinition> = {
  query_datasource: {
    description: "Execute a query against a datasource.",
    params: z.object({
      datasource_uid: z.string().describe("Datasource UID"),
      expr: z.string().describe("Query expression (PromQL, SQL, etc)"),
      from: z.string().default("now-1h").describe("Start time"),
      to: z.string().default("now").describe("End time"),
      interval: z.string().optional().describe("Step interval (e.g. 15s, 1m)"),
    }),
    returns: z.object({ results: z.any() }),
    execute: async (params, ctx) => {
      const q: any = { refId: "A", datasource: { uid: params.datasource_uid }, expr: params.expr };
      if (params.interval) q.interval = params.interval;
      const data = await gfPost(ctx, "/ds/query", { queries: [q], from: params.from, to: params.to });
      return { results: data.results };
    },
  },
};
