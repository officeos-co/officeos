import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { redisCmd } from "../core/client.ts";

export const keys: Record<string, ActionDefinition> = {
  keys: {
    description: "Find keys matching a glob-style pattern.",
    params: z.object({ pattern: z.string().default("*").describe("Glob-style key pattern") }),
    returns: z.array(z.string()),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "KEYS", [params.pattern]);
      return data.result ?? [];
    },
  },

  del: {
    description: "Delete one or more keys.",
    params: z.object({ keys: z.string().describe("JSON array of keys to delete") }),
    returns: z.object({ deleted_count: z.number() }),
    execute: async (params, ctx) => {
      const ks: string[] = JSON.parse(params.keys);
      const data = await redisCmd(ctx, "DEL", ks);
      return { deleted_count: data.result };
    },
  },

  exists: {
    description: "Check if a key exists.",
    params: z.object({ key: z.string().describe("Key to check") }),
    returns: z.object({ exists: z.boolean() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "EXISTS", [params.key]);
      return { exists: data.result === 1 || data.result === true };
    },
  },

  expire: {
    description: "Set a time-to-live on a key.",
    params: z.object({ key: z.string().describe("Key to set expiry on"), seconds: z.number().describe("Time-to-live in seconds") }),
    returns: z.object({ ok: z.boolean() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "EXPIRE", [params.key, params.seconds]);
      return { ok: data.result === 1 || data.result === true };
    },
  },

  ttl: {
    description: "Get the remaining time-to-live of a key.",
    params: z.object({ key: z.string().describe("Key to check") }),
    returns: z.object({ ttl: z.number() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "TTL", [params.key]);
      return { ttl: data.result };
    },
  },

  type: {
    description: "Get the data type of a key.",
    params: z.object({ key: z.string().describe("Key to check") }),
    returns: z.object({ type: z.string() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "TYPE", [params.key]);
      return { type: data.result };
    },
  },

  rename: {
    description: "Rename a key.",
    params: z.object({ key: z.string().describe("Current key name"), new_key: z.string().describe("New key name") }),
    returns: z.object({ ok: z.boolean() }),
    execute: async (params, ctx) => {
      await redisCmd(ctx, "RENAME", [params.key, params.new_key]);
      return { ok: true };
    },
  },
};
