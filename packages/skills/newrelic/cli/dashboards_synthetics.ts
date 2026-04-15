import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { nerdgraph } from "../core/client.ts";

export const dashboardsSynthetics: Record<string, ActionDefinition> = {
  list_dashboards: {
    description: "List New Relic dashboards in the account.",
    params: z.object({
      name: z.string().optional().describe("Filter by dashboard name"),
      cursor: z.string().optional().describe("Pagination cursor"),
    }),
    returns: z.object({
      dashboards: z.array(z.object({
        guid: z.string(),
        name: z.string(),
        description: z.string().optional(),
        createdAt: z.string().optional(),
        updatedAt: z.string().optional(),
        permalink: z.string().optional(),
      })),
      nextCursor: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const { api_key, account_id } = ctx.credentials;
      const nameFilter = params.name ? ` AND name LIKE '%${params.name}%'` : "";
      const gql = `
        query ListDashboards($cursor: String) {
          actor {
            entitySearch(query: "accountId = ${account_id} AND type = 'DASHBOARD'${nameFilter}", options: {limit: 50}) {
              results(cursor: $cursor) {
                entities {
                  guid
                  name
                  ... on DashboardEntityOutline {
                    description
                    createdAt
                    updatedAt
                    permalink
                  }
                }
                nextCursor
              }
            }
          }
        }
      `;
      const data = await nerdgraph(ctx.fetch, api_key, gql, { cursor: params.cursor ?? null });
      const result = data?.actor?.entitySearch?.results;
      return {
        dashboards: (result?.entities ?? []).map((e: any) => ({
          guid: e.guid,
          name: e.name,
          description: e.description,
          createdAt: e.createdAt,
          updatedAt: e.updatedAt,
          permalink: e.permalink,
        })),
        nextCursor: result?.nextCursor ?? null,
      };
    },
  },

  get_dashboard: {
    description: "Get a dashboard by entity GUID including pages and widgets.",
    params: z.object({
      guid: z.string().describe("Dashboard entity GUID"),
    }),
    returns: z.object({
      guid: z.string(),
      name: z.string(),
      description: z.string().optional(),
      pages: z.array(z.any()).optional(),
      createdAt: z.string().optional(),
      updatedAt: z.string().optional(),
      permalink: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key } = ctx.credentials;
      const gql = `
        query GetDashboard($guid: EntityGuid!) {
          actor {
            entity(guid: $guid) {
              guid
              name
              ... on DashboardEntity {
                description
                createdAt
                updatedAt
                permalink
                pages {
                  name
                  widgets {
                    id
                    title
                    configuration { ... on WidgetConfigurationBillboard { nrqlQueries { query } } }
                  }
                }
              }
            }
          }
        }
      `;
      const data = await nerdgraph(ctx.fetch, api_key, gql, { guid: params.guid });
      const e = data?.actor?.entity;
      return {
        guid: e?.guid,
        name: e?.name,
        description: e?.description,
        pages: e?.pages,
        createdAt: e?.createdAt,
        updatedAt: e?.updatedAt,
        permalink: e?.permalink,
      };
    },
  },

  create_dashboard: {
    description: "Create a new New Relic dashboard.",
    params: z.object({
      name: z.string().describe("Dashboard name"),
      description: z.string().optional().describe("Dashboard description"),
      pages: z.array(z.any()).optional().describe("Array of page definitions"),
      permissions: z.enum(["PUBLIC_READ_ONLY", "PUBLIC_READ_WRITE", "PRIVATE"]).default("PRIVATE")
        .describe("Dashboard permissions"),
    }),
    returns: z.object({ guid: z.string(), name: z.string(), permalink: z.string().optional() }),
    execute: async (params, ctx) => {
      const { api_key, account_id } = ctx.credentials;
      const gql = `
        mutation CreateDashboard($accountId: Int!, $dashboard: DashboardInput!) {
          dashboardCreate(accountId: $accountId, dashboard: $dashboard) {
            entityResult { guid name permalink }
            errors { description type }
          }
        }
      `;
      const dashboard: Record<string, any> = {
        name: params.name,
        permissions: params.permissions,
        pages: params.pages ?? [],
      };
      if (params.description) dashboard.description = params.description;
      const data = await nerdgraph(ctx.fetch, api_key, gql, {
        accountId: parseInt(account_id, 10),
        dashboard,
      });
      const result = data?.dashboardCreate?.entityResult;
      return { guid: result?.guid, name: result?.name, permalink: result?.permalink };
    },
  },

  list_synthetic_monitors: {
    description: "List Synthetics monitors in the account.",
    params: z.object({
      name: z.string().optional().describe("Filter by monitor name"),
      cursor: z.string().optional().describe("Pagination cursor"),
    }),
    returns: z.object({
      monitors: z.array(z.object({
        guid: z.string(),
        name: z.string(),
        monitorType: z.string().optional(),
        status: z.string().optional(),
        period: z.string().optional(),
        uri: z.string().optional(),
      })),
      nextCursor: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const { api_key, account_id } = ctx.credentials;
      const nameFilter = params.name ? ` AND name LIKE '%${params.name}%'` : "";
      const gql = `
        query ListMonitors($cursor: String) {
          actor {
            entitySearch(query: "accountId = ${account_id} AND type = 'MONITOR'${nameFilter}", options: {limit: 50}) {
              results(cursor: $cursor) {
                entities {
                  guid
                  name
                  ... on SyntheticMonitorEntityOutline {
                    monitorType
                    monitoredUrl
                    period
                  }
                }
                nextCursor
              }
            }
          }
        }
      `;
      const data = await nerdgraph(ctx.fetch, api_key, gql, { cursor: params.cursor ?? null });
      const result = data?.actor?.entitySearch?.results;
      return {
        monitors: (result?.entities ?? []).map((e: any) => ({
          guid: e.guid,
          name: e.name,
          monitorType: e.monitorType,
          status: e.alertSeverity,
          period: e.period,
          uri: e.monitoredUrl,
        })),
        nextCursor: result?.nextCursor ?? null,
      };
    },
  },

  get_synthetic_monitor: {
    description: "Get details of a specific Synthetics monitor.",
    params: z.object({
      guid: z.string().describe("Monitor GUID"),
    }),
    returns: z.object({
      guid: z.string(),
      name: z.string(),
      monitorType: z.string().optional(),
      status: z.string().optional(),
      period: z.string().optional(),
      uri: z.string().optional(),
      tags: z.array(z.any()).optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key } = ctx.credentials;
      const gql = `
        query GetMonitor($guid: EntityGuid!) {
          actor {
            entity(guid: $guid) {
              guid
              name
              tags { key values }
              ... on SyntheticMonitorEntity {
                monitorType
                monitoredUrl
                period
              }
            }
          }
        }
      `;
      const data = await nerdgraph(ctx.fetch, api_key, gql, { guid: params.guid });
      const e = data?.actor?.entity;
      return {
        guid: e?.guid,
        name: e?.name,
        monitorType: e?.monitorType,
        period: e?.period,
        uri: e?.monitoredUrl,
        tags: e?.tags,
      };
    },
  },
};
