import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sfFetch, enc } from "../core/client.ts";

export const query: Record<string, ActionDefinition> = {
  query: {
    description: "Execute a SOQL query to retrieve records.",
    params: z.object({
      soql: z.string().describe("SOQL query string"),
    }),
    returns: z.object({
      totalSize: z.number().describe("Total matching records"),
      done: z.boolean().describe("Whether all results are returned"),
      records: z.array(z.record(z.unknown())).describe("Matching records"),
    }),
    execute: async (params, ctx) => {
      const data = await sfFetch(ctx, `/query?q=${enc(params.soql)}`);
      return { totalSize: data.totalSize, done: data.done, records: data.records };
    },
  },

  query_all: {
    description: "Execute a SOQL query including deleted and archived records.",
    params: z.object({
      soql: z.string().describe("SOQL query (includes deleted/archived records)"),
    }),
    returns: z.object({
      totalSize: z.number().describe("Total matching records"),
      done: z.boolean().describe("Whether all results are returned"),
      records: z.array(z.record(z.unknown())).describe("Matching records including soft-deleted"),
    }),
    execute: async (params, ctx) => {
      const data = await sfFetch(ctx, `/queryAll?q=${enc(params.soql)}`);
      return { totalSize: data.totalSize, done: data.done, records: data.records };
    },
  },
};
