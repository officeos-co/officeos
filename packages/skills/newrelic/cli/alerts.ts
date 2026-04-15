import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { nerdgraph } from "../core/client.ts";

export const alerts: Record<string, ActionDefinition> = {
  list_alert_policies: {
    description: "List alert policies in the account.",
    params: z.object({
      name: z.string().optional().describe("Filter by policy name"),
      cursor: z.string().optional().describe("Pagination cursor"),
    }),
    returns: z.object({
      policies: z.array(z.object({
        id: z.string(),
        name: z.string(),
        incidentPreference: z.string().optional(),
      })),
      nextCursor: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const { api_key, account_id } = ctx.credentials;
      const gql = `
        query ListPolicies($accountId: Int!, $cursor: String, $name: String) {
          actor {
            account(id: $accountId) {
              alerts {
                policiesSearch(searchCriteria: {name: $name}, cursor: $cursor) {
                  policies { id name incidentPreference }
                  nextCursor
                }
              }
            }
          }
        }
      `;
      const data = await nerdgraph(ctx.fetch, api_key, gql, {
        accountId: parseInt(account_id, 10),
        cursor: params.cursor ?? null,
        name: params.name ?? null,
      });
      const result = data?.actor?.account?.alerts?.policiesSearch;
      return { policies: result?.policies ?? [], nextCursor: result?.nextCursor ?? null };
    },
  },

  create_alert_policy: {
    description: "Create a new alert policy.",
    params: z.object({
      name: z.string().describe("Policy name"),
      incident_preference: z.enum(["PER_POLICY", "PER_CONDITION", "PER_CONDITION_AND_TARGET"])
        .default("PER_POLICY")
        .describe("Incident rollup preference"),
    }),
    returns: z.object({ id: z.string(), name: z.string(), incidentPreference: z.string() }),
    execute: async (params, ctx) => {
      const { api_key, account_id } = ctx.credentials;
      const gql = `
        mutation CreatePolicy($accountId: Int!, $policy: AlertsPolicyInput!) {
          alertsPolicyCreate(accountId: $accountId, policy: $policy) {
            id name incidentPreference
          }
        }
      `;
      const data = await nerdgraph(ctx.fetch, api_key, gql, {
        accountId: parseInt(account_id, 10),
        policy: { name: params.name, incidentPreference: params.incident_preference },
      });
      return data?.alertsPolicyCreate;
    },
  },

  list_alert_conditions: {
    description: "List NRQL alert conditions, optionally filtered by policy.",
    params: z.object({
      policy_id: z.string().optional().describe("Filter by policy ID"),
      cursor: z.string().optional().describe("Pagination cursor"),
    }),
    returns: z.object({
      conditions: z.array(z.object({
        id: z.string(),
        name: z.string(),
        enabled: z.boolean(),
        type: z.string().optional(),
        nrql: z.object({ query: z.string() }).optional(),
      })),
      nextCursor: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const { api_key, account_id } = ctx.credentials;
      const gql = `
        query ListConditions($accountId: Int!, $cursor: String, $policyId: ID) {
          actor {
            account(id: $accountId) {
              alerts {
                nrqlConditionsSearch(searchCriteria: {policyId: $policyId}, cursor: $cursor) {
                  nrqlConditions { id name enabled type nrql { query } }
                  nextCursor
                }
              }
            }
          }
        }
      `;
      const data = await nerdgraph(ctx.fetch, api_key, gql, {
        accountId: parseInt(account_id, 10),
        cursor: params.cursor ?? null,
        policyId: params.policy_id ?? null,
      });
      const result = data?.actor?.account?.alerts?.nrqlConditionsSearch;
      return { conditions: result?.nrqlConditions ?? [], nextCursor: result?.nextCursor ?? null };
    },
  },

  create_alert_condition: {
    description: "Create a NRQL alert condition on an existing policy.",
    params: z.object({
      policy_id: z.string().describe("Policy ID to attach the condition to"),
      name: z.string().describe("Condition name"),
      nrql: z.string().describe("NRQL query for the condition"),
      critical_threshold: z.number().describe("Critical alert threshold value"),
      warning_threshold: z.number().optional().describe("Warning alert threshold value"),
      aggregation_window: z.number().int().default(60).describe("Aggregation window in seconds"),
      operator: z.enum(["ABOVE", "BELOW", "EQUALS"]).default("ABOVE").describe("Threshold comparison operator"),
      fill_option: z.enum(["NONE", "LAST_VALUE", "STATIC"]).optional().describe("Signal gap fill option"),
    }),
    returns: z.object({ id: z.string(), name: z.string(), enabled: z.boolean(), nrql: z.object({ query: z.string() }).optional() }),
    execute: async (params, ctx) => {
      const { api_key, account_id } = ctx.credentials;
      const terms: any[] = [{
        threshold: params.critical_threshold,
        thresholdDuration: params.aggregation_window,
        thresholdOccurrences: "ALL",
        operator: params.operator,
        priority: "CRITICAL",
      }];
      if (params.warning_threshold !== undefined) {
        terms.push({
          threshold: params.warning_threshold,
          thresholdDuration: params.aggregation_window,
          thresholdOccurrences: "ALL",
          operator: params.operator,
          priority: "WARNING",
        });
      }
      const condition: Record<string, any> = {
        name: params.name,
        nrql: { query: params.nrql },
        signal: {
          aggregationWindow: params.aggregation_window,
          fillOption: params.fill_option ?? "NONE",
        },
        terms,
        enabled: true,
      };
      const gql = `
        mutation CreateCondition($accountId: Int!, $policyId: ID!, $condition: AlertsNrqlConditionStaticInput!) {
          alertsNrqlConditionStaticCreate(accountId: $accountId, policyId: $policyId, condition: $condition) {
            id name enabled nrql { query }
          }
        }
      `;
      const data = await nerdgraph(ctx.fetch, api_key, gql, {
        accountId: parseInt(account_id, 10),
        policyId: params.policy_id,
        condition,
      });
      return data?.alertsNrqlConditionStaticCreate;
    },
  },
};
