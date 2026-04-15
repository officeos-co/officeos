import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { metaGet } from "../core/client.ts";

export const targeting: Record<string, ActionDefinition> = {
  search_targeting: {
    description: "Search for targeting options by keyword.",
    params: z.object({
      query: z.string().describe("Search term"),
      type: z.enum(["interest", "behavior", "demographics", "education", "work_employer", "work_position"]).describe("Targeting type"),
      per_page: z.number().min(1).max(100).default(25).describe("Results per page"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        name: z.string(),
        type: z.string(),
        audience_size_lower_bound: z.number().nullable(),
        audience_size_upper_bound: z.number().nullable(),
        path: z.array(z.string()).nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await metaGet(ctx, "/search", {
        type: "adTargetingCategory",
        class: params.type,
        q: params.query,
        limit: String(params.per_page),
      });
      return (data.data ?? []).map((t: any) => ({
        id: t.id,
        name: t.name,
        type: t.type,
        audience_size_lower_bound: t.audience_size_lower_bound ?? null,
        audience_size_upper_bound: t.audience_size_upper_bound ?? null,
        path: t.path ?? null,
      }));
    },
  },

  get_targeting_suggestions: {
    description: "Get targeting suggestions based on existing targeting IDs.",
    params: z.object({
      targeting_list: z.array(z.string()).describe("List of targeting IDs to seed"),
      type: z.string().optional().describe("Filter suggestion type"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        name: z.string(),
        type: z.string(),
        audience_size_lower_bound: z.number().nullable(),
        audience_size_upper_bound: z.number().nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { targeting_list: JSON.stringify(params.targeting_list) };
      if (params.type) q.type = params.type;
      const data = await metaGet(ctx, "/search", { ...q, type: "adTargetingSuggestions" });
      return (data.data ?? []).map((t: any) => ({
        id: t.id,
        name: t.name,
        type: t.type,
        audience_size_lower_bound: t.audience_size_lower_bound ?? null,
        audience_size_upper_bound: t.audience_size_upper_bound ?? null,
      }));
    },
  },
};
