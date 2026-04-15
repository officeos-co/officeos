import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { redisCmd, redisSubscribe } from "../core/client.ts";

export const pubsub: Record<string, ActionDefinition> = {
  publish: {
    description: "Publish a message to a channel.",
    params: z.object({
      channel: z.string().describe("Channel to publish to"),
      message: z.string().describe("Message payload (string)"),
    }),
    returns: z.object({ receivers: z.number() }),
    execute: async (params, ctx) => {
      const data = await redisCmd(ctx, "PUBLISH", [params.channel, params.message]);
      return { receivers: data.result };
    },
  },

  subscribe: {
    description: "Subscribe to channels and collect messages for a limited time.",
    params: z.object({
      channels: z.string().describe("JSON array of channels to subscribe to"),
      timeout: z.number().default(10).describe("Max seconds to listen before returning messages"),
    }),
    returns: z.array(z.object({ channel: z.string(), message: z.string() })),
    execute: async (params, ctx) => {
      const channels: string[] = JSON.parse(params.channels);
      const data = await redisSubscribe(ctx, channels, params.timeout);
      return data.messages ?? [];
    },
  },
};
