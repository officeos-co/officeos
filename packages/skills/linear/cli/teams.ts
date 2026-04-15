import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gql } from "../core/client.ts";

export const teams: Record<string, ActionDefinition> = {
  list_teams: {
    description: "List all teams.",
    params: z.object({}),
    returns: z.array(z.object({
      id: z.string().describe("Team UUID"),
      name: z.string().describe("Team name"),
      key: z.string().describe("Team key"),
      description: z.string().nullable().describe("Team description"),
      members_count: z.number().describe("Number of members"),
    })),
    execute: async (_params, ctx) => {
      const data = await gql(ctx, `query { teams { nodes { id name key description members { nodes { id } } } } }`);
      return (data.teams.nodes ?? []).map((t: any) => ({
        id: t.id,
        name: t.name,
        key: t.key,
        description: t.description ?? null,
        members_count: t.members?.nodes?.length ?? 0,
      }));
    },
  },

  get_team: {
    description: "Get detailed team information.",
    params: z.object({
      team_id: z.string().describe("Team UUID"),
    }),
    returns: z.object({
      id: z.string().describe("Team UUID"),
      name: z.string().describe("Team name"),
      key: z.string().describe("Team key"),
      description: z.string().nullable().describe("Team description"),
      members: z.array(z.string()).describe("Member names"),
      states: z.array(z.string()).describe("Workflow state names"),
      labels: z.array(z.string()).describe("Label names"),
      cycles_enabled: z.boolean().describe("Whether cycles are enabled"),
    }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($id: String!) { team(id: $id) { id name key description members { nodes { name } } states { nodes { name } } labels { nodes { name } } cycles { nodes { id } } } }`, { id: params.team_id });
      const t = data.team;
      return {
        id: t.id,
        name: t.name,
        key: t.key,
        description: t.description ?? null,
        members: (t.members?.nodes ?? []).map((m: any) => m.name),
        states: (t.states?.nodes ?? []).map((s: any) => s.name),
        labels: (t.labels?.nodes ?? []).map((l: any) => l.name),
        cycles_enabled: (t.cycles?.nodes?.length ?? 0) > 0,
      };
    },
  },

  list_workflow_states: {
    description: "List workflow states for a team.",
    params: z.object({
      team_id: z.string().describe("Team UUID"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("State UUID"),
      name: z.string().describe("State name"),
      type: z.string().describe("State type (triage, backlog, unstarted, started, completed, canceled)"),
      color: z.string().describe("Hex color"),
      position: z.number().describe("Display position"),
    })),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($id: String!) { team(id: $id) { states { nodes { id name type color position } } } }`, { id: params.team_id });
      return (data.team.states.nodes ?? []).map((s: any) => ({
        id: s.id,
        name: s.name,
        type: s.type,
        color: s.color,
        position: s.position,
      }));
    },
  },
};
