import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { metaGet, metaPost } from "../core/client.ts";

export const audiences: Record<string, ActionDefinition> = {
  list_custom_audiences: {
    description: "List custom audiences for an ad account.",
    params: z.object({
      account_id: z.string().describe("Ad account ID"),
      per_page: z.number().min(1).max(100).default(25).describe("Results per page (1-100)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        name: z.string(),
        subtype: z.string(),
        approximate_count: z.number().nullable(),
        delivery_status: z.any().nullable(),
        operation_status: z.any().nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await metaGet(ctx, `/${params.account_id}/customaudiences`, {
        fields: "id,name,subtype,approximate_count,delivery_status,operation_status",
        limit: String(params.per_page),
      });
      return (data.data ?? []).map((a: any) => ({
        id: a.id,
        name: a.name,
        subtype: a.subtype,
        approximate_count: a.approximate_count ?? null,
        delivery_status: a.delivery_status ?? null,
        operation_status: a.operation_status ?? null,
      }));
    },
  },

  create_custom_audience: {
    description: "Create a custom audience for targeting.",
    params: z.object({
      account_id: z.string().describe("Ad account ID"),
      name: z.string().describe("Audience name"),
      subtype: z.enum(["WEBSITE", "APP", "ENGAGEMENT", "CUSTOMER_LIST", "LOOKALIKE"]).describe("Audience subtype"),
      description: z.string().optional().describe("Audience description"),
      rule: z.any().optional().describe("JSON rule definition (required for WEBSITE/APP/ENGAGEMENT)"),
    }),
    returns: z.object({ id: z.string(), name: z.string(), subtype: z.string() }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = { name: params.name, subtype: params.subtype };
      if (params.description) body.description = params.description;
      if (params.rule) body.rule = params.rule;
      const r = await metaPost(ctx, `/${params.account_id}/customaudiences`, body);
      return { id: r.id, name: params.name, subtype: params.subtype };
    },
  },

  get_audience_size: {
    description: "Get the approximate size of a custom audience.",
    params: z.object({ audience_id: z.string().describe("Custom audience ID") }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      approximate_count: z.number().nullable(),
      delivery_status: z.any().nullable(),
    }),
    execute: async (params, ctx) => {
      const a = await metaGet(ctx, `/${params.audience_id}`, { fields: "id,name,approximate_count,delivery_status" });
      return {
        id: a.id,
        name: a.name,
        approximate_count: a.approximate_count ?? null,
        delivery_status: a.delivery_status ?? null,
      };
    },
  },
};
