import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { pdGet } from "../core/client.ts";

export const oncallsSchedules: Record<string, ActionDefinition> = {
  list_oncalls: {
    description: "List on-call entries across schedules and escalation policies.",
    params: z.object({
      schedule_ids: z.array(z.string()).optional().describe("Filter by schedule IDs"),
      user_ids: z.array(z.string()).optional().describe("Filter by user IDs"),
      escalation_policy_ids: z.array(z.string()).optional().describe("Filter by escalation policy IDs"),
      earliest: z.boolean().optional().describe("Return only the earliest on-call per layer"),
      since: z.string().optional().describe("Start of on-call window (ISO 8601)"),
      until: z.string().optional().describe("End of on-call window (ISO 8601)"),
    }),
    returns: z.array(z.object({
      escalation_policy: z.object({ id: z.string(), summary: z.string().optional() }).optional(),
      escalation_level: z.number().optional(),
      schedule: z.object({ id: z.string(), summary: z.string().optional() }).optional(),
      user: z.object({ id: z.string(), summary: z.string().optional(), name: z.string().optional() }).optional(),
      start: z.string().optional(),
      end: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const qp: Record<string, string | string[]> = {};
      if (params.schedule_ids) qp["schedule_ids"] = params.schedule_ids;
      if (params.user_ids) qp["user_ids"] = params.user_ids;
      if (params.escalation_policy_ids) qp["escalation_policy_ids"] = params.escalation_policy_ids;
      if (params.earliest !== undefined) qp.earliest = String(params.earliest);
      if (params.since) qp.since = params.since;
      if (params.until) qp.until = params.until;
      const data = await pdGet(ctx.fetch, ctx.credentials.api_key, "/oncalls", qp);
      return (data.oncalls ?? []).map((o: any) => ({
        escalation_policy: o.escalation_policy,
        escalation_level: o.escalation_level,
        schedule: o.schedule,
        user: o.user,
        start: o.start,
        end: o.end,
      }));
    },
  },

  list_schedules: {
    description: "List on-call schedules.",
    params: z.object({
      query: z.string().optional().describe("Filter by schedule name"),
      limit: z.number().int().max(100).default(25).describe("Max results"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      name: z.string(),
      time_zone: z.string().optional(),
      description: z.string().optional(),
      html_url: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const qp: Record<string, string> = { limit: String(params.limit) };
      if (params.query) qp.query = params.query;
      const data = await pdGet(ctx.fetch, ctx.credentials.api_key, "/schedules", qp);
      return (data.schedules ?? []).map((s: any) => ({
        id: s.id,
        name: s.name,
        time_zone: s.time_zone,
        description: s.description,
        html_url: s.html_url,
      }));
    },
  },

  get_schedule: {
    description: "Get a schedule and its rendered on-call entries for a time window.",
    params: z.object({
      schedule_id: z.string().describe("Schedule ID"),
      since: z.string().optional().describe("Start of rendered window (ISO 8601)"),
      until: z.string().optional().describe("End of rendered window (ISO 8601)"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      time_zone: z.string().optional(),
      final_schedule: z.object({
        rendered_schedule_entries: z.array(z.object({
          start: z.string().optional(),
          end: z.string().optional(),
          user: z.object({ id: z.string(), summary: z.string().optional() }).optional(),
        })).optional(),
      }).optional(),
    }),
    execute: async (params, ctx) => {
      const qp: Record<string, string> = {};
      if (params.since) qp.since = params.since;
      if (params.until) qp.until = params.until;
      const data = await pdGet(ctx.fetch, ctx.credentials.api_key, `/schedules/${params.schedule_id}`, qp);
      const s = data.schedule;
      return {
        id: s.id,
        name: s.name,
        time_zone: s.time_zone,
        final_schedule: s.final_schedule,
      };
    },
  },

  list_escalation_policies: {
    description: "List escalation policies.",
    params: z.object({
      query: z.string().optional().describe("Filter by policy name"),
      user_ids: z.array(z.string()).optional().describe("Filter policies containing these users"),
      team_ids: z.array(z.string()).optional().describe("Filter by team IDs"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      name: z.string(),
      description: z.string().optional(),
      num_loops: z.number().optional(),
      html_url: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const qp: Record<string, string | string[]> = {};
      if (params.query) qp.query = params.query;
      if (params.user_ids) qp["user_ids"] = params.user_ids;
      if (params.team_ids) qp["team_ids"] = params.team_ids;
      const data = await pdGet(ctx.fetch, ctx.credentials.api_key, "/escalation_policies", qp);
      return (data.escalation_policies ?? []).map((ep: any) => ({
        id: ep.id,
        name: ep.name,
        description: ep.description,
        num_loops: ep.num_loops,
        html_url: ep.html_url,
      }));
    },
  },

  get_escalation_policy: {
    description: "Get details of an escalation policy.",
    params: z.object({
      policy_id: z.string().describe("Escalation policy ID"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      description: z.string().optional(),
      num_loops: z.number().optional(),
      escalation_rules: z.array(z.any()).optional(),
      services: z.array(z.any()).optional(),
      teams: z.array(z.any()).optional(),
    }),
    execute: async (params, ctx) => {
      const data = await pdGet(ctx.fetch, ctx.credentials.api_key, `/escalation_policies/${params.policy_id}`);
      const ep = data.escalation_policy;
      return {
        id: ep.id,
        name: ep.name,
        description: ep.description,
        num_loops: ep.num_loops,
        escalation_rules: ep.escalation_rules,
        services: ep.services,
        teams: ep.teams,
      };
    },
  },
};
