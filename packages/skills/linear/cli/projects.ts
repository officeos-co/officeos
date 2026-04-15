import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gql } from "../core/client.ts";

export const projects: Record<string, ActionDefinition> = {
  list_projects: {
    description: "List projects.",
    params: z.object({
      first: z.number().default(50).describe("Number of results to return"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Project UUID"),
      name: z.string().describe("Project name"),
      description: z.string().nullable().describe("Project description"),
      state: z.string().describe("Project state"),
      progress: z.number().describe("Progress percentage"),
      lead: z.string().nullable().describe("Lead user name"),
      start_date: z.string().nullable().describe("Start date"),
      target_date: z.string().nullable().describe("Target completion date"),
    })),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($first: Int) { projects(first: $first) { nodes { id name description state progress lead { name } startDate targetDate } } }`, { first: params.first });
      return (data.projects.nodes ?? []).map((p: any) => ({
        id: p.id,
        name: p.name,
        description: p.description ?? null,
        state: p.state ?? "",
        progress: p.progress ?? 0,
        lead: p.lead?.name ?? null,
        start_date: p.startDate ?? null,
        target_date: p.targetDate ?? null,
      }));
    },
  },

  get_project: {
    description: "Get detailed project information.",
    params: z.object({
      project_id: z.string().describe("Project UUID"),
    }),
    returns: z.object({
      id: z.string().describe("Project UUID"),
      name: z.string().describe("Project name"),
      description: z.string().nullable().describe("Project description"),
      state: z.string().describe("Project state"),
      progress: z.number().describe("Progress percentage"),
      lead: z.string().nullable().describe("Lead user name"),
      members: z.array(z.string()).describe("Member names"),
      teams: z.array(z.string()).describe("Team names"),
      issues_count: z.number().describe("Number of issues"),
      start_date: z.string().nullable().describe("Start date"),
      target_date: z.string().nullable().describe("Target completion date"),
    }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($id: String!) { project(id: $id) { id name description state progress lead { name } members { nodes { name } } teams { nodes { name } } issues { nodes { id } } startDate targetDate } }`, { id: params.project_id });
      const p = data.project;
      return {
        id: p.id,
        name: p.name,
        description: p.description ?? null,
        state: p.state ?? "",
        progress: p.progress ?? 0,
        lead: p.lead?.name ?? null,
        members: (p.members?.nodes ?? []).map((m: any) => m.name),
        teams: (p.teams?.nodes ?? []).map((t: any) => t.name),
        issues_count: p.issues?.nodes?.length ?? 0,
        start_date: p.startDate ?? null,
        target_date: p.targetDate ?? null,
      };
    },
  },

  create_project: {
    description: "Create a new project.",
    params: z.object({
      name: z.string().describe("Project name"),
      description: z.string().optional().describe("Project description"),
      team_ids: z.array(z.string()).describe("Team UUIDs to associate"),
      lead_id: z.string().optional().describe("Lead user UUID"),
      start_date: z.string().optional().describe("Start date (ISO format)"),
      target_date: z.string().optional().describe("Target completion date"),
    }),
    returns: z.object({
      id: z.string().describe("Project UUID"),
      name: z.string().describe("Project name"),
      url: z.string().describe("Project URL"),
    }),
    execute: async (params, ctx) => {
      const input: any = { name: params.name, teamIds: params.team_ids };
      if (params.description) input.description = params.description;
      if (params.lead_id) input.leadId = params.lead_id;
      if (params.start_date) input.startDate = params.start_date;
      if (params.target_date) input.targetDate = params.target_date;
      const data = await gql(ctx, `mutation($input: ProjectCreateInput!) { projectCreate(input: $input) { success project { id name url } } }`, { input });
      const p = data.projectCreate.project;
      return { id: p.id, name: p.name, url: p.url };
    },
  },

  update_project: {
    description: "Update a project.",
    params: z.object({
      project_id: z.string().describe("Project UUID to update"),
      name: z.string().optional().describe("New project name"),
      description: z.string().optional().describe("New description"),
      state: z.string().optional().describe("State: planned, started, paused, completed, canceled"),
      lead_id: z.string().optional().describe("New lead user UUID"),
      start_date: z.string().optional().describe("New start date"),
      target_date: z.string().optional().describe("New target date"),
    }),
    returns: z.object({
      id: z.string().describe("Project UUID"),
      name: z.string().describe("Project name"),
      state: z.string().describe("Project state"),
    }),
    execute: async (params, ctx) => {
      const input: any = {};
      if (params.name !== undefined) input.name = params.name;
      if (params.description !== undefined) input.description = params.description;
      if (params.state !== undefined) input.state = params.state;
      if (params.lead_id !== undefined) input.leadId = params.lead_id;
      if (params.start_date !== undefined) input.startDate = params.start_date;
      if (params.target_date !== undefined) input.targetDate = params.target_date;
      const data = await gql(ctx, `mutation($id: String!, $input: ProjectUpdateInput!) { projectUpdate(id: $id, input: $input) { success project { id name state } } }`, { id: params.project_id, input });
      const p = data.projectUpdate.project;
      return { id: p.id, name: p.name, state: p.state ?? "" };
    },
  },

  archive_project: {
    description: "Archive a project.",
    params: z.object({
      project_id: z.string().describe("Project UUID to archive"),
    }),
    returns: z.object({
      id: z.string().describe("Project UUID"),
      name: z.string().describe("Project name"),
      archived_at: z.string().nullable().describe("Archive timestamp"),
    }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `mutation($id: String!) { projectArchive(id: $id) { success entity { id name archivedAt } } }`, { id: params.project_id });
      const p = data.projectArchive.entity;
      return { id: p.id, name: p.name, archived_at: p.archivedAt ?? null };
    },
  },
};
