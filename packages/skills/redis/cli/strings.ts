import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { redisCmd, redisConnect } from "../core/client.ts";

export const strings: Record<string, ActionDefinition> = {
  connect: {
    description: "Test connection to a Redis server.",
    params: z.object({
      host: z.string().default("localhost").describe("Redis server hostname"),
      port: z.number().default(6379).describe("Redis server port"),
      password: z.string().optional().describe("Authentication password"),
      db: z.number().default(0).describe("Database index (0-15)"),
      tls: z.boolean().default(false).describe("Enable TLS connection"),
    }),
    returns: z.object({ connected: z.boolean(), redis_version: z.string(), db: z.number() }),
    execute: async (params, ctx) => {
      const data = await redisConnect(ctx, params.host, params.port, params.password, params.db, params.tls);
      const versionMatch = typeof data.result === "string" ? data.result.match(/redis_version:(\S+)/) : null;
      return { connected: true, redis_version: versionMatch?.[1] ?? "unknown", db: params.db };
    },
  },

  get: {
    description: "Get the value of a key.",
    params: z.object({ key: z.string().describe("Key to read") }),
    returns: z.object({ value: z.string().nullable() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "GET", [params.key]);
      return { value: data.result ?? null };
    },
  },

  set: {
    description: "Set the value of a key with optional expiry and conditions.",
    params: z.object({
      key: z.string().describe("Key to set"),
      value: z.string().describe("Value to store"),
      ex: z.number().optional().describe("Expire after N seconds"),
      px: z.number().optional().describe("Expire after N milliseconds"),
      nx: z.boolean().default(false).describe("Only set if key does not exist"),
      xx: z.boolean().default(false).describe("Only set if key already exists"),
    }),
    returns: z.object({ ok: z.boolean(), key: z.string() }),
    execute: async (params, ctx) => {
      const args: unknown[] = [params.key, params.value];
      if (params.ex != null) args.push("EX", params.ex);
      if (params.px != null) args.push("PX", params.px);
      if (params.nx) args.push("NX");
      if (params.xx) args.push("XX");
      const data = await redisCmd(ctx, "SET", args);
      return { ok: data.result === "OK" || data.result != null, key: params.key };
    },
  },

  mget: {
    description: "Get the values of multiple keys.",
    params: z.object({ keys: z.string().describe("JSON array of keys to retrieve") }),
    returns: z.array(z.object({ key: z.string(), value: z.string().nullable() })),
    execute: async (params, ctx) => {
      const keys: string[] = JSON.parse(params.keys);
      const data = await redisCmd(ctx, "MGET", keys);
      const values: (string | null)[] = data.result ?? [];
      return keys.map((k, i) => ({ key: k, value: values[i] ?? null }));
    },
  },

  mset: {
    description: "Set multiple key-value pairs at once.",
    params: z.object({ entries: z.string().describe("JSON object of key-value pairs to set") }),
    returns: z.object({ ok: z.boolean(), count: z.number() }),
    execute: async (params, ctx) => {
      const obj = JSON.parse(params.entries);
      const args: string[] = [];
      for (const [k, v] of Object.entries(obj)) args.push(k, String(v));
      await redisCmd(ctx, "MSET", args);
      return { ok: true, count: Object.keys(obj).length };
    },
  },

  incr: {
    description: "Increment the integer value of a key.",
    params: z.object({ key: z.string().describe("Key to increment"), by: z.number().default(1).describe("Increment amount") }),
    returns: z.object({ value: z.number() }),
    execute: async (params, ctx) => {
      const data = params.by === 1
        ? await redisCmd(ctx, "INCR", [params.key])
        : await redisCmd(ctx, "INCRBY", [params.key, params.by]);
      return { value: data.result };
    },
  },

  decr: {
    description: "Decrement the integer value of a key.",
    params: z.object({ key: z.string().describe("Key to decrement"), by: z.number().default(1).describe("Decrement amount") }),
    returns: z.object({ value: z.number() }),
    execute: async (params, ctx) => {
      const data = params.by === 1
        ? await redisCmd(ctx, "DECR", [params.key])
        : await redisCmd(ctx, "DECRBY", [params.key, params.by]);
      return { value: data.result };
    },
  },

  append: {
    description: "Append a value to a key.",
    params: z.object({ key: z.string().describe("Key to append to"), value: z.string().describe("Value to append") }),
    returns: z.object({ length: z.number() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "APPEND", [params.key, params.value]);
      return { length: data.result };
    },
  },
};
