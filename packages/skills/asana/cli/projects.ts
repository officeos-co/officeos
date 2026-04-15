import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { asanaGet, asanaPost, asanaPut } from "../core/client.ts";

export const projects: Record<string, ActionDefinition> = {
  list_projects: {
    description: "List projects in a workspace.",
    params: z.object({
      workspace_gid: z.string().describe("Workspace GID"),
      team_gid: z.string().optional().describe("Filter by team GID"),
      archived: z.boolean().optional().describe("Include archived projects"),
    }),
    returns: z.array(z.object({
      gid: z.string(),
      name: z.string(),
      color: z.string().nullable(),
      archived: z.boolean(),
      created_at: z.string(),
      modified_at: z.string(),
    })),
    execute: async (params, ctx) => {
      const qs: Record<string, string> = {
        workspace: params.workspace_gid,
        opt_fields: "gid,name,color,archived,created_at,modified_at",
      };
      if (params.team_gid) qs["team"] = params.team_gid;
      if (params.archived !== undefined) qs["archived"] = String(params.archived);
      const data = await asanaGet(ctx, "/projects", qs);
      return (Array.isArray(data) ? data : []).map((p: Record<string, unknown>) => ({
        gid: p.gid as string,
        name: p.name as string,
        color: (p.color as string) ?? null,
        archived: (p.archived as boolean) ?? false,
        created_at: p.created_at as string,
        modified_at: p.modified_at as string,
      }));
    },
  },

  get_project: {
    description: "Get details of a project.",
    params: z.object({
      project_gid: z.string().describe("Project GID"),
    }),
    returns: z.object({
      gid: z.string(),
      name: z.string(),
      notes: z.string(),
      color: z.string().nullable(),
      archived: z.boolean(),
      created_at: z.string(),
      modified_at: z.string(),
      due_date: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const data = await asanaGet(
        ctx,
        `/projects/${encodeURIComponent(params.project_gid)}`,
        { opt_fields: "gid,name,notes,color,archived,created_at,modified_at,due_date" },
      );
      return {
        gid: data.gid as string,
        name: data.name as string,
        notes: (data.notes as string) ?? "",
        color: (data.color as string) ?? null,
        archived: (data.archived as boolean) ?? false,
        created_at: data.created_at as string,
        modified_at: data.modified_at as string,
        due_date: (data.due_date as string) ?? null,
      };
    },
  },

  create_project: {
    description: "Create a new project.",
    params: z.object({
      workspace_gid: z.string().describe("Workspace GID"),
      name: z.string().describe("Project name"),
      team_gid: z.string().optional().describe("Team GID"),
      notes: z.string().optional().describe("Project description"),
      color: z.string().optional().describe("Project color"),
      due_date: z.string().optional().describe("Due date (YYYY-MM-DD)"),
    }),
    returns: z.object({
      gid: z.string(),
      name: z.string(),
      color: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {
        name: params.name,
        workspace: params.workspace_gid,
      };
      if (params.team_gid) body["team"] = params.team_gid;
      if (params.notes) body["notes"] = params.notes;
      if (params.color) body["color"] = params.color;
      if (params.due_date) body["due_date"] = params.due_date;
      const data = await asanaPost(ctx, "/projects", body);
      return {
        gid: data.gid as string,
        name: data.name as string,
        color: (data.color as string) ?? null,
      };
    },
  },

  update_project: {
    description: "Update a project.",
    params: z.object({
      project_gid: z.string().describe("Project GID"),
      name: z.string().optional().describe("New name"),
      notes: z.string().optional().describe("New description"),
      archived: z.boolean().optional().describe("Archive/unarchive"),
      color: z.string().optional().describe("New color"),
    }),
    returns: z.object({
      gid: z.string(),
      name: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {};
      if (params.name) body["name"] = params.name;
      if (params.notes) body["notes"] = params.notes;
      if (params.archived !== undefined) body["archived"] = params.archived;
      if (params.color) body["color"] = params.color;
      const data = await asanaPut(ctx, `/projects/${encodeURIComponent(params.project_gid)}`, body);
      return { gid: data.gid as string, name: data.name as string };
    },
  },

  list_sections: {
    description: "List sections in a project.",
    params: z.object({
      project_gid: z.string().describe("Project GID"),
    }),
    returns: z.array(z.object({
      gid: z.string(),
      name: z.string(),
      created_at: z.string(),
    })),
    execute: async (params, ctx) => {
      const data = await asanaGet(
        ctx,
        `/projects/${encodeURIComponent(params.project_gid)}/sections`,
        { opt_fields: "gid,name,created_at" },
      );
      return (Array.isArray(data) ? data : []).map((s: Record<string, unknown>) => ({
        gid: s.gid as string,
        name: s.name as string,
        created_at: s.created_at as string,
      }));
    },
  },

  create_section: {
    description: "Create a section in a project.",
    params: z.object({
      project_gid: z.string().describe("Project GID"),
      name: z.string().describe("Section name"),
    }),
    returns: z.object({
      gid: z.string(),
      name: z.string(),
    }),
    execute: async (params, ctx) => {
      const data = await asanaPost(
        ctx,
        `/projects/${encodeURIComponent(params.project_gid)}/sections`,
        { name: params.name },
      );
      return { gid: data.gid as string, name: data.name as string };
    },
  },
};
