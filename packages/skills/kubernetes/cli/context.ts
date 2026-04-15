import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";

export const context: Record<string, ActionDefinition> = {
  list_contexts: {
    description: "List available kubeconfig contexts (returns current connection info).",
    params: z.object({}),
    returns: z.array(
      z.object({
        name: z.string().describe("Context name"),
        cluster: z.string().describe("Cluster name"),
        user: z.string().describe("User name"),
        namespace: z.string().describe("Default namespace"),
        current: z.boolean().describe("Whether this is the active context"),
      }),
    ),
    execute: async (_params, ctx) => {
      return [{
        name: "current",
        cluster: ctx.credentials.api_url,
        user: "token-auth",
        namespace: ctx.credentials.namespace ?? "default",
        current: true,
      }];
    },
  },

  use_context: {
    description: "Switch kubeconfig context (not applicable with direct API access).",
    params: z.object({
      name: z.string().describe("Context name"),
    }),
    returns: z.object({ name: z.string() }),
    execute: async (params, _ctx) => {
      return { name: params.name };
    },
  },

  current_context: {
    description: "Get the current kubeconfig context.",
    params: z.object({}),
    returns: z.object({
      name: z.string(),
      cluster: z.string(),
      user: z.string(),
      namespace: z.string(),
    }),
    execute: async (_params, ctx) => {
      return {
        name: "current",
        cluster: ctx.credentials.api_url,
        user: "token-auth",
        namespace: ctx.credentials.namespace ?? "default",
      };
    },
  },
};
