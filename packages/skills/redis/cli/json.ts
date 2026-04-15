import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { redisCmd } from "../core/client.ts";

export const json: Record<string, ActionDefinition> = {
  json_set: {
    description: "Set a JSON value at a key and path (requires RedisJSON module).",
    params: z.object({
      key: z.string().describe("Key to store JSON document"),
      path: z.string().default("$").describe("JSONPath expression for nested set"),
      value: z.string().describe("JSON value to store"),
      nx: z.boolean().default(false).describe("Only set if path does not exist"),
      xx: z.boolean().default(false).describe("Only set if path already exists"),
    }),
    returns: z.object({ ok: z.boolean() }),
    execute: async (params, ctx) => {
      const args: unknown[] = [params.key, params.path, params.value];
      if (params.nx) args.push("NX");
      if (params.xx) args.push("XX");
      await redisCmd(ctx, "JSON.SET", args);
      return { ok: true };
    },
  },

  json_get: {
    description: "Get a JSON value at a key and path (requires RedisJSON module).",
    params: z.object({
      key: z.string().describe("Key containing the JSON document"),
      path: z.string().default("$").describe("JSONPath expression to retrieve"),
    }),
    returns: z.object({ value: z.unknown() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "JSON.GET", [params.key, params.path]);
      let value = data.result;
      if (typeof value === "string") {
        try { value = JSON.parse(value); } catch { /* keep as string */ }
      }
      return { value };
    },
  },
};
