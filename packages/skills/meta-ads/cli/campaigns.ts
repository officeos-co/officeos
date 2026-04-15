import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { metaGet, metaPost, metaDelete } from "../core/client.ts";

export const campaigns: Record<string, ActionDefinition> = {
  list_campaigns: {
    description: "List campaigns for an ad account.",
    params: z.object({
      account_id: z.string().describe("Ad account ID"),
      status: z.enum(["ACTIVE", "PAUSED", "DELETED", "ARCHIVED"]).optional().describe("Filter by status"),
      date_range: z.object({
        since: z.string().describe("Start date YYYY-MM-DD"),
        until: z.string().describe("End date YYYY-MM-DD"),
      }).optional().describe("Date range filter"),
      per_page: z.number().min(1).max(100).default(25).describe("Results per page (1-100)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        name: z.string(),
        objective: z.string(),
        status: z.string(),
        daily_budget: z.string().nullable(),
        lifetime_budget: z.string().nullable(),
        created_time: z.string(),
        updated_time: z.string(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = {
        fields: "id,name,objective,status,daily_budget,lifetime_budget,created_time,updated_time",
        limit: String(params.per_page),
      };
      if (params.status) q.effective_status = `["${params.status}"]`;
      if (params.date_range) q.time_range = JSON.stringify(params.date_range);
      const data = await metaGet(ctx, `/${params.account_id}/campaigns`, q);
      return (data.data ?? []).map((c: any) => ({
        id: c.id,
        name: c.name,
        objective: c.objective,
        status: c.status ?? c.effective_status,
        daily_budget: c.daily_budget ?? null,
        lifetime_budget: c.lifetime_budget ?? null,
        created_time: c.created_time,
        updated_time: c.updated_time,
      }));
    },
  },

  get_campaign: {
    description: "Get detailed information about a single campaign.",
    params: z.object({ campaign_id: z.string().describe("Campaign ID") }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      objective: z.string(),
      status: z.string(),
      daily_budget: z.string().nullable(),
      lifetime_budget: z.string().nullable(),
      bid_strategy: z.string().nullable(),
      special_ad_categories: z.array(z.string()),
      created_time: z.string(),
      updated_time: z.string(),
    }),
    execute: async (params, ctx) => {
      const c = await metaGet(ctx, `/${params.campaign_id}`, {
        fields: "id,name,objective,status,daily_budget,lifetime_budget,bid_strategy,special_ad_categories,created_time,updated_time",
      });
      return {
        id: c.id,
        name: c.name,
        objective: c.objective,
        status: c.status,
        daily_budget: c.daily_budget ?? null,
        lifetime_budget: c.lifetime_budget ?? null,
        bid_strategy: c.bid_strategy ?? null,
        special_ad_categories: c.special_ad_categories ?? [],
        created_time: c.created_time,
        updated_time: c.updated_time,
      };
    },
  },

  create_campaign: {
    description: "Create a new ad campaign.",
    params: z.object({
      account_id: z.string().describe("Ad account ID"),
      name: z.string().describe("Campaign name"),
      objective: z.enum(["OUTCOME_AWARENESS", "OUTCOME_TRAFFIC", "OUTCOME_ENGAGEMENT", "OUTCOME_LEADS", "OUTCOME_APP_PROMOTION", "OUTCOME_SALES"]).describe("Campaign objective"),
      status: z.enum(["ACTIVE", "PAUSED"]).default("PAUSED").describe("Initial status"),
      daily_budget: z.number().optional().describe("Daily budget in cents"),
      lifetime_budget: z.number().optional().describe("Lifetime budget in cents (mutually exclusive with daily_budget)"),
      bid_strategy: z.enum(["LOWEST_COST_WITHOUT_CAP", "LOWEST_COST_WITH_BID_CAP", "COST_CAP"]).optional().describe("Bid strategy"),
      special_ad_categories: z.array(z.enum(["CREDIT", "EMPLOYMENT", "HOUSING", "SOCIAL_ISSUES_ELECTIONS_POLITICS"])).default([]).describe("Special ad categories"),
    }),
    returns: z.object({ id: z.string(), name: z.string(), status: z.string() }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {
        name: params.name,
        objective: params.objective,
        status: params.status,
        special_ad_categories: params.special_ad_categories,
      };
      if (params.daily_budget !== undefined) body.daily_budget = params.daily_budget;
      if (params.lifetime_budget !== undefined) body.lifetime_budget = params.lifetime_budget;
      if (params.bid_strategy) body.bid_strategy = params.bid_strategy;
      const r = await metaPost(ctx, `/${params.account_id}/campaigns`, body);
      return { id: r.id, name: params.name, status: params.status };
    },
  },

  update_campaign: {
    description: "Update an existing campaign.",
    params: z.object({
      campaign_id: z.string().describe("Campaign ID"),
      name: z.string().optional().describe("Updated name"),
      status: z.enum(["ACTIVE", "PAUSED"]).optional().describe("Updated status"),
      daily_budget: z.number().optional().describe("Updated daily budget in cents"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {};
      if (params.name) body.name = params.name;
      if (params.status) body.status = params.status;
      if (params.daily_budget !== undefined) body.daily_budget = params.daily_budget;
      const r = await metaPost(ctx, `/${params.campaign_id}`, body);
      return { success: r.success ?? true };
    },
  },

  pause_campaign: {
    description: "Pause a running campaign.",
    params: z.object({ campaign_id: z.string().describe("Campaign ID") }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      const r = await metaPost(ctx, `/${params.campaign_id}`, { status: "PAUSED" });
      return { success: r.success ?? true };
    },
  },

  delete_campaign: {
    description: "Permanently delete a campaign. This cannot be undone.",
    params: z.object({ campaign_id: z.string().describe("Campaign ID") }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      const r = await metaDelete(ctx, `/${params.campaign_id}`);
      return { success: r.success ?? true };
    },
  },
};
