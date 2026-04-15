import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { cfFetch, cfPost, enc } from "../core/client.ts";

export const settings: Record<string, ActionDefinition> = {
  get_zone_settings: {
    description: "Get all zone settings.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
    }),
    returns: z.any().describe("All zone settings as key-value pairs"),
    execute: async (params, ctx) => {
      const data = await cfFetch(ctx, `/zones/${enc(params.zone_id)}/settings`);
      const result: Record<string, any> = {};
      for (const s of Array.isArray(data) ? data : []) {
        result[s.id] = s.value;
      }
      return result;
    },
  },

  update_zone_setting: {
    description: "Update a single zone setting.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
      setting: z.string().describe("Setting name (e.g. always_use_https, minify, security_level)"),
      value: z.string().describe("New value for the setting"),
    }),
    returns: z.object({
      setting: z.string(),
      value: z.any(),
      modified_on: z.string(),
    }),
    execute: async (params, ctx) => {
      let parsedValue: any = params.value;
      try { parsedValue = JSON.parse(params.value); } catch { /* keep as string */ }
      const r = await cfPost(
        ctx,
        `/zones/${enc(params.zone_id)}/settings/${enc(params.setting)}`,
        { value: parsedValue },
        "PATCH",
      );
      return {
        setting: params.setting,
        value: r?.value ?? parsedValue,
        modified_on: r?.modified_on ?? new Date().toISOString(),
      };
    },
  },
};
