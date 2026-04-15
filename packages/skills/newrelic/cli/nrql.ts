import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { nerdgraph } from "../core/client.ts";

export const nrql: Record<string, ActionDefinition> = {
  nrql: {
    description: "Run a NRQL query against your New Relic account.",
    params: z.object({
      query: z.string().describe("NRQL query string (e.g. SELECT count(*) FROM Transaction SINCE 1 hour ago)"),
      timeout: z.number().int().default(5).describe("Query timeout in seconds"),
    }),
    returns: z.object({
      results: z.array(z.any()),
      metadata: z.any().optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key, account_id } = ctx.credentials;
      const gql = `
        query NrqlQuery($accountId: Int!, $query: Nrql!, $timeout: Seconds) {
          actor {
            account(id: $accountId) {
              nrql(query: $query, timeout: $timeout) {
                results
                metadata { facets eventTypes messages }
              }
            }
          }
        }
      `;
      const data = await nerdgraph(ctx.fetch, api_key, gql, {
        accountId: parseInt(account_id, 10),
        query: params.query,
        timeout: params.timeout,
      });
      const nrqlData = data?.actor?.account?.nrql;
      return { results: nrqlData?.results ?? [], metadata: nrqlData?.metadata };
    },
  },

  nrql_async: {
    description: "Run an async NRQL query for large datasets or long time windows.",
    params: z.object({
      query: z.string().describe("NRQL query string"),
      timeout: z.number().int().default(60).describe("Max wait seconds"),
    }),
    returns: z.object({
      results: z.array(z.any()),
      metadata: z.any().optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key, account_id } = ctx.credentials;
      const gql = `
        query NrqlAsyncQuery($accountId: Int!, $query: Nrql!) {
          actor {
            account(id: $accountId) {
              nrql(query: $query, async: true) {
                results
                metadata { facets eventTypes messages }
              }
            }
          }
        }
      `;
      const data = await nerdgraph(ctx.fetch, api_key, gql, {
        accountId: parseInt(account_id, 10),
        query: params.query,
      });
      const nrqlData = data?.actor?.account?.nrql;
      return { results: nrqlData?.results ?? [], metadata: nrqlData?.metadata };
    },
  },
};
