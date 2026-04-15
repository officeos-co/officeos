import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gql } from "../core/client.ts";

export const labels: Record<string, ActionDefinition> = {
  list_labels: {
    description: "List labels, optionally filtered by team.",
    params: z.object({
      team_id: z.string().optional().describe("Filter by team (omit for workspace labels)"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Label UUID"),
      name: z.string().describe("Label name"),
      color: z.string().describe("Hex color"),
      description: z.string().nullable().describe("Label description"),
    })),
    execute: async (params, ctx) => {
      if (params.team_id) {
        const data = await gql(ctx, `query($id: String!) { team(id: $id) { labels { nodes { id name color description } } } }`, { id: params.team_id });
        return (data.team.labels.nodes ?? []).map((l: any) => ({
          id: l.id, name: l.name, color: l.color, description: l.description ?? null,
        }));
      }
      const data = await gql(ctx, `query { issueLabels { nodes { id name color description } } }`);
      return (data.issueLabels.nodes ?? []).map((l: any) => ({
        id: l.id, name: l.name, color: l.color, description: l.description ?? null,
      }));
    },
  },

  create_label: {
    description: "Create a label.",
    params: z.object({
      name: z.string().describe("Label name"),
      color: z.string().optional().describe("Hex color code"),
      team_id: z.string().optional().describe("Team UUID (omit for workspace label)"),
      description: z.string().optional().describe("Label description"),
    }),
    returns: z.object({
      id: z.string().describe("Label UUID"),
      name: z.string().describe("Label name"),
      color: z.string().describe("Hex color"),
    }),
    execute: async (params, ctx) => {
      const input: any = { name: params.name };
      if (params.color) input.color = params.color;
      if (params.team_id) input.teamId = params.team_id;
      if (params.description) input.description = params.description;
      const data = await gql(ctx, `mutation($input: IssueLabelCreateInput!) { issueLabelCreate(input: $input) { success issueLabel { id name color } } }`, { input });
      const l = data.issueLabelCreate.issueLabel;
      return { id: l.id, name: l.name, color: l.color };
    },
  },
};
