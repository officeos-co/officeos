import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gql } from "../core/client.ts";

export const issues: Record<string, ActionDefinition> = {
  list_issues: {
    description: "List issues with optional filters.",
    params: z.object({
      team_id: z.string().optional().describe("Filter by team UUID"),
      state: z.string().optional().describe("Filter by workflow state name"),
      assignee: z.string().optional().describe("Filter by assignee user ID"),
      label: z.string().optional().describe("Filter by label name"),
      priority: z.number().optional().describe("Filter by priority (0=none, 1=urgent, 4=low)"),
      project: z.string().optional().describe("Filter by project ID"),
      first: z.number().default(50).describe("Number of results to return"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Issue UUID"),
      identifier: z.string().describe("Issue identifier (e.g. ENG-123)"),
      title: z.string().describe("Issue title"),
      state: z.string().describe("Workflow state name"),
      priority: z.number().describe("Priority level"),
      assignee: z.string().nullable().describe("Assignee name"),
      labels: z.array(z.string()).describe("Label names"),
      created_at: z.string().describe("Creation date"),
    })),
    execute: async (params, ctx) => {
      const filters: string[] = [];
      if (params.team_id) filters.push(`team: { id: { eq: "${params.team_id}" } }`);
      if (params.state) filters.push(`state: { name: { eq: "${params.state}" } }`);
      if (params.assignee) filters.push(`assignee: { id: { eq: "${params.assignee}" } }`);
      if (params.label) filters.push(`labels: { name: { eq: "${params.label}" } }`);
      if (params.priority !== undefined) filters.push(`priority: { eq: ${params.priority} }`);
      if (params.project) filters.push(`project: { id: { eq: "${params.project}" } }`);
      const filterStr = filters.length > 0 ? `filter: { ${filters.join(", ")} },` : "";
      const data = await gql(ctx, `query { issues(${filterStr} first: ${params.first}) { nodes { id identifier title state { name } priority assignee { name } labels { nodes { name } } createdAt } } }`);
      return (data.issues.nodes ?? []).map((i: any) => ({
        id: i.id,
        identifier: i.identifier,
        title: i.title,
        state: i.state?.name ?? "",
        priority: i.priority,
        assignee: i.assignee?.name ?? null,
        labels: (i.labels?.nodes ?? []).map((l: any) => l.name),
        created_at: i.createdAt,
      }));
    },
  },

  get_issue: {
    description: "Get detailed information about a single issue.",
    params: z.object({
      issue_id: z.string().describe("Issue UUID or identifier"),
    }),
    returns: z.object({
      id: z.string().describe("Issue UUID"),
      identifier: z.string().describe("Issue identifier"),
      title: z.string().describe("Issue title"),
      description: z.string().nullable().describe("Issue description"),
      state: z.string().describe("Workflow state name"),
      priority: z.number().describe("Priority level"),
      assignee: z.string().nullable().describe("Assignee name"),
      labels: z.array(z.string()).describe("Label names"),
      project: z.string().nullable().describe("Project name"),
      cycle: z.string().nullable().describe("Cycle name"),
      estimate: z.number().nullable().describe("Estimate points"),
      created_at: z.string().describe("Creation date"),
      updated_at: z.string().describe("Last update date"),
    }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($id: String!) { issue(id: $id) { id identifier title description state { name } priority assignee { name } labels { nodes { name } } project { name } cycle { name } estimate createdAt updatedAt } }`, { id: params.issue_id });
      const i = data.issue;
      return {
        id: i.id,
        identifier: i.identifier,
        title: i.title,
        description: i.description ?? null,
        state: i.state?.name ?? "",
        priority: i.priority,
        assignee: i.assignee?.name ?? null,
        labels: (i.labels?.nodes ?? []).map((l: any) => l.name),
        project: i.project?.name ?? null,
        cycle: i.cycle?.name ?? null,
        estimate: i.estimate ?? null,
        created_at: i.createdAt,
        updated_at: i.updatedAt,
      };
    },
  },

  create_issue: {
    description: "Create a new issue.",
    params: z.object({
      title: z.string().describe("Issue title"),
      description: z.string().optional().describe("Issue description (markdown)"),
      team_id: z.string().describe("Team UUID to create issue in"),
      assignee_id: z.string().optional().describe("User UUID to assign"),
      priority: z.number().default(0).describe("Priority (0=none, 1=urgent, 2=high, 3=medium, 4=low)"),
      state_id: z.string().optional().describe("Workflow state UUID"),
      label_ids: z.array(z.string()).optional().describe("Label UUIDs to apply"),
      project_id: z.string().optional().describe("Project UUID to associate"),
      estimate: z.number().optional().describe("Estimate points"),
    }),
    returns: z.object({
      id: z.string().describe("Issue UUID"),
      identifier: z.string().describe("Issue identifier"),
      title: z.string().describe("Issue title"),
      url: z.string().describe("Issue URL"),
    }),
    execute: async (params, ctx) => {
      const input: any = { title: params.title, teamId: params.team_id, priority: params.priority };
      if (params.description) input.description = params.description;
      if (params.assignee_id) input.assigneeId = params.assignee_id;
      if (params.state_id) input.stateId = params.state_id;
      if (params.label_ids) input.labelIds = params.label_ids;
      if (params.project_id) input.projectId = params.project_id;
      if (params.estimate !== undefined) input.estimate = params.estimate;
      const data = await gql(ctx, `mutation($input: IssueCreateInput!) { issueCreate(input: $input) { success issue { id identifier title url } } }`, { input });
      const i = data.issueCreate.issue;
      return { id: i.id, identifier: i.identifier, title: i.title, url: i.url };
    },
  },

  update_issue: {
    description: "Update an existing issue.",
    params: z.object({
      issue_id: z.string().describe("Issue UUID to update"),
      title: z.string().optional().describe("New title"),
      description: z.string().optional().describe("New description"),
      assignee_id: z.string().optional().describe("New assignee UUID"),
      priority: z.number().optional().describe("New priority level"),
      state_id: z.string().optional().describe("New workflow state UUID"),
      label_ids: z.array(z.string()).optional().describe("Replace label UUIDs"),
      project_id: z.string().optional().describe("New project UUID"),
      estimate: z.number().optional().describe("New estimate points"),
    }),
    returns: z.object({
      id: z.string().describe("Issue UUID"),
      identifier: z.string().describe("Issue identifier"),
      title: z.string().describe("Issue title"),
      state: z.string().describe("Workflow state name"),
    }),
    execute: async (params, ctx) => {
      const input: any = {};
      if (params.title !== undefined) input.title = params.title;
      if (params.description !== undefined) input.description = params.description;
      if (params.assignee_id !== undefined) input.assigneeId = params.assignee_id;
      if (params.priority !== undefined) input.priority = params.priority;
      if (params.state_id !== undefined) input.stateId = params.state_id;
      if (params.label_ids !== undefined) input.labelIds = params.label_ids;
      if (params.project_id !== undefined) input.projectId = params.project_id;
      if (params.estimate !== undefined) input.estimate = params.estimate;
      const data = await gql(ctx, `mutation($id: String!, $input: IssueUpdateInput!) { issueUpdate(id: $id, input: $input) { success issue { id identifier title state { name } } } }`, { id: params.issue_id, input });
      const i = data.issueUpdate.issue;
      return { id: i.id, identifier: i.identifier, title: i.title, state: i.state?.name ?? "" };
    },
  },

  delete_issue: {
    description: "Permanently delete an issue.",
    params: z.object({
      issue_id: z.string().describe("Issue UUID to delete"),
    }),
    returns: z.object({ success: z.boolean().describe("Whether the deletion succeeded") }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `mutation($id: String!) { issueDelete(id: $id) { success } }`, { id: params.issue_id });
      return { success: data.issueDelete.success };
    },
  },

  archive_issue: {
    description: "Archive an issue (preferred over delete).",
    params: z.object({
      issue_id: z.string().describe("Issue UUID to archive"),
    }),
    returns: z.object({
      id: z.string().describe("Issue UUID"),
      identifier: z.string().describe("Issue identifier"),
      archived_at: z.string().nullable().describe("Archive timestamp"),
    }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `mutation($id: String!) { issueArchive(id: $id) { success entity { id identifier archivedAt } } }`, { id: params.issue_id });
      const i = data.issueArchive.entity;
      return { id: i.id, identifier: i.identifier, archived_at: i.archivedAt ?? null };
    },
  },

};
