import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { redisCmd } from "../core/client.ts";

export const hashes: Record<string, ActionDefinition> = {
  hget: {
    description: "Get the value of a hash field.",
    params: z.object({ key: z.string().describe("Hash key"), field: z.string().describe("Field to retrieve") }),
    returns: z.object({ value: z.string().nullable() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "HGET", [params.key, params.field]);
      return { value: data.result ?? null };
    },
  },

  hset: {
    description: "Set one or more fields in a hash.",
    params: z.object({ key: z.string().describe("Hash key"), fields: z.string().describe("JSON object of field-value pairs to set") }),
    returns: z.object({ added_count: z.number() }),
    execute: async (params, ctx) => {
      const obj = JSON.parse(params.fields);
      const args: string[] = [params.key];
      for (const [f, v] of Object.entries(obj)) args.push(f, String(v));
      const data = await redisCmd(ctx, "HSET", args);
      return { added_count: data.result };
    },
  },

  hgetall: {
    description: "Get all fields and values of a hash.",
    params: z.object({ key: z.string().describe("Hash key") }),
    returns: z.record(z.string()),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "HGETALL", [params.key]);
      return data.result ?? {};
    },
  },

  hdel: {
    description: "Delete one or more fields from a hash.",
    params: z.object({ key: z.string().describe("Hash key"), fields: z.string().describe("JSON array of fields to delete") }),
    returns: z.object({ removed_count: z.number() }),
    execute: async (params, ctx) => {
      const fields: string[] = JSON.parse(params.fields);
      const data = await redisCmd(ctx, "HDEL", [params.key, ...fields]);
      return { removed_count: data.result };
    },
  },

  hkeys: {
    description: "Get all field names of a hash.",
    params: z.object({ key: z.string().describe("Hash key") }),
    returns: z.array(z.string()),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "HKEYS", [params.key]);
      return data.result ?? [];
    },
  },

  hvals: {
    description: "Get all field values of a hash.",
    params: z.object({ key: z.string().describe("Hash key") }),
    returns: z.array(z.string()),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "HVALS", [params.key]);
      return data.result ?? [];
    },
  },
};
