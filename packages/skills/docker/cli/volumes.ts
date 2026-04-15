import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { dFetch, dPost, dDelete, enc } from "../core/client.ts";

export const volumes: Record<string, ActionDefinition> = {
  list_volumes: {
    description: "List volumes.",
    params: z.object({
      filter: z.string().optional().describe("Filter expression (e.g. dangling=true)"),
    }),
    returns: z.array(
      z.object({
        name: z.string().describe("Volume name"),
        driver: z.string().describe("Volume driver"),
        mountpoint: z.string().describe("Volume mountpoint"),
        created: z.string().describe("Created timestamp"),
      }),
    ),
    execute: async (params, ctx) => {
      const qs = new URLSearchParams();
      if (params.filter) {
        const [k, v] = params.filter.split("=");
        qs.set("filters", JSON.stringify({ [k]: [v] }));
      }
      const data = await dFetch(ctx, `/volumes?${qs}`);
      return (data.Volumes ?? []).map((v: any) => ({ name: v.Name, driver: v.Driver, mountpoint: v.Mountpoint, created: v.CreatedAt ?? "" }));
    },
  },

  create_volume: {
    description: "Create a volume.",
    params: z.object({
      name: z.string().describe("Volume name"),
      driver: z.string().default("local").describe("Volume driver"),
      labels: z.string().optional().describe("JSON object of labels"),
    }),
    returns: z.object({
      name: z.string().describe("Volume name"),
      driver: z.string().describe("Volume driver"),
      mountpoint: z.string().describe("Volume mountpoint"),
    }),
    execute: async (params, ctx) => {
      const body: any = { Name: params.name, Driver: params.driver };
      if (params.labels) body.Labels = JSON.parse(params.labels);
      const v = await dPost(ctx, `/volumes/create`, body);
      return { name: v.Name, driver: v.Driver, mountpoint: v.Mountpoint };
    },
  },

  rm_volume: {
    description: "Remove a volume.",
    params: z.object({
      name: z.string().describe("Volume name"),
      force: z.boolean().default(false).describe("Force remove"),
    }),
    returns: z.object({ name: z.string() }),
    execute: async (params, ctx) => {
      await dDelete(ctx, `/volumes/${enc(params.name)}?force=${params.force}`);
      return { name: params.name };
    },
  },

  inspect_volume: {
    description: "Inspect a volume.",
    params: z.object({ name: z.string().describe("Volume name") }),
    returns: z.object({
      name: z.string().describe("Volume name"),
      driver: z.string().describe("Volume driver"),
      mountpoint: z.string().describe("Volume mountpoint"),
      labels: z.any().describe("Volume labels"),
      options: z.any().describe("Volume options"),
      created: z.string().describe("Created timestamp"),
    }),
    execute: async (params, ctx) => {
      const v = await dFetch(ctx, `/volumes/${enc(params.name)}`);
      return { name: v.Name, driver: v.Driver, mountpoint: v.Mountpoint, labels: v.Labels ?? {}, options: v.Options ?? {}, created: v.CreatedAt ?? "" };
    },
  },
};
