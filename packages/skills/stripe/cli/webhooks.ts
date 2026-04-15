import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { stripeGet, stripePost } from "../core/client.ts";

export const webhooks: Record<string, ActionDefinition> = {
  list_webhook_endpoints: {
    description: "List webhook endpoints.",
    params: z.object({
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        url: z.string(),
        status: z.string(),
        enabled_events: z.array(z.string()),
        created: z.number(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await stripeGet(ctx, "/webhook_endpoints", { limit: String(params.limit) });
      return data.data.map((w: any) => ({
        id: w.id,
        url: w.url,
        status: w.status,
        enabled_events: w.enabled_events ?? [],
        created: w.created,
      }));
    },
  },

  create_webhook_endpoint: {
    description: "Create a new webhook endpoint to receive events.",
    params: z.object({
      url: z.string().describe("Endpoint URL to receive events"),
      events: z.array(z.string()).describe("List of event types to subscribe to"),
      description: z.string().optional().describe("Endpoint description"),
    }),
    returns: z.object({
      id: z.string(),
      url: z.string(),
      status: z.string(),
      secret: z.string().describe("Webhook signing secret (shown only once)"),
      enabled_events: z.array(z.string()),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = { url: params.url };
      params.events.forEach((e, i) => {
        body[`enabled_events[${i}]`] = e;
      });
      if (params.description) body.description = params.description;
      const w = await stripePost(ctx, "/webhook_endpoints", body);
      return {
        id: w.id,
        url: w.url,
        status: w.status,
        secret: w.secret,
        enabled_events: w.enabled_events ?? [],
      };
    },
  },
};
