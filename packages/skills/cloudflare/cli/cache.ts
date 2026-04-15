import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { cfFetch, cfPost, enc } from "../core/client.ts";

export const cache: Record<string, ActionDefinition> = {
  purge_cache: {
    description: "Purge cached content for a zone.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
      purge_everything: z.boolean().default(false).describe("Purge entire cache"),
      files: z.array(z.string()).optional().describe("Specific URLs to purge"),
      tags: z.array(z.string()).optional().describe("Cache tags to purge"),
    }),
    returns: z.object({ id: z.string() }),
    execute: async (params, ctx) => {
      const body: any = {};
      if (params.purge_everything) {
        body.purge_everything = true;
      } else {
        if (params.files) body.files = params.files;
        if (params.tags) body.tags = params.tags;
      }
      const r = await cfPost(ctx, `/zones/${enc(params.zone_id)}/purge_cache`, body);
      return { id: r?.id ?? "" };
    },
  },

  cache_settings: {
    description: "Get or update cache settings for a zone.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
      browser_ttl: z.number().optional().describe("Browser cache TTL in seconds"),
      cache_level: z.string().optional().describe("aggressive, basic, or simplified"),
    }),
    returns: z.object({
      browser_ttl: z.any(),
      cache_level: z.any(),
      development_mode: z.any(),
    }),
    execute: async (params, ctx) => {
      if (params.browser_ttl !== undefined) {
        await cfPost(
          ctx,
          `/zones/${enc(params.zone_id)}/settings/browser_cache_ttl`,
          { value: params.browser_ttl },
          "PATCH",
        );
      }
      if (params.cache_level !== undefined) {
        await cfPost(
          ctx,
          `/zones/${enc(params.zone_id)}/settings/cache_level`,
          { value: params.cache_level },
          "PATCH",
        );
      }
      const browserTtl = await cfFetch(ctx, `/zones/${enc(params.zone_id)}/settings/browser_cache_ttl`);
      const cacheLevel = await cfFetch(ctx, `/zones/${enc(params.zone_id)}/settings/cache_level`);
      const devMode = await cfFetch(ctx, `/zones/${enc(params.zone_id)}/settings/development_mode`);
      return {
        browser_ttl: browserTtl?.value,
        cache_level: cacheLevel?.value,
        development_mode: devMode?.value,
      };
    },
  },
};
