import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { VERCEL_API, vFetch, enc } from "../core/client.ts";

export const logs: Record<string, ActionDefinition> = {
  get_deployment_logs: {
    description: "Get runtime logs for a deployment.",
    params: z.object({
      deployment_id: z.string().describe("Deployment ID"),
    }),
    returns: z.array(
      z.object({
        timestamp: z.number(),
        type: z.string(),
        text: z.string(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await vFetch(
        ctx,
        `${VERCEL_API}/v2/deployments/${enc(params.deployment_id)}/events`,
      );
      return (Array.isArray(data) ? data : []).map((e: any) => ({
        timestamp: e.created ?? e.date ?? 0,
        type: e.type ?? "stdout",
        text: e.text ?? e.payload?.text ?? "",
      }));
    },
  },

  get_build_logs: {
    description: "Get build output logs for a deployment.",
    params: z.object({
      deployment_id: z.string().describe("Deployment ID"),
    }),
    returns: z.array(
      z.object({
        timestamp: z.number(),
        text: z.string(),
      }),
    ),
    execute: async (params, ctx) => {
      const r = await vFetch(ctx, `${VERCEL_API}/v13/deployments/${enc(params.deployment_id)}`);
      const builds = r.builds ?? [];
      const result: { timestamp: number; text: string }[] = [];
      for (const b of builds) {
        for (const entry of b.output ?? []) {
          result.push({ timestamp: entry.created ?? 0, text: entry.text ?? entry.path ?? "" });
        }
      }
      return result;
    },
  },
};
