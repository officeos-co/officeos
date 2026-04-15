import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { doGet, doPost, doDelete, qs } from "../core/client.ts";

export const spaces: Record<string, ActionDefinition> = {
  list_spaces: {
    description: "List Spaces (S3-compatible object storage).",
    params: z.object({
      region: z.string().optional().describe("Filter by region"),
    }),
    returns: z.array(z.object({
      name: z.string().describe("Space name"),
      region: z.string().describe("Region slug"),
      created_at: z.string().describe("Creation timestamp"),
    })),
    execute: async (params, ctx) => {
      const q = qs({ region: params.region });
      const data = await doGet(ctx, `/spaces${q}`);
      return (data.spaces ?? []).map((s: any) => ({
        name: s.name,
        region: s.region,
        created_at: s.created_at ?? "",
      }));
    },
  },

  create_space: {
    description: "Create a new Space.",
    params: z.object({
      name: z.string().describe("Space name"),
      region: z.string().describe("Region slug"),
    }),
    returns: z.object({
      name: z.string().describe("Space name"),
      region: z.string().describe("Region slug"),
      created_at: z.string().describe("Creation timestamp"),
    }),
    execute: async (params, ctx) => {
      const data = await doPost(ctx, "/spaces", { name: params.name, region: params.region });
      const s = data.space ?? data;
      return { name: s.name ?? params.name, region: s.region ?? params.region, created_at: s.created_at ?? new Date().toISOString() };
    },
  },

  delete_space: {
    description: "Delete a Space (must be empty).",
    params: z.object({
      name: z.string().describe("Space name"),
      region: z.string().describe("Region slug"),
    }),
    returns: z.object({
      deleted: z.boolean().describe("Whether deletion succeeded"),
    }),
    execute: async (params, ctx) => {
      await doDelete(ctx, `/spaces/${params.name}?region=${params.region}`);
      return { deleted: true };
    },
  },

  list_space_objects: {
    description: "List objects in a Space.",
    params: z.object({
      space: z.string().describe("Space name"),
      region: z.string().describe("Region slug"),
      prefix: z.string().optional().describe("Key prefix filter"),
      max_keys: z.number().min(1).max(1000).default(1000).describe("Maximum objects to return"),
    }),
    returns: z.array(z.object({
      key: z.string().describe("Object key"),
      size: z.number().describe("Object size in bytes"),
      last_modified: z.string().describe("Last modified date"),
      etag: z.string().describe("ETag"),
    })),
    execute: async (params, ctx) => {
      const q = qs({ prefix: params.prefix, max_keys: params.max_keys });
      const data = await doGet(ctx, `/spaces/${params.space}/objects${q}`);
      return (data.objects ?? []).map((o: any) => ({
        key: o.key,
        size: o.size ?? 0,
        last_modified: o.last_modified ?? "",
        etag: o.etag ?? "",
      }));
    },
  },

  put_space_object: {
    description: "Upload an object to a Space.",
    params: z.object({
      space: z.string().describe("Space name"),
      region: z.string().describe("Region slug"),
      key: z.string().describe("Object key"),
      body: z.string().describe("Object content"),
      content_type: z.string().default("application/octet-stream").describe("MIME type"),
      acl: z.enum(["private", "public-read"]).default("private").describe("Access control"),
    }),
    returns: z.object({
      etag: z.string().describe("ETag of stored object"),
      key: z.string().describe("Object key"),
    }),
    execute: async (params, ctx) => {
      const data = await doPost(ctx, `/spaces/${params.space}/objects/${params.key}`, {
        body: params.body,
        content_type: params.content_type,
        acl: params.acl,
        region: params.region,
      });
      return { etag: data?.etag ?? "", key: params.key };
    },
  },

  get_space_object: {
    description: "Get an object from a Space.",
    params: z.object({
      space: z.string().describe("Space name"),
      region: z.string().describe("Region slug"),
      key: z.string().describe("Object key"),
    }),
    returns: z.object({
      content_type: z.string().describe("Content type"),
      content_length: z.number().describe("Content length"),
      last_modified: z.string().describe("Last modified date"),
      body: z.string().describe("Object content (text or base64 for binary)"),
    }),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/spaces/${params.space}/objects/${params.key}?region=${params.region}`);
      return {
        content_type: data.content_type ?? "application/octet-stream",
        content_length: data.content_length ?? 0,
        last_modified: data.last_modified ?? "",
        body: data.body ?? "",
      };
    },
  },

  delete_space_object: {
    description: "Delete an object from a Space.",
    params: z.object({
      space: z.string().describe("Space name"),
      region: z.string().describe("Region slug"),
      key: z.string().describe("Object key"),
    }),
    returns: z.object({
      deleted: z.boolean().describe("Whether deletion succeeded"),
    }),
    execute: async (params, ctx) => {
      await doDelete(ctx, `/spaces/${params.space}/objects/${params.key}?region=${params.region}`);
      return { deleted: true };
    },
  },
};
