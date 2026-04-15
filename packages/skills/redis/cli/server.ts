import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { redisCmd } from "../core/client.ts";

export const server: Record<string, ActionDefinition> = {
  info: {
    description: "Get Redis server information.",
    params: z.object({ section: z.string().optional().describe("Info section: server, memory, stats, clients, etc.") }),
    returns: z.record(z.string()),
    execute: async (params, ctx) => {
      const args = params.section ? [params.section] : [];
      const data = await redisCmd(ctx, "INFO", args);
      const result: Record<string, string> = {};
      if (typeof data.result === "string") {
        for (const line of data.result.split("\n")) {
          const trimmed = line.trim();
          if (trimmed && !trimmed.startsWith("#")) {
            const idx = trimmed.indexOf(":");
            if (idx > 0) result[trimmed.slice(0, idx)] = trimmed.slice(idx + 1);
          }
        }
      } else if (typeof data.result === "object" && data.result != null) {
        return data.result;
      }
      return result;
    },
  },

  dbsize: {
    description: "Get the number of keys in the current database.",
    params: z.object({}),
    returns: z.object({ key_count: z.number() }),
    execute: async (_params, ctx) => {
      const data = await redisCmd(ctx, "DBSIZE", []);
      return { key_count: data.result };
    },
  },

  flushdb: {
    description: "Remove all keys from the current database.",
    params: z.object({ async: z.boolean().default(false).describe("Flush asynchronously in background") }),
    returns: z.object({ ok: z.boolean() }),
    execute: async (params, ctx) => {
      const args = params.async ? ["ASYNC"] : [];
      await redisCmd(ctx, "FLUSHDB", args);
      return { ok: true };
    },
  },

  ping: {
    description: "Ping the Redis server to check connectivity.",
    params: z.object({}),
    returns: z.object({ pong: z.string() }),
    execute: async (_params, ctx) => {
      const data = await redisCmd(ctx, "PING", []);
      return { pong: data.result ?? "PONG" };
    },
  },
};
