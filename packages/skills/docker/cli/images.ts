import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { dFetch, dPost, dDelete, dockerUrl, hdrs, enc } from "../core/client.ts";

export const images: Record<string, ActionDefinition> = {
  list_images: {
    description: "List images on the Docker host.",
    params: z.object({
      all: z.boolean().default(false).describe("Include intermediate images"),
      filter: z.string().optional().describe("Filter expression (e.g. dangling=true)"),
    }),
    returns: z.array(
      z.object({
        id: z.string().describe("Image ID"),
        repo_tags: z.array(z.string()).describe("Repository tags"),
        size: z.number().describe("Image size in bytes"),
        created: z.number().describe("Unix timestamp"),
      }),
    ),
    execute: async (params, ctx) => {
      const qs = new URLSearchParams({ all: String(params.all) });
      if (params.filter) {
        const [k, v] = params.filter.split("=");
        qs.set("filters", JSON.stringify({ [k]: [v] }));
      }
      const data = await dFetch(ctx, `/images/json?${qs}`);
      return data.map((i: any) => ({ id: i.Id, repo_tags: i.RepoTags ?? [], size: i.Size, created: i.Created }));
    },
  },

  pull: {
    description: "Pull an image from a registry.",
    params: z.object({
      image: z.string().describe("Image to pull (e.g. nginx:1.25)"),
    }),
    returns: z.object({
      status: z.string().describe("Pull status"),
      image: z.string().describe("Pulled image name"),
      digest: z.string().describe("Image digest"),
    }),
    execute: async (params, ctx) => {
      const [repo, tag = "latest"] = params.image.split(":");
      const res = await ctx.fetch(
        dockerUrl(ctx.credentials.host, `/images/create?fromImage=${enc(repo)}&tag=${enc(tag)}`),
        { method: "POST", headers: hdrs() },
      );
      if (!res.ok) throw new Error(`Docker API ${res.status}: ${await res.text()}`);
      const text = await res.text();
      const lines = text.trim().split("\n").map((l) => { try { return JSON.parse(l); } catch { return {}; } });
      const last = lines[lines.length - 1];
      return { status: last?.status ?? "pulled", image: params.image, digest: last?.id ?? "" };
    },
  },

  push: {
    description: "Push an image to a registry.",
    params: z.object({
      image: z.string().describe("Image to push"),
    }),
    returns: z.object({ status: z.string().describe("Push status"), digest: z.string().describe("Image digest") }),
    execute: async (params, ctx) => {
      const [repo, tag = "latest"] = params.image.split(":");
      const res = await ctx.fetch(
        dockerUrl(ctx.credentials.host, `/images/${enc(repo)}/push?tag=${enc(tag)}`),
        { method: "POST", headers: hdrs() },
      );
      if (!res.ok) throw new Error(`Docker API ${res.status}: ${await res.text()}`);
      return { status: "pushed", digest: "" };
    },
  },

  build: {
    description: "Build an image from a Dockerfile.",
    params: z.object({
      path: z.string().describe("Build context path"),
      tag: z.string().describe("Image tag"),
      dockerfile: z.string().default("Dockerfile").describe("Dockerfile path"),
      no_cache: z.boolean().default(false).describe("Build without cache"),
      build_args: z.string().optional().describe("JSON object of build arguments"),
    }),
    returns: z.object({
      image_id: z.string().describe("Built image ID"),
      tag: z.string().describe("Image tag"),
      size: z.number().describe("Image size"),
    }),
    execute: async (params, ctx) => {
      const qs = new URLSearchParams({ t: params.tag, dockerfile: params.dockerfile, nocache: String(params.no_cache) });
      if (params.build_args) qs.set("buildargs", params.build_args);
      const res = await ctx.fetch(dockerUrl(ctx.credentials.host, `/build?${qs}`), {
        method: "POST",
        headers: { "Content-Type": "application/x-tar" },
      });
      if (!res.ok) throw new Error(`Docker API ${res.status}: ${await res.text()}`);
      return { image_id: "", tag: params.tag, size: 0 };
    },
  },

  tag: {
    description: "Tag an image with a new name.",
    params: z.object({
      source: z.string().describe("Source image tag"),
      target: z.string().describe("Target image tag"),
    }),
    returns: z.object({ source: z.string(), target: z.string() }),
    execute: async (params, ctx) => {
      const [repo, tagName = "latest"] = params.target.split(":");
      await dPost(ctx, `/images/${enc(params.source)}/tag?repo=${enc(repo)}&tag=${enc(tagName)}`);
      return { source: params.source, target: params.target };
    },
  },

  rm_image: {
    description: "Remove an image.",
    params: z.object({
      image: z.string().describe("Image ID or tag"),
      force: z.boolean().default(false).describe("Force remove"),
    }),
    returns: z.object({ deleted: z.array(z.any()).describe("Deleted image layers") }),
    execute: async (params, ctx) => {
      const data = await dDelete(ctx, `/images/${enc(params.image)}?force=${params.force}`);
      return { deleted: Array.isArray(data) ? data : [] };
    },
  },

  inspect_image: {
    description: "Inspect an image (full metadata).",
    params: z.object({ image: z.string().describe("Image ID or tag") }),
    returns: z.object({
      config: z.any().describe("Image config"),
      layers: z.any().describe("Image layers"),
      os: z.string().describe("Operating system"),
      architecture: z.string().describe("Architecture"),
      size: z.number().describe("Image size in bytes"),
    }),
    execute: async (params, ctx) => {
      const i = await dFetch(ctx, `/images/${enc(params.image)}/json`);
      return { config: i.Config, layers: i.RootFS?.Layers ?? [], os: i.Os ?? "", architecture: i.Architecture ?? "", size: i.Size ?? 0 };
    },
  },

  history: {
    description: "Show the history of an image (layers).",
    params: z.object({ image: z.string().describe("Image ID or tag") }),
    returns: z.array(z.object({
      created_by: z.string().describe("Layer command"),
      size: z.number().describe("Layer size in bytes"),
      created: z.number().describe("Unix timestamp"),
    })),
    execute: async (params, ctx) => {
      const data = await dFetch(ctx, `/images/${enc(params.image)}/history`);
      return data.map((l: any) => ({ created_by: l.CreatedBy ?? "", size: l.Size ?? 0, created: l.Created ?? 0 }));
    },
  },
};
