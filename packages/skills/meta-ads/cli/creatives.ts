import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { metaGet, metaPost } from "../core/client.ts";

export const creatives: Record<string, ActionDefinition> = {
  list_creatives: {
    description: "List ad creatives for an ad account.",
    params: z.object({
      account_id: z.string().describe("Ad account ID"),
      per_page: z.number().min(1).max(100).default(25).describe("Results per page (1-100)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        name: z.string().nullable(),
        object_story_spec: z.any().nullable(),
        thumbnail_url: z.string().nullable(),
        status: z.string().nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await metaGet(ctx, `/${params.account_id}/adcreatives`, {
        fields: "id,name,object_story_spec,thumbnail_url,status",
        limit: String(params.per_page),
      });
      return (data.data ?? []).map((c: any) => ({
        id: c.id,
        name: c.name ?? null,
        object_story_spec: c.object_story_spec ?? null,
        thumbnail_url: c.thumbnail_url ?? null,
        status: c.status ?? null,
      }));
    },
  },

  get_creative: {
    description: "Get detailed information about a single ad creative.",
    params: z.object({ creative_id: z.string().describe("Creative ID") }),
    returns: z.object({
      id: z.string(),
      name: z.string().nullable(),
      object_story_spec: z.any().nullable(),
      url_tags: z.string().nullable(),
      thumbnail_url: z.string().nullable(),
      image_url: z.string().nullable(),
      video_id: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const c = await metaGet(ctx, `/${params.creative_id}`, {
        fields: "id,name,object_story_spec,url_tags,thumbnail_url,image_url,video_id",
      });
      return {
        id: c.id,
        name: c.name ?? null,
        object_story_spec: c.object_story_spec ?? null,
        url_tags: c.url_tags ?? null,
        thumbnail_url: c.thumbnail_url ?? null,
        image_url: c.image_url ?? null,
        video_id: c.video_id ?? null,
      };
    },
  },

  create_creative: {
    description: "Create a new ad creative.",
    params: z.object({
      account_id: z.string().describe("Ad account ID"),
      name: z.string().describe("Creative name"),
      object_story_spec: z.any().describe("JSON spec with page_id and link_data (link, message, image_hash/video_data)"),
      url_tags: z.string().optional().describe("URL parameters to append to all links"),
    }),
    returns: z.object({ id: z.string(), name: z.string() }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {
        name: params.name,
        object_story_spec: params.object_story_spec,
      };
      if (params.url_tags) body.url_tags = params.url_tags;
      const r = await metaPost(ctx, `/${params.account_id}/adcreatives`, body);
      return { id: r.id, name: params.name };
    },
  },
};
