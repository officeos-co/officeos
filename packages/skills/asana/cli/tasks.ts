import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { asanaGet, asanaPost, asanaPut, asanaDelete, mapTask } from "../core/client.ts";

const TaskResultSchema = z.object({
  gid: z.string(),
  name: z.string(),
  completed: z.boolean(),
  assignee: z.string().nullable(),
  due_on: z.string().nullable(),
  created_at: z.string(),
  modified_at: z.string(),
});

export const tasks: Record<string, ActionDefinition> = {
  list_tasks: {
    description: "List tasks in a project or section.",
    params: z.object({
      project_gid: z.string().optional().describe("Project GID"),
      section_gid: z.string().optional().describe("Section GID"),
      assignee: z.string().optional().describe("Filter by assignee GID or me"),
      completed: z.boolean().optional().describe("Filter by completion status"),
      modified_since: z.string().optional().describe("ISO 8601 — tasks modified after"),
    }),
    returns: z.array(TaskResultSchema),
    execute: async (params, ctx) => {
      const qs: Record<string, string> = {
        opt_fields: "gid,name,completed,assignee.name,due_on,created_at,modified_at",
      };
      if (params.project_gid) qs["project"] = params.project_gid;
      if (params.section_gid) qs["section"] = params.section_gid;
      if (params.assignee) qs["assignee"] = params.assignee;
      if (params.completed !== undefined) qs["completed_since"] = params.completed ? "now" : "2000-01-01T00:00:00.000Z";
      if (params.modified_since) qs["modified_since"] = params.modified_since;
      const data = await asanaGet(ctx, "/tasks", qs);
      return (Array.isArray(data) ? data : []).map(mapTask);
    },
  },

  get_task: {
    description: "Get details of a task.",
    params: z.object({
      task_gid: z.string().describe("Task GID"),
    }),
    returns: z.object({
      gid: z.string(),
      name: z.string(),
      notes: z.string(),
      completed: z.boolean(),
      assignee: z.string().nullable(),
      due_on: z.string().nullable(),
      created_at: z.string(),
      modified_at: z.string(),
    }),
    execute: async (params, ctx) => {
      const data = await asanaGet(
        ctx,
        `/tasks/${encodeURIComponent(params.task_gid)}`,
        { opt_fields: "gid,name,notes,completed,assignee.name,due_on,created_at,modified_at" },
      );
      return {
        gid: data.gid as string,
        name: data.name as string,
        notes: (data.notes as string) ?? "",
        completed: (data.completed as boolean) ?? false,
        assignee: (data.assignee as Record<string, unknown>)?.name as string ?? null,
        due_on: (data.due_on as string) ?? null,
        created_at: data.created_at as string,
        modified_at: data.modified_at as string,
      };
    },
  },

  create_task: {
    description: "Create a new task.",
    params: z.object({
      project_gid: z.string().describe("Project GID"),
      name: z.string().describe("Task name"),
      notes: z.string().optional().describe("Task description"),
      assignee: z.string().optional().describe("Assignee GID or me"),
      due_on: z.string().optional().describe("Due date (YYYY-MM-DD)"),
      section_gid: z.string().optional().describe("Section GID to add task to"),
      tags: z.array(z.string()).optional().describe("Tag GIDs to attach"),
      parent_gid: z.string().optional().describe("Parent task GID (for subtasks)"),
    }),
    returns: z.object({
      gid: z.string(),
      name: z.string(),
      created_at: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {
        name: params.name,
        projects: [params.project_gid],
      };
      if (params.notes) body["notes"] = params.notes;
      if (params.assignee) body["assignee"] = params.assignee;
      if (params.due_on) body["due_on"] = params.due_on;
      if (params.section_gid) body["memberships"] = [{ project: params.project_gid, section: params.section_gid }];
      if (params.tags) body["tags"] = params.tags;
      if (params.parent_gid) body["parent"] = params.parent_gid;
      const data = await asanaPost(ctx, "/tasks", body);
      return {
        gid: data.gid as string,
        name: data.name as string,
        created_at: data.created_at as string,
      };
    },
  },

  update_task: {
    description: "Update a task.",
    params: z.object({
      task_gid: z.string().describe("Task GID"),
      name: z.string().optional().describe("New name"),
      notes: z.string().optional().describe("New description"),
      completed: z.boolean().optional().describe("Mark complete/incomplete"),
      assignee: z.string().optional().describe("New assignee GID"),
      due_on: z.string().optional().describe("New due date (YYYY-MM-DD)"),
    }),
    returns: z.object({
      gid: z.string(),
      name: z.string(),
      completed: z.boolean(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {};
      if (params.name) body["name"] = params.name;
      if (params.notes) body["notes"] = params.notes;
      if (params.completed !== undefined) body["completed"] = params.completed;
      if (params.assignee) body["assignee"] = params.assignee;
      if (params.due_on) body["due_on"] = params.due_on;
      const data = await asanaPut(ctx, `/tasks/${encodeURIComponent(params.task_gid)}`, body);
      return {
        gid: data.gid as string,
        name: data.name as string,
        completed: (data.completed as boolean) ?? false,
      };
    },
  },

  delete_task: {
    description: "Delete a task.",
    params: z.object({
      task_gid: z.string().describe("Task GID"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      return asanaDelete(ctx, `/tasks/${encodeURIComponent(params.task_gid)}`);
    },
  },

  list_comments: {
    description: "List comments (stories) on a task.",
    params: z.object({
      task_gid: z.string().describe("Task GID"),
    }),
    returns: z.array(z.object({
      gid: z.string(),
      type: z.string(),
      text: z.string(),
      created_at: z.string(),
      created_by: z.string().nullable(),
    })),
    execute: async (params, ctx) => {
      const data = await asanaGet(
        ctx,
        `/tasks/${encodeURIComponent(params.task_gid)}/stories`,
        { opt_fields: "gid,type,text,created_at,created_by.name" },
      );
      return (Array.isArray(data) ? data : []).map((s: Record<string, unknown>) => ({
        gid: s.gid as string,
        type: s.type as string,
        text: (s.text as string) ?? "",
        created_at: s.created_at as string,
        created_by: (s.created_by as Record<string, unknown>)?.name as string ?? null,
      }));
    },
  },

  add_comment: {
    description: "Add a comment to a task.",
    params: z.object({
      task_gid: z.string().describe("Task GID"),
      text: z.string().describe("Comment text"),
    }),
    returns: z.object({
      gid: z.string(),
      text: z.string(),
      created_at: z.string(),
    }),
    execute: async (params, ctx) => {
      const data = await asanaPost(ctx, `/tasks/${encodeURIComponent(params.task_gid)}/stories`, {
        text: params.text,
      });
      return {
        gid: data.gid as string,
        text: (data.text as string) ?? params.text,
        created_at: data.created_at as string,
      };
    },
  },
};
