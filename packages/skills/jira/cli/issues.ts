import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { jiraGet, jiraPost, jiraPut, jiraDelete, jiraBase, mapIssue } from "../core/client.ts";

const IssueResultSchema = z.object({
  id: z.string(),
  key: z.string(),
  summary: z.string(),
  status: z.string(),
  assignee: z.string().nullable(),
  reporter: z.string().nullable(),
  priority: z.string().nullable(),
  labels: z.array(z.string()),
  created: z.string(),
  updated: z.string(),
});

export const issues: Record<string, ActionDefinition> = {
  list_issues: {
    description: "Search for issues using JQL.",
    params: z.object({
      jql: z.string().describe("JQL query string"),
      max_results: z.number().min(1).max(100).optional().describe("Maximum results (1-100)"),
      start_at: z.number().optional().describe("Offset for pagination"),
      fields: z.string().optional().describe("Comma-separated list of fields"),
    }),
    returns: z.object({
      total: z.number(),
      start_at: z.number(),
      max_results: z.number(),
      issues: z.array(IssueResultSchema),
    }),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      const body: Record<string, unknown> = {
        jql: params.jql,
        maxResults: params.max_results ?? 50,
        startAt: params.start_at ?? 0,
      };
      if (params.fields) body["fields"] = params.fields.split(",").map((f) => f.trim());
      const data = await jiraPost(ctx, `${base}/search`, body);
      return {
        total: data.total,
        start_at: data.startAt,
        max_results: data.maxResults,
        issues: (data.issues ?? []).map(mapIssue),
      };
    },
  },

  get_issue: {
    description: "Get a single issue by key or ID.",
    params: z.object({
      issue_key: z.string().describe("Issue key or ID"),
    }),
    returns: z.object({
      id: z.string(),
      key: z.string(),
      summary: z.string(),
      description: z.string().nullable(),
      status: z.string(),
      assignee: z.string().nullable(),
      reporter: z.string().nullable(),
      priority: z.string().nullable(),
      labels: z.array(z.string()),
      created: z.string(),
      updated: z.string(),
      due_date: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      const data = await jiraGet(ctx, `${base}/issue/${encodeURIComponent(params.issue_key)}`);
      const f = data.fields ?? {};
      return {
        id: data.id,
        key: data.key,
        summary: f.summary ?? "",
        description: typeof f.description === "string" ? f.description : null,
        status: f.status?.name ?? "",
        assignee: f.assignee?.displayName ?? null,
        reporter: f.reporter?.displayName ?? null,
        priority: f.priority?.name ?? null,
        labels: f.labels ?? [],
        created: f.created,
        updated: f.updated,
        due_date: f.duedate ?? null,
      };
    },
  },

  create_issue: {
    description: "Create a new issue.",
    params: z.object({
      project_key: z.string().describe("Project key"),
      summary: z.string().describe("Issue summary"),
      issue_type: z.string().optional().describe("Issue type: Bug, Task, Story, Epic, etc."),
      description: z.string().optional().describe("Issue description (plain text)"),
      priority: z.string().optional().describe("Priority: Highest, High, Medium, Low, Lowest"),
      assignee: z.string().optional().describe("Assignee account ID"),
      labels: z.array(z.string()).optional().describe("Labels to attach"),
      due_date: z.string().optional().describe("Due date (YYYY-MM-DD)"),
      parent_key: z.string().optional().describe("Parent issue key (for sub-tasks)"),
    }),
    returns: z.object({
      id: z.string(),
      key: z.string(),
      url: z.string(),
    }),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      const fields: Record<string, unknown> = {
        project: { key: params.project_key },
        summary: params.summary,
        issuetype: { name: params.issue_type ?? "Task" },
      };
      if (params.description) {
        fields["description"] = {
          type: "doc",
          version: 1,
          content: [{ type: "paragraph", content: [{ type: "text", text: params.description }] }],
        };
      }
      if (params.priority) fields["priority"] = { name: params.priority };
      if (params.assignee) fields["assignee"] = { accountId: params.assignee };
      if (params.labels) fields["labels"] = params.labels;
      if (params.due_date) fields["duedate"] = params.due_date;
      if (params.parent_key) fields["parent"] = { key: params.parent_key };
      const data = await jiraPost(ctx, `${base}/issue`, { fields });
      return {
        id: data.id,
        key: data.key,
        url: data.self,
      };
    },
  },

  update_issue: {
    description: "Update fields of an existing issue.",
    params: z.object({
      issue_key: z.string().describe("Issue key or ID"),
      summary: z.string().optional().describe("New summary"),
      description: z.string().optional().describe("New description"),
      priority: z.string().optional().describe("New priority"),
      assignee: z.string().optional().describe("New assignee account ID"),
      labels: z.array(z.string()).optional().describe("Replace labels"),
      due_date: z.string().optional().describe("New due date (YYYY-MM-DD)"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      const fields: Record<string, unknown> = {};
      if (params.summary) fields["summary"] = params.summary;
      if (params.description) {
        fields["description"] = {
          type: "doc",
          version: 1,
          content: [{ type: "paragraph", content: [{ type: "text", text: params.description }] }],
        };
      }
      if (params.priority) fields["priority"] = { name: params.priority };
      if (params.assignee) fields["assignee"] = { accountId: params.assignee };
      if (params.labels) fields["labels"] = params.labels;
      if (params.due_date) fields["duedate"] = params.due_date;
      await jiraPut(ctx, `${base}/issue/${encodeURIComponent(params.issue_key)}`, { fields });
      return { success: true };
    },
  },

  delete_issue: {
    description: "Delete an issue.",
    params: z.object({
      issue_key: z.string().describe("Issue key or ID"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      return jiraDelete(ctx, `${base}/issue/${encodeURIComponent(params.issue_key)}`);
    },
  },

  transition_issue: {
    description: "Move an issue to a new status using a transition.",
    params: z.object({
      issue_key: z.string().describe("Issue key or ID"),
      transition_id: z.string().describe("Transition ID (from get_transitions)"),
      comment: z.string().optional().describe("Comment to add when transitioning"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      const body: Record<string, unknown> = { transition: { id: params.transition_id } };
      if (params.comment) {
        body["update"] = {
          comment: [{
            add: {
              body: {
                type: "doc",
                version: 1,
                content: [{ type: "paragraph", content: [{ type: "text", text: params.comment }] }],
              },
            },
          }],
        };
      }
      await jiraPost(ctx, `${base}/issue/${encodeURIComponent(params.issue_key)}/transitions`, body);
      return { success: true };
    },
  },

  get_transitions: {
    description: "List available transitions for an issue.",
    params: z.object({
      issue_key: z.string().describe("Issue key or ID"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      name: z.string(),
      to_status: z.string(),
    })),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      const data = await jiraGet(ctx, `${base}/issue/${encodeURIComponent(params.issue_key)}/transitions`);
      return (data.transitions ?? []).map((t: Record<string, unknown>) => ({
        id: t.id as string,
        name: t.name as string,
        to_status: (t.to as Record<string, unknown>)?.name as string ?? "",
      }));
    },
  },

  assign_issue: {
    description: "Assign or unassign an issue.",
    params: z.object({
      issue_key: z.string().describe("Issue key or ID"),
      account_id: z.string().optional().describe("Account ID to assign (omit to unassign)"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      await jiraPut(ctx, `${base}/issue/${encodeURIComponent(params.issue_key)}/assignee`, {
        accountId: params.account_id ?? null,
      });
      return { success: true };
    },
  },

  search_issues: {
    description: "Full JQL search with pagination.",
    params: z.object({
      jql: z.string().describe("JQL query"),
      max_results: z.number().min(1).max(100).optional().describe("Max results (1-100)"),
      start_at: z.number().optional().describe("Pagination offset"),
      fields: z.string().optional().describe("Comma-separated fields"),
    }),
    returns: z.object({
      total: z.number(),
      start_at: z.number(),
      max_results: z.number(),
      issues: z.array(IssueResultSchema),
    }),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      const body: Record<string, unknown> = {
        jql: params.jql,
        maxResults: params.max_results ?? 50,
        startAt: params.start_at ?? 0,
      };
      if (params.fields) body["fields"] = params.fields.split(",").map((f) => f.trim());
      const data = await jiraPost(ctx, `${base}/search`, body);
      return {
        total: data.total,
        start_at: data.startAt,
        max_results: data.maxResults,
        issues: (data.issues ?? []).map(mapIssue),
      };
    },
  },
};
