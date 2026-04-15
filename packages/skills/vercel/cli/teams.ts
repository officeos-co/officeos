import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { VERCEL_API, vFetch, enc } from "../core/client.ts";

export const teams: Record<string, ActionDefinition> = {
  list_teams: {
    description: "List teams the authenticated user belongs to.",
    params: z.object({
      limit: z.number().min(1).max(100).default(20).describe("Results per page"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        name: z.string(),
        slug: z.string(),
        createdAt: z.number(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await vFetch(ctx, `${VERCEL_API}/v2/teams?limit=${params.limit}`);
      return (data.teams ?? []).map((t: any) => ({
        id: t.id,
        name: t.name,
        slug: t.slug,
        createdAt: t.createdAt ?? t.created,
      }));
    },
  },

  get_team: {
    description: "Get detailed information about a team.",
    params: z.object({
      team_id: z.string().describe("Team ID"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      slug: z.string(),
      createdAt: z.number(),
      updatedAt: z.number(),
    }),
    execute: async (params, ctx) => {
      const r = await vFetch(ctx, `${VERCEL_API}/v2/teams/${enc(params.team_id)}`);
      return {
        id: r.id,
        name: r.name,
        slug: r.slug,
        createdAt: r.createdAt ?? r.created,
        updatedAt: r.updatedAt ?? r.createdAt ?? 0,
      };
    },
  },
};
