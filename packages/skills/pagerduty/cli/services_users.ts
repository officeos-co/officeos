import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { pdGet } from "../core/client.ts";

export const servicesUsers: Record<string, ActionDefinition> = {
  list_services: {
    description: "List services in the PagerDuty account.",
    params: z.object({
      team_ids: z.array(z.string()).optional().describe("Filter by team IDs"),
      query: z.string().optional().describe("Search by service name"),
      limit: z.number().int().max(100).default(25).describe("Max results"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      name: z.string(),
      status: z.string().optional(),
      description: z.string().optional(),
      html_url: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const qp: Record<string, string | string[]> = { limit: String(params.limit) };
      if (params.team_ids) qp["team_ids"] = params.team_ids;
      if (params.query) qp.query = params.query;
      const data = await pdGet(ctx.fetch, ctx.credentials.api_key, "/services", qp);
      return (data.services ?? []).map((s: any) => ({
        id: s.id,
        name: s.name,
        status: s.status,
        description: s.description,
        html_url: s.html_url,
      }));
    },
  },

  get_service: {
    description: "Get details of a specific service.",
    params: z.object({
      service_id: z.string().describe("Service ID"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      status: z.string().optional(),
      description: z.string().optional(),
      escalation_policy: z.object({ id: z.string(), summary: z.string().optional() }).optional(),
      teams: z.array(z.object({ id: z.string(), summary: z.string().optional() })).optional(),
      html_url: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const data = await pdGet(ctx.fetch, ctx.credentials.api_key, `/services/${params.service_id}`);
      const s = data.service;
      return {
        id: s.id,
        name: s.name,
        status: s.status,
        description: s.description,
        escalation_policy: s.escalation_policy,
        teams: s.teams,
        html_url: s.html_url,
      };
    },
  },

  list_users: {
    description: "List users in the PagerDuty account.",
    params: z.object({
      query: z.string().optional().describe("Search by name or email"),
      team_ids: z.array(z.string()).optional().describe("Filter by team IDs"),
      limit: z.number().int().max(100).default(25).describe("Max results"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      name: z.string(),
      email: z.string(),
      role: z.string().optional(),
      html_url: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const qp: Record<string, string | string[]> = { limit: String(params.limit) };
      if (params.query) qp.query = params.query;
      if (params.team_ids) qp["team_ids"] = params.team_ids;
      const data = await pdGet(ctx.fetch, ctx.credentials.api_key, "/users", qp);
      return (data.users ?? []).map((u: any) => ({
        id: u.id,
        name: u.name,
        email: u.email,
        role: u.role,
        html_url: u.html_url,
      }));
    },
  },

  get_user: {
    description: "Get details of a specific user.",
    params: z.object({
      user_id: z.string().describe("User ID"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      email: z.string(),
      role: z.string().optional(),
      teams: z.array(z.any()).optional(),
      html_url: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const data = await pdGet(ctx.fetch, ctx.credentials.api_key, `/users/${params.user_id}`);
      const u = data.user;
      return { id: u.id, name: u.name, email: u.email, role: u.role, teams: u.teams, html_url: u.html_url };
    },
  },
};
