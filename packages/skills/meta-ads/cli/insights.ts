import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { metaGet } from "../core/client.ts";

const insightFields = "spend,impressions,clicks,cpc,ctr,conversions,cost_per_conversion,reach,frequency";

const insightReturn = z.object({
  spend: z.string().nullable(),
  impressions: z.string().nullable(),
  clicks: z.string().nullable(),
  cpc: z.string().nullable(),
  ctr: z.string().nullable(),
  conversions: z.string().nullable(),
  cost_per_conversion: z.string().nullable(),
  reach: z.string().nullable(),
  frequency: z.string().nullable(),
});

function mapInsight(d: any) {
  return {
    spend: d.spend ?? null,
    impressions: d.impressions ?? null,
    clicks: d.clicks ?? null,
    cpc: d.cpc ?? null,
    ctr: d.ctr ?? null,
    conversions: d.conversions ?? null,
    cost_per_conversion: d.cost_per_conversion ?? null,
    reach: d.reach ?? null,
    frequency: d.frequency ?? null,
  };
}

export const insights: Record<string, ActionDefinition> = {
  get_insights: {
    description: "Get performance insights for an account, campaign, ad set, or ad.",
    params: z.object({
      object_id: z.string().describe("Account, campaign, ad set, or ad ID"),
      level: z.enum(["account", "campaign", "adset", "ad"]).default("campaign").describe("Aggregation level"),
      date_range: z.object({
        since: z.string().describe("Start date YYYY-MM-DD"),
        until: z.string().describe("End date YYYY-MM-DD"),
      }).optional().describe("Date range"),
      fields: z.array(z.string()).optional().describe("Metrics to return"),
      breakdowns: z.array(z.string()).optional().describe("Breakdown dimensions"),
      time_increment: z.number().optional().describe("Aggregate by N days (1=daily, 7=weekly)"),
    }),
    returns: z.array(z.any()).describe("List of metrics per breakdown combination"),
    execute: async (params, ctx) => {
      const defaultFields = ["spend", "impressions", "clicks", "cpc", "ctr", "conversions", "cost_per_conversion", "reach", "frequency"];
      const q: Record<string, string> = { level: params.level, fields: (params.fields ?? defaultFields).join(",") };
      if (params.date_range) q.time_range = JSON.stringify(params.date_range);
      if (params.breakdowns) q.breakdowns = params.breakdowns.join(",");
      if (params.time_increment !== undefined) q.time_increment = String(params.time_increment);
      const data = await metaGet(ctx, `/${params.object_id}/insights`, q);
      return data.data ?? [];
    },
  },

  get_campaign_insights: {
    description: "Get performance insights for a specific campaign.",
    params: z.object({
      campaign_id: z.string().describe("Campaign ID"),
      date_range: z.object({
        since: z.string().describe("Start date YYYY-MM-DD"),
        until: z.string().describe("End date YYYY-MM-DD"),
      }).optional().describe("Date range"),
    }),
    returns: insightReturn,
    execute: async (params, ctx) => {
      const q: Record<string, string> = { fields: insightFields };
      if (params.date_range) q.time_range = JSON.stringify(params.date_range);
      const data = await metaGet(ctx, `/${params.campaign_id}/insights`, q);
      return mapInsight(data.data?.[0] ?? {});
    },
  },

  get_ad_set_insights: {
    description: "Get performance insights for a specific ad set.",
    params: z.object({
      ad_set_id: z.string().describe("Ad set ID"),
      date_range: z.object({
        since: z.string().describe("Start date YYYY-MM-DD"),
        until: z.string().describe("End date YYYY-MM-DD"),
      }).optional().describe("Date range"),
    }),
    returns: insightReturn,
    execute: async (params, ctx) => {
      const q: Record<string, string> = { fields: insightFields };
      if (params.date_range) q.time_range = JSON.stringify(params.date_range);
      const data = await metaGet(ctx, `/${params.ad_set_id}/insights`, q);
      return mapInsight(data.data?.[0] ?? {});
    },
  },

  get_ad_insights: {
    description: "Get performance insights for a specific ad.",
    params: z.object({
      ad_id: z.string().describe("Ad ID"),
      date_range: z.object({
        since: z.string().describe("Start date YYYY-MM-DD"),
        until: z.string().describe("End date YYYY-MM-DD"),
      }).optional().describe("Date range"),
    }),
    returns: insightReturn,
    execute: async (params, ctx) => {
      const q: Record<string, string> = { fields: insightFields };
      if (params.date_range) q.time_range = JSON.stringify(params.date_range);
      const data = await metaGet(ctx, `/${params.ad_id}/insights`, q);
      return mapInsight(data.data?.[0] ?? {});
    },
  },
};
