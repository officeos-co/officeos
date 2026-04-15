import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { redisCmd } from "../core/client.ts";

export const sorted_sets: Record<string, ActionDefinition> = {
  zadd: {
    description: "Add members with scores to a sorted set.",
    params: z.object({
      key: z.string().describe("Sorted set key"),
      members: z.string().describe("JSON array of {score, value} objects"),
      nx: z.boolean().optional().describe("Only add new elements, do not update existing scores"),
      xx: z.boolean().optional().describe("Only update existing elements, do not add new ones"),
    }),
    returns: z.object({ added_count: z.number() }),
    execute: async (params, ctx) => {
      const members: { score: number; value: string }[] = JSON.parse(params.members);
      const args: unknown[] = [params.key];
      if (params.nx) args.push("NX");
      if (params.xx) args.push("XX");
      for (const m of members) args.push(m.score, m.value);
      const data = await redisCmd(ctx, "ZADD", args);
      return { added_count: data.result };
    },
  },

  zrem: {
    description: "Remove members from a sorted set.",
    params: z.object({ key: z.string().describe("Sorted set key"), members: z.string().describe("JSON array of members to remove") }),
    returns: z.object({ removed_count: z.number() }),
    execute: async (params, ctx) => {
      const members: string[] = JSON.parse(params.members);
      const data = await redisCmd(ctx, "ZREM", [params.key, ...members]);
      return { removed_count: data.result };
    },
  },

  zrange: {
    description: "Get members of a sorted set by rank range.",
    params: z.object({
      key: z.string().describe("Sorted set key"),
      start: z.number().default(0).describe("Start rank (0-based)"),
      stop: z.number().default(-1).describe("Stop rank (inclusive, -1 = end)"),
      rev: z.boolean().default(false).describe("Reverse order (highest score first)"),
      withscores: z.boolean().default(false).describe("Include scores in output"),
    }),
    returns: z.array(z.unknown()),
    execute: async (params, ctx) => {
      const cmd = params.rev ? "ZREVRANGE" : "ZRANGE";
      const args: unknown[] = [params.key, params.start, params.stop];
      if (params.withscores) args.push("WITHSCORES");
      const data = await redisCmd(ctx, cmd, args);
      return data.result ?? [];
    },
  },

  zrangebyscore: {
    description: "Get members of a sorted set by score range.",
    params: z.object({
      key: z.string().describe("Sorted set key"),
      min: z.string().default("-inf").describe("Minimum score (use '-inf' for no lower bound)"),
      max: z.string().default("+inf").describe("Maximum score (use '+inf' for no upper bound)"),
      withscores: z.boolean().default(false).describe("Include scores in output"),
      offset: z.number().default(0).describe("Number of results to skip"),
      limit: z.number().optional().describe("Maximum number of results"),
    }),
    returns: z.array(z.unknown()),
    execute: async (params, ctx) => {
      const args: unknown[] = [params.key, params.min, params.max];
      if (params.withscores) args.push("WITHSCORES");
      if (params.limit != null) args.push("LIMIT", params.offset, params.limit);
      const data = await redisCmd(ctx, "ZRANGEBYSCORE", args);
      return data.result ?? [];
    },
  },

  zscore: {
    description: "Get the score of a member in a sorted set.",
    params: z.object({ key: z.string().describe("Sorted set key"), member: z.string().describe("Member to look up") }),
    returns: z.object({ score: z.number().nullable() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "ZSCORE", [params.key, params.member]);
      return { score: data.result != null ? Number(data.result) : null };
    },
  },

  zcard: {
    description: "Get the number of members in a sorted set.",
    params: z.object({ key: z.string().describe("Sorted set key") }),
    returns: z.object({ count: z.number() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "ZCARD", [params.key]);
      return { count: data.result };
    },
  },
};
