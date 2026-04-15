import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { jiraGet, jiraBase, jiraAgileBase } from "../core/client.ts";

export const projects: Record<string, ActionDefinition> = {
  list_projects: {
    description: "List all projects accessible to the user.",
    params: z.object({
      max_results: z.number().optional().describe("Max projects to return"),
      query: z.string().optional().describe("Filter by name/key"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      key: z.string(),
      name: z.string(),
      type: z.string(),
      style: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      const qs: Record<string, string> = {};
      if (params.max_results) qs["maxResults"] = String(params.max_results);
      if (params.query) qs["query"] = params.query;
      const data = await jiraGet(ctx, `${base}/project/search`, qs);
      return (data.values ?? []).map((p: Record<string, unknown>) => ({
        id: p.id as string,
        key: p.key as string,
        name: p.name as string,
        type: p.projectTypeKey as string ?? "",
        style: p.style as string | undefined,
      }));
    },
  },

  get_project: {
    description: "Get details of a project.",
    params: z.object({
      project_key: z.string().describe("Project key"),
    }),
    returns: z.object({
      id: z.string(),
      key: z.string(),
      name: z.string(),
      description: z.string().nullable(),
      type: z.string(),
    }),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      const data = await jiraGet(ctx, `${base}/project/${encodeURIComponent(params.project_key)}`);
      return {
        id: data.id as string,
        key: data.key as string,
        name: data.name as string,
        description: (data.description as string) ?? null,
        type: data.projectTypeKey as string ?? "",
      };
    },
  },

  list_boards: {
    description: "List all Scrum/Kanban boards.",
    params: z.object({
      project_key: z.string().optional().describe("Filter by project key"),
      type: z.enum(["scrum", "kanban"]).optional().describe("Board type: scrum or kanban"),
      max_results: z.number().optional().describe("Max boards to return"),
    }),
    returns: z.array(z.object({
      id: z.number(),
      name: z.string(),
      type: z.string(),
    })),
    execute: async (params, ctx) => {
      const base = jiraAgileBase(ctx);
      const qs: Record<string, string> = {};
      if (params.project_key) qs["projectKeyOrId"] = params.project_key;
      if (params.type) qs["type"] = params.type;
      if (params.max_results) qs["maxResults"] = String(params.max_results);
      const data = await jiraGet(ctx, `${base}/board`, qs);
      return (data.values ?? []).map((b: Record<string, unknown>) => ({
        id: b.id as number,
        name: b.name as string,
        type: b.type as string,
      }));
    },
  },

  list_sprints: {
    description: "List sprints for a board.",
    params: z.object({
      board_id: z.number().describe("Board ID"),
      state: z.enum(["active", "future", "closed"]).optional().describe("Sprint state"),
    }),
    returns: z.array(z.object({
      id: z.number(),
      name: z.string(),
      state: z.string(),
      start_date: z.string().optional(),
      end_date: z.string().optional(),
      goal: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const base = jiraAgileBase(ctx);
      const qs: Record<string, string> = {};
      if (params.state) qs["state"] = params.state;
      const data = await jiraGet(ctx, `${base}/board/${params.board_id}/sprint`, qs);
      return (data.values ?? []).map((s: Record<string, unknown>) => ({
        id: s.id as number,
        name: s.name as string,
        state: s.state as string,
        start_date: s.startDate as string | undefined,
        end_date: s.endDate as string | undefined,
        goal: s.goal as string | undefined,
      }));
    },
  },

  get_sprint: {
    description: "Get details of a sprint.",
    params: z.object({
      sprint_id: z.number().describe("Sprint ID"),
    }),
    returns: z.object({
      id: z.number(),
      name: z.string(),
      state: z.string(),
      start_date: z.string().optional(),
      end_date: z.string().optional(),
      goal: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const base = jiraAgileBase(ctx);
      const data = await jiraGet(ctx, `${base}/sprint/${params.sprint_id}`);
      return {
        id: data.id as number,
        name: data.name as string,
        state: data.state as string,
        start_date: data.startDate as string | undefined,
        end_date: data.endDate as string | undefined,
        goal: data.goal as string | undefined,
      };
    },
  },
};
