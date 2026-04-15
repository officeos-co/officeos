import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { redisCmd } from "../core/client.ts";

export const lists: Record<string, ActionDefinition> = {
  lpush: {
    description: "Push values to the left (head) of a list.",
    params: z.object({ key: z.string().describe("List key"), values: z.string().describe("JSON array of values to push left") }),
    returns: z.object({ length: z.number() }),
    execute: async (params, ctx) => {
      const vals: string[] = JSON.parse(params.values);
      const data = await redisCmd(ctx, "LPUSH", [params.key, ...vals]);
      return { length: data.result };
    },
  },

  rpush: {
    description: "Push values to the right (tail) of a list.",
    params: z.object({ key: z.string().describe("List key"), values: z.string().describe("JSON array of values to push right") }),
    returns: z.object({ length: z.number() }),
    execute: async (params, ctx) => {
      const vals: string[] = JSON.parse(params.values);
      const data = await redisCmd(ctx, "RPUSH", [params.key, ...vals]);
      return { length: data.result };
    },
  },

  lpop: {
    description: "Pop elements from the left (head) of a list.",
    params: z.object({ key: z.string().describe("List key"), count: z.number().default(1).describe("Number of elements to pop") }),
    returns: z.object({ values: z.array(z.string()) }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "LPOP", [params.key, params.count]);
      const result = data.result;
      return { values: Array.isArray(result) ? result : result != null ? [result] : [] };
    },
  },

  rpop: {
    description: "Pop elements from the right (tail) of a list.",
    params: z.object({ key: z.string().describe("List key"), count: z.number().default(1).describe("Number of elements to pop") }),
    returns: z.object({ values: z.array(z.string()) }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "RPOP", [params.key, params.count]);
      const result = data.result;
      return { values: Array.isArray(result) ? result : result != null ? [result] : [] };
    },
  },

  lrange: {
    description: "Get a range of elements from a list.",
    params: z.object({
      key: z.string().describe("List key"),
      start: z.number().default(0).describe("Start index (0-based, negative from end)"),
      stop: z.number().default(-1).describe("Stop index (inclusive, -1 = end)"),
    }),
    returns: z.array(z.string()),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "LRANGE", [params.key, params.start, params.stop]);
      return data.result ?? [];
    },
  },

  llen: {
    description: "Get the length of a list.",
    params: z.object({ key: z.string().describe("List key") }),
    returns: z.object({ length: z.number() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "LLEN", [params.key]);
      return { length: data.result };
    },
  },
};
