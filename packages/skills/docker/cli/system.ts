import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { dFetch, dPost } from "../core/client.ts";

export const system: Record<string, ActionDefinition> = {
  system_info: {
    description: "Get Docker system information.",
    params: z.object({}),
    returns: z.object({
      server_version: z.string().describe("Docker server version"),
      os: z.string().describe("Operating system"),
      architecture: z.string().describe("Architecture"),
      cpus: z.number().describe("Number of CPUs"),
      memory: z.number().describe("Total memory in bytes"),
      containers_running: z.number().describe("Running containers"),
      containers_stopped: z.number().describe("Stopped containers"),
      images: z.number().describe("Number of images"),
      storage_driver: z.string().describe("Storage driver"),
    }),
    execute: async (_params, ctx) => {
      const i = await dFetch(ctx, `/info`);
      return {
        server_version: i.ServerVersion ?? "",
        os: i.OperatingSystem ?? "",
        architecture: i.Architecture ?? "",
        cpus: i.NCPU ?? 0,
        memory: i.MemTotal ?? 0,
        containers_running: i.ContainersRunning ?? 0,
        containers_stopped: i.ContainersStopped ?? 0,
        images: i.Images ?? 0,
        storage_driver: i.Driver ?? "",
      };
    },
  },

  system_df: {
    description: "Show Docker disk usage.",
    params: z.object({
      verbose: z.boolean().default(false).describe("Show detailed breakdown"),
    }),
    returns: z.object({
      images: z.any().describe("Image disk usage"),
      containers: z.any().describe("Container disk usage"),
      volumes: z.any().describe("Volume disk usage"),
      build_cache: z.any().describe("Build cache disk usage"),
    }),
    execute: async (_params, ctx) => {
      const d = await dFetch(ctx, `/system/df`);
      return { images: d.Images ?? [], containers: d.Containers ?? [], volumes: d.Volumes ?? [], build_cache: d.BuildCache ?? [] };
    },
  },

  system_prune: {
    description: "Remove unused Docker data (containers, images, networks, build cache).",
    params: z.object({
      all: z.boolean().default(false).describe("Remove all unused images, not just dangling"),
      volumes: z.boolean().default(false).describe("Also prune volumes"),
      force: z.boolean().default(false).describe("Skip confirmation"),
      filter: z.string().optional().describe("Filter (e.g. until=24h)"),
    }),
    returns: z.object({ space_reclaimed: z.number().describe("Space reclaimed in bytes") }),
    execute: async (params, ctx) => {
      const qs = new URLSearchParams();
      if (params.filter) {
        const [k, v] = params.filter.split("=");
        qs.set("filters", JSON.stringify({ [k]: [v] }));
      }
      const containers = await dPost(ctx, `/containers/prune?${qs}`);
      const imgQs = new URLSearchParams(qs);
      if (params.all) imgQs.set("filters", JSON.stringify({ ...JSON.parse(qs.get("filters") || "{}"), dangling: ["false"] }));
      const imgs = await dPost(ctx, `/images/prune?${imgQs}`);
      await dPost(ctx, `/networks/prune?${qs}`);
      let volSpace = 0;
      if (params.volumes) {
        const vols = await dPost(ctx, `/volumes/prune?${qs}`);
        volSpace = vols.SpaceReclaimed ?? 0;
      }
      return { space_reclaimed: (containers.SpaceReclaimed ?? 0) + (imgs.SpaceReclaimed ?? 0) + volSpace };
    },
  },
};
