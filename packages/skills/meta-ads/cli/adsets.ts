import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { metaGet, metaPost } from "../core/client.ts";

export const adsets: Record<string, ActionDefinition> = {
  list_ad_sets: {
    description: "List ad sets for an ad account.",
    params: z.object({
      account_id: z.string().describe("Ad account ID"),
      campaign_id: z.string().optional().describe("Filter by campaign"),
      status: z.enum(["ACTIVE", "PAUSED", "DELETED", "ARCHIVED"]).optional().describe("Filter by status"),
      per_page: z.number().min(1).max(100).default(25).describe("Results per page (1-100)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        name: z.string(),
        campaign_id: z.string(),
        status: z.string(),
        daily_budget: z.string().nullable(),
        targeting: z.any().describe("Targeting spec"),
        optimization_goal: z.string(),
        billing_event: z.string(),
        start_time: z.string().nullable(),
        end_time: z.string().nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = {
        fields: "id,name,campaign_id,status,daily_budget,targeting,optimization_goal,billing_event,start_time,end_time",
        limit: String(params.per_page),
      };
      if (params.status) q.effective_status = `["${params.status}"]`;
      const path = params.campaign_id ? `/${params.campaign_id}/adsets` : `/${params.account_id}/adsets`;
      const data = await metaGet(ctx, path, q);
      return (data.data ?? []).map((s: any) => ({
        id: s.id,
        name: s.name,
        campaign_id: s.campaign_id,
        status: s.status,
        daily_budget: s.daily_budget ?? null,
        targeting: s.targeting ?? null,
        optimization_goal: s.optimization_goal,
        billing_event: s.billing_event,
        start_time: s.start_time ?? null,
        end_time: s.end_time ?? null,
      }));
    },
  },

  get_ad_set: {
    description: "Get detailed information about a single ad set.",
    params: z.object({ ad_set_id: z.string().describe("Ad set ID") }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      campaign_id: z.string(),
      status: z.string(),
      daily_budget: z.string().nullable(),
      lifetime_budget: z.string().nullable(),
      targeting: z.any().describe("Targeting spec"),
      optimization_goal: z.string(),
      billing_event: z.string(),
      bid_amount: z.number().nullable(),
      start_time: z.string().nullable(),
      end_time: z.string().nullable(),
      created_time: z.string(),
    }),
    execute: async (params, ctx) => {
      const s = await metaGet(ctx, `/${params.ad_set_id}`, {
        fields: "id,name,campaign_id,status,daily_budget,lifetime_budget,targeting,optimization_goal,billing_event,bid_amount,start_time,end_time,created_time",
      });
      return {
        id: s.id,
        name: s.name,
        campaign_id: s.campaign_id,
        status: s.status,
        daily_budget: s.daily_budget ?? null,
        lifetime_budget: s.lifetime_budget ?? null,
        targeting: s.targeting ?? null,
        optimization_goal: s.optimization_goal,
        billing_event: s.billing_event,
        bid_amount: s.bid_amount ?? null,
        start_time: s.start_time ?? null,
        end_time: s.end_time ?? null,
        created_time: s.created_time,
      };
    },
  },

  create_ad_set: {
    description: "Create a new ad set within a campaign.",
    params: z.object({
      campaign_id: z.string().describe("Parent campaign ID"),
      name: z.string().describe("Ad set name"),
      daily_budget: z.number().optional().describe("Daily budget in cents"),
      lifetime_budget: z.number().optional().describe("Lifetime budget in cents"),
      billing_event: z.enum(["IMPRESSIONS", "LINK_CLICKS", "APP_INSTALLS"]).describe("Billing event"),
      optimization_goal: z.enum(["CONVERSIONS", "LINK_CLICKS", "IMPRESSIONS", "REACH", "LEAD_GENERATION"]).describe("Optimization goal"),
      targeting: z.any().describe("JSON targeting spec (geo, age, gender, interests, behaviors)"),
      start_time: z.string().optional().describe("ISO 8601 start time"),
      end_time: z.string().optional().describe("ISO 8601 end time"),
    }),
    returns: z.object({ id: z.string(), name: z.string(), status: z.string() }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {
        campaign_id: params.campaign_id,
        name: params.name,
        billing_event: params.billing_event,
        optimization_goal: params.optimization_goal,
        targeting: params.targeting,
        status: "PAUSED",
      };
      if (params.daily_budget !== undefined) body.daily_budget = params.daily_budget;
      if (params.lifetime_budget !== undefined) body.lifetime_budget = params.lifetime_budget;
      if (params.start_time) body.start_time = params.start_time;
      if (params.end_time) body.end_time = params.end_time;
      const campaign = await metaGet(ctx, `/${params.campaign_id}`, { fields: "account_id" });
      const r = await metaPost(ctx, `/act_${campaign.account_id}/adsets`, body);
      return { id: r.id, name: params.name, status: "PAUSED" };
    },
  },

  update_ad_set: {
    description: "Update an existing ad set.",
    params: z.object({
      ad_set_id: z.string().describe("Ad set ID"),
      name: z.string().optional().describe("Updated name"),
      daily_budget: z.number().optional().describe("Updated budget in cents"),
      targeting: z.any().optional().describe("Updated targeting spec"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {};
      if (params.name) body.name = params.name;
      if (params.daily_budget !== undefined) body.daily_budget = params.daily_budget;
      if (params.targeting) body.targeting = params.targeting;
      const r = await metaPost(ctx, `/${params.ad_set_id}`, body);
      return { success: r.success ?? true };
    },
  },

  pause_ad_set: {
    description: "Pause an ad set.",
    params: z.object({ ad_set_id: z.string().describe("Ad set ID") }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      const r = await metaPost(ctx, `/${params.ad_set_id}`, { status: "PAUSED" });
      return { success: r.success ?? true };
    },
  },
};
