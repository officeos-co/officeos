import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { metaGet, metaPost } from "../core/client.ts";

export const ads: Record<string, ActionDefinition> = {
  list_ads: {
    description: "List ads for an ad account.",
    params: z.object({
      account_id: z.string().describe("Ad account ID"),
      ad_set_id: z.string().optional().describe("Filter by ad set"),
      status: z.enum(["ACTIVE", "PAUSED", "DELETED", "ARCHIVED"]).optional().describe("Filter by status"),
      per_page: z.number().min(1).max(100).default(25).describe("Results per page (1-100)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        name: z.string(),
        ad_set_id: z.string(),
        creative_id: z.string().nullable(),
        status: z.string(),
        created_time: z.string(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = {
        fields: "id,name,adset_id,creative{id},status,created_time",
        limit: String(params.per_page),
      };
      if (params.status) q.effective_status = `["${params.status}"]`;
      const path = params.ad_set_id ? `/${params.ad_set_id}/ads` : `/${params.account_id}/ads`;
      const data = await metaGet(ctx, path, q);
      return (data.data ?? []).map((a: any) => ({
        id: a.id,
        name: a.name,
        ad_set_id: a.adset_id,
        creative_id: a.creative?.id ?? null,
        status: a.status,
        created_time: a.created_time,
      }));
    },
  },

  get_ad: {
    description: "Get detailed information about a single ad.",
    params: z.object({ ad_id: z.string().describe("Ad ID") }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      ad_set_id: z.string(),
      creative: z.any().describe("Ad creative data"),
      status: z.string(),
      created_time: z.string(),
      updated_time: z.string(),
      preview_shareable_link: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const a = await metaGet(ctx, `/${params.ad_id}`, {
        fields: "id,name,adset_id,creative,status,created_time,updated_time,preview_shareable_link",
      });
      return {
        id: a.id,
        name: a.name,
        ad_set_id: a.adset_id,
        creative: a.creative ?? null,
        status: a.status,
        created_time: a.created_time,
        updated_time: a.updated_time,
        preview_shareable_link: a.preview_shareable_link ?? null,
      };
    },
  },

  create_ad: {
    description: "Create a new ad within an ad set.",
    params: z.object({
      ad_set_id: z.string().describe("Parent ad set ID"),
      creative_id: z.string().describe("Ad creative ID to use"),
      name: z.string().describe("Ad name"),
      status: z.enum(["ACTIVE", "PAUSED"]).default("PAUSED").describe("Initial status"),
    }),
    returns: z.object({ id: z.string(), name: z.string(), status: z.string() }),
    execute: async (params, ctx) => {
      const adSet = await metaGet(ctx, `/${params.ad_set_id}`, { fields: "account_id" });
      const body: Record<string, unknown> = {
        adset_id: params.ad_set_id,
        creative: { creative_id: params.creative_id },
        name: params.name,
        status: params.status,
      };
      const r = await metaPost(ctx, `/act_${adSet.account_id}/ads`, body);
      return { id: r.id, name: params.name, status: params.status };
    },
  },

  update_ad: {
    description: "Update an existing ad.",
    params: z.object({
      ad_id: z.string().describe("Ad ID"),
      name: z.string().optional().describe("Updated name"),
      status: z.enum(["ACTIVE", "PAUSED"]).optional().describe("Updated status"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {};
      if (params.name) body.name = params.name;
      if (params.status) body.status = params.status;
      const r = await metaPost(ctx, `/${params.ad_id}`, body);
      return { success: r.success ?? true };
    },
  },

  pause_ad: {
    description: "Pause an ad.",
    params: z.object({ ad_id: z.string().describe("Ad ID") }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      const r = await metaPost(ctx, `/${params.ad_id}`, { status: "PAUSED" });
      return { success: r.success ?? true };
    },
  },
};
