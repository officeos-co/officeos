import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { redisCmd } from "../core/client.ts";

export const sets: Record<string, ActionDefinition> = {
  sadd: {
    description: "Add members to a set.",
    params: z.object({ key: z.string().describe("Set key"), members: z.string().describe("JSON array of members to add") }),
    returns: z.object({ added_count: z.number() }),
    execute: async (params, ctx) => {
      const members: string[] = JSON.parse(params.members);
      const data = await redisCmd(ctx, "SADD", [params.key, ...members]);
      return { added_count: data.result };
    },
  },

  srem: {
    description: "Remove members from a set.",
    params: z.object({ key: z.string().describe("Set key"), members: z.string().describe("JSON array of members to remove") }),
    returns: z.object({ removed_count: z.number() }),
    execute: async (params, ctx) => {
      const members: string[] = JSON.parse(params.members);
      const data = await redisCmd(ctx, "SREM", [params.key, ...members]);
      return { removed_count: data.result };
    },
  },

  smembers: {
    description: "Get all members of a set.",
    params: z.object({ key: z.string().describe("Set key") }),
    returns: z.array(z.string()),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "SMEMBERS", [params.key]);
      return data.result ?? [];
    },
  },

  sismember: {
    description: "Check if a value is a member of a set.",
    params: z.object({ key: z.string().describe("Set key"), member: z.string().describe("Member to check") }),
    returns: z.object({ is_member: z.boolean() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "SISMEMBER", [params.key, params.member]);
      return { is_member: data.result === 1 || data.result === true };
    },
  },

  scard: {
    description: "Get the number of members in a set.",
    params: z.object({ key: z.string().describe("Set key") }),
    returns: z.object({ count: z.number() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "SCARD", [params.key]);
      return { count: data.result };
    },
  },

  sunion: {
    description: "Get the union of multiple sets.",
    params: z.object({ keys: z.string().describe("JSON array of set keys") }),
    returns: z.array(z.string()),
    execute: async (params, ctx) => {
      const keys: string[] = JSON.parse(params.keys);
      const data = await redisCmd(ctx, "SUNION", keys);
      return data.result ?? [];
    },
  },

  sinter: {
    description: "Get the intersection of multiple sets.",
    params: z.object({ keys: z.string().describe("JSON array of set keys") }),
    returns: z.array(z.string()),
    execute: async (params, ctx) => {
      const keys: string[] = JSON.parse(params.keys);
      const data = await redisCmd(ctx, "SINTER", keys);
      return data.result ?? [];
    },
  },

  sdiff: {
    description: "Get the difference of multiple sets (first set minus the rest).",
    params: z.object({ keys: z.string().describe("JSON array of set keys (first set minus the rest)") }),
    returns: z.array(z.string()),
    execute: async (params, ctx) => {
      const keys: string[] = JSON.parse(params.keys);
      const data = await redisCmd(ctx, "SDIFF", keys);
      return data.result ?? [];
    },
  },
};
