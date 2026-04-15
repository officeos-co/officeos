import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sentryFetch, sentryPost, enc } from "../core/client.ts";

export const projects: Record<string, ActionDefinition> = {
  list_projects: {
    description: "List all projects in the organization.",
    params: z.object({}),
    returns: z.array(z.object({
      id: z.string().describe("Project ID"),
      slug: z.string().describe("Project slug"),
      name: z.string().describe("Project name"),
      platform: z.string().nullable().describe("Platform"),
      status: z.string().describe("Project status"),
      date_created: z.string().describe("Creation date"),
    })),
    execute: async (_params, ctx) => {
      const org = ctx.credentials.organization;
      const data = await sentryFetch(ctx, `/organizations/${enc(org)}/projects/`);
      return data.map((p: any) => ({
        id: p.id,
        slug: p.slug,
        name: p.name,
        platform: p.platform ?? null,
        status: p.status,
        date_created: p.dateCreated,
      }));
    },
  },

  get_project: {
    description: "Get detailed information about a project.",
    params: z.object({
      project: z.string().describe("Project slug"),
    }),
    returns: z.object({
      id: z.string().describe("Project ID"),
      slug: z.string().describe("Project slug"),
      name: z.string().describe("Project name"),
      platform: z.string().nullable().describe("Platform"),
      status: z.string().describe("Project status"),
      dsn: z.string().nullable().describe("DSN for the project"),
      team: z.string().nullable().describe("Associated team slug"),
      date_created: z.string().describe("Creation date"),
      features: z.array(z.string()).describe("Enabled features"),
    }),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      const p = await sentryFetch(ctx, `/projects/${enc(org)}/${enc(params.project)}/`);
      return {
        id: p.id,
        slug: p.slug,
        name: p.name,
        platform: p.platform ?? null,
        status: p.status,
        dsn: p.keys?.[0]?.dsn?.public ?? null,
        team: p.team?.slug ?? null,
        date_created: p.dateCreated,
        features: p.features ?? [],
      };
    },
  },

  create_project: {
    description: "Create a new project in the organization.",
    params: z.object({
      team: z.string().describe("Team slug"),
      name: z.string().describe("Project name"),
      platform: z.string().optional().describe("Platform (e.g. node, python, csharp)"),
    }),
    returns: z.object({
      id: z.string().describe("Project ID"),
      slug: z.string().describe("Project slug"),
      name: z.string().describe("Project name"),
      dsn: z.string().nullable().describe("DSN for the project"),
    }),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      const body: any = { name: params.name };
      if (params.platform) body.platform = params.platform;
      const p = await sentryPost(ctx, `/teams/${enc(org)}/${enc(params.team)}/projects/`, body);
      return { id: p.id, slug: p.slug, name: p.name, dsn: p.keys?.[0]?.dsn?.public ?? null };
    },
  },

  get_project_stats: {
    description: "Get event count time series for a project.",
    params: z.object({
      project: z.string().describe("Project slug"),
      stat: z.enum(["received", "rejected", "blacklisted"]).default("received").describe("Stat type"),
      resolution: z.string().default("1h").describe("Bucket resolution (e.g. 1h, 1d)"),
      since: z.string().default("-24h").describe("Start time (relative or ISO 8601)"),
      until: z.string().optional().describe("End time"),
    }),
    returns: z.array(z.object({
      timestamp: z.number().describe("Unix timestamp"),
      count: z.number().describe("Event count"),
    })),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      let url = `/projects/${enc(org)}/${enc(params.project)}/stats/?stat=${params.stat}&resolution=${enc(params.resolution)}&since=${enc(params.since)}`;
      if (params.until) url += `&until=${enc(params.until)}`;
      const data = await sentryFetch(ctx, url);
      return data.map((point: any) => ({ timestamp: point[0], count: point[1] }));
    },
  },
};
