import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { metaGet, metaPost } from "../core/client.ts";

export const adImages: Record<string, ActionDefinition> = {
  upload_image: {
    description: "Upload an image from a URL for use in ad creatives.",
    params: z.object({
      account_id: z.string().describe("Ad account ID"),
      url: z.string().describe("Public URL of the image to upload"),
      name: z.string().optional().describe("Image name for reference"),
    }),
    returns: z.object({
      image_hash: z.string(),
      url: z.string(),
      name: z.string().nullable(),
      width: z.number().nullable(),
      height: z.number().nullable(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = { url: params.url };
      if (params.name) body.name = params.name;
      const r = await metaPost(ctx, `/${params.account_id}/adimages`, body);
      const images = r.images ?? {};
      const img = Object.values(images)[0] as any ?? {};
      return {
        image_hash: img.hash ?? "",
        url: img.url ?? params.url,
        name: params.name ?? null,
        width: img.width ?? null,
        height: img.height ?? null,
      };
    },
  },

  list_images: {
    description: "List images for an ad account.",
    params: z.object({
      account_id: z.string().describe("Ad account ID"),
      per_page: z.number().min(1).max(100).default(25).describe("Results per page (1-100)"),
    }),
    returns: z.array(
      z.object({
        image_hash: z.string(),
        url: z.string(),
        name: z.string().nullable(),
        width: z.number().nullable(),
        height: z.number().nullable(),
        created_time: z.string().nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await metaGet(ctx, `/${params.account_id}/adimages`, {
        fields: "hash,url,name,width,height,created_time",
        limit: String(params.per_page),
      });
      return (data.data ?? []).map((i: any) => ({
        image_hash: i.hash,
        url: i.url,
        name: i.name ?? null,
        width: i.width ?? null,
        height: i.height ?? null,
        created_time: i.created_time ?? null,
      }));
    },
  },
};
