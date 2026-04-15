import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { asanaGet } from "../core/client.ts";

export const workspaces: Record<string, ActionDefinition> = {
  list_workspaces: {
    description: "List all workspaces the authenticated user belongs to.",
    params: z.object({}),
    returns: z.array(z.object({
      gid: z.string(),
      name: z.string(),
    })),
    execute: async (_params, ctx) => {
      const data = await asanaGet(ctx, "/workspaces");
      return (Array.isArray(data) ? data : []).map((w: Record<string, unknown>) => ({
        gid: w.gid as string,
        name: w.name as string,
      }));
    },
  },

  list_tags: {
    description: "List all tags in a workspace.",
    params: z.object({
      workspace_gid: z.string().describe("Workspace GID"),
    }),
    returns: z.array(z.object({
      gid: z.string(),
      name: z.string(),
      color: z.string().nullable(),
    })),
    execute: async (params, ctx) => {
      const data = await asanaGet(ctx, `/workspaces/${encodeURIComponent(params.workspace_gid)}/tags`);
      return (Array.isArray(data) ? data : []).map((t: Record<string, unknown>) => ({
        gid: t.gid as string,
        name: t.name as string,
        color: (t.color as string) ?? null,
      }));
    },
  },

  search_tasks: {
    description: "Search for tasks in a workspace using text.",
    params: z.object({
      workspace_gid: z.string().describe("Workspace GID"),
      text: z.string().describe("Full-text search query"),
      completed: z.boolean().optional().describe("Filter by completion status"),
      assignee: z.string().optional().describe("Filter by assignee GID"),
      project_gid: z.string().optional().describe("Filter by project GID"),
    }),
    returns: z.array(z.object({
      gid: z.string(),
      name: z.string(),
      completed: z.boolean(),
      assignee: z.string().nullable(),
      due_on: z.string().nullable(),
    })),
    execute: async (params, ctx) => {
      const qs: Record<string, string> = { text: params.text };
      if (params.completed !== undefined) qs["completed"] = String(params.completed);
      if (params.assignee) qs["assignee.any"] = params.assignee;
      if (params.project_gid) qs["projects.any"] = params.project_gid;
      qs["opt_fields"] = "gid,name,completed,assignee.name,due_on";
      const data = await asanaGet(ctx, `/workspaces/${encodeURIComponent(params.workspace_gid)}/tasks/search`, qs);
      return (Array.isArray(data) ? data : []).map((t: Record<string, unknown>) => ({
        gid: t.gid as string,
        name: t.name as string,
        completed: (t.completed as boolean) ?? false,
        assignee: (t.assignee as Record<string, unknown>)?.name as string ?? null,
        due_on: (t.due_on as string) ?? null,
      }));
    },
  },
};
