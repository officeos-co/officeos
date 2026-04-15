import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gql } from "../core/client.ts";

export const search: Record<string, ActionDefinition> = {
  search_issues: {
    description: "Free-text search across issues.",
    params: z.object({
      query: z.string().describe("Free-text search query"),
      first: z.number().default(20).describe("Number of results to return"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Issue UUID"),
      identifier: z.string().describe("Issue identifier"),
      title: z.string().describe("Issue title"),
      state: z.string().describe("Workflow state name"),
      priority: z.number().describe("Priority level"),
    })),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($query: String!, $first: Int) { searchIssues(query: $query, first: $first) { nodes { id identifier title state { name } priority } } }`, { query: params.query, first: params.first });
      return (data.searchIssues.nodes ?? []).map((i: any) => ({
        id: i.id,
        identifier: i.identifier,
        title: i.title,
        state: i.state?.name ?? "",
        priority: i.priority,
      }));
    },
  },
};
