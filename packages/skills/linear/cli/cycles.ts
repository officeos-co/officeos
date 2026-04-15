import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gql } from "../core/client.ts";

export const cycles: Record<string, ActionDefinition> = {
  list_cycles: {
    description: "List cycles for a team.",
    params: z.object({
      team_id: z.string().describe("Team UUID"),
      first: z.number().default(20).describe("Number of results to return"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Cycle UUID"),
      name: z.string().nullable().describe("Cycle name"),
      number: z.number().describe("Cycle number"),
      starts_at: z.string().describe("Start date"),
      ends_at: z.string().describe("End date"),
      progress: z.number().describe("Progress percentage"),
      scope: z.number().describe("Total scope (issue count)"),
    })),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($id: String!, $first: Int) { team(id: $id) { cycles(first: $first) { nodes { id name number startsAt endsAt progress scopeHistory completedScopeHistory } } } }`, { id: params.team_id, first: params.first });
      return (data.team.cycles.nodes ?? []).map((c: any) => ({
        id: c.id,
        name: c.name ?? null,
        number: c.number,
        starts_at: c.startsAt,
        ends_at: c.endsAt,
        progress: c.progress ?? 0,
        scope: c.scopeHistory?.length ?? 0,
      }));
    },
  },

  get_cycle: {
    description: "Get detailed cycle information.",
    params: z.object({
      cycle_id: z.string().describe("Cycle UUID"),
    }),
    returns: z.object({
      id: z.string().describe("Cycle UUID"),
      name: z.string().nullable().describe("Cycle name"),
      number: z.number().describe("Cycle number"),
      starts_at: z.string().describe("Start date"),
      ends_at: z.string().describe("End date"),
      progress: z.number().describe("Progress percentage"),
      scope: z.number().describe("Total scope"),
      completed_scope: z.number().describe("Completed scope"),
      issues: z.array(z.string()).describe("Issue identifiers in this cycle"),
    }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($id: String!) { cycle(id: $id) { id name number startsAt endsAt progress scope completedScope issues { nodes { identifier } } } }`, { id: params.cycle_id });
      const c = data.cycle;
      return {
        id: c.id,
        name: c.name ?? null,
        number: c.number,
        starts_at: c.startsAt,
        ends_at: c.endsAt,
        progress: c.progress ?? 0,
        scope: c.scope ?? 0,
        completed_scope: c.completedScope ?? 0,
        issues: (c.issues?.nodes ?? []).map((i: any) => i.identifier),
      };
    },
  },

  get_active_cycle: {
    description: "Get the currently active cycle for a team.",
    params: z.object({
      team_id: z.string().describe("Team UUID"),
    }),
    returns: z.object({
      id: z.string().nullable().describe("Cycle UUID"),
      name: z.string().nullable().describe("Cycle name"),
      number: z.number().nullable().describe("Cycle number"),
      starts_at: z.string().nullable().describe("Start date"),
      ends_at: z.string().nullable().describe("End date"),
      progress: z.number().nullable().describe("Progress percentage"),
    }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($id: String!) { team(id: $id) { activeCycle { id name number startsAt endsAt progress } } }`, { id: params.team_id });
      const c = data.team.activeCycle;
      if (!c) return { id: null, name: null, number: null, starts_at: null, ends_at: null, progress: null };
      return { id: c.id, name: c.name ?? null, number: c.number, starts_at: c.startsAt, ends_at: c.endsAt, progress: c.progress ?? 0 };
    },
  },

  add_issue_to_cycle: {
    description: "Add an issue to a cycle.",
    params: z.object({
      issue_id: z.string().describe("Issue UUID to add"),
      cycle_id: z.string().describe("Target cycle UUID"),
    }),
    returns: z.object({
      issue_identifier: z.string().describe("Issue identifier"),
      cycle_name: z.string().nullable().describe("Cycle name"),
    }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `mutation($id: String!, $input: IssueUpdateInput!) { issueUpdate(id: $id, input: $input) { success issue { identifier cycle { name } } } }`, { id: params.issue_id, input: { cycleId: params.cycle_id } });
      const i = data.issueUpdate.issue;
      return { issue_identifier: i.identifier, cycle_name: i.cycle?.name ?? null };
    },
  },

  remove_issue_from_cycle: {
    description: "Remove an issue from its cycle.",
    params: z.object({
      issue_id: z.string().describe("Issue UUID to remove"),
    }),
    returns: z.object({ success: z.boolean().describe("Whether the removal succeeded") }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `mutation($id: String!, $input: IssueUpdateInput!) { issueUpdate(id: $id, input: $input) { success } }`, { id: params.issue_id, input: { cycleId: null } });
      return { success: data.issueUpdate.success };
    },
  },
};
