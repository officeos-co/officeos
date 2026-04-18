import { defineSkill, z } from "@harro/skill-sdk";

const TODOIST_API = "https://api.todoist.com/rest/v2";

function headers(token: string) {
  return {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
  };
}

async function tdFetch(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  url: string,
  init?: RequestInit,
) {
  const res = await ctx.fetch(url, {
    ...init,
    headers: { ...headers(ctx.credentials.api_token), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Todoist API ${res.status}: ${body}`);
  }
  // Some endpoints return 204 No Content
  if (res.status === 204) return null;
  return res.json();
}

async function tdPost(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  url: string,
  body: unknown,
  method = "POST",
) {
  const res = await ctx.fetch(url, {
    method,
    headers: headers(ctx.credentials.api_token),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Todoist API ${res.status}: ${text}`);
  }
  if (res.status === 204) return null;
  return res.json();
}

async function tdDelete(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  url: string,
) {
  const res = await ctx.fetch(url, {
    method: "DELETE",
    headers: headers(ctx.credentials.api_token),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Todoist API ${res.status}: ${text}`);
  }
  return { success: true };
}

import doc from "./SKILL.md";

export default defineSkill({
  name: "todoist",
  title: "Todoist",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M21 0H3C1.35 0 0 1.35 0 3v3.858s3.854 2.24 4.098 2.38c.31.18.694.177 1.004 0 .26-.147 8.02-4.608 8.136-4.675.279-.161.58-.107.748-.01.164.097.606.348.84.48.232.134.221.502.013.622l-9.712 5.59c-.346.2-.69.204-1.048.002C3.478 10.907.998 9.463 0 8.882v2.02l4.098 2.38c.31.18.694.177 1.004 0 .26-.147 8.02-4.609 8.136-4.676.279-.16.58-.106.748-.008.164.096.606.347.84.48.232.133.221.5.013.62-.208.121-9.288 5.346-9.712 5.59-.346.2-.69.205-1.048.002C3.478 14.951.998 13.506 0 12.926v2.02l4.098 2.38c.31.18.694.177 1.004 0 .26-.147 8.02-4.609 8.136-4.676.279-.16.58-.106.748-.009.164.097.606.348.84.48.232.133.221.502.013.622l-9.712 5.59c-.346.199-.69.204-1.048.001C3.478 18.994.998 17.55 0 16.97V21c0 1.65 1.35 3 3 3h18c1.65 0 3-1.35 3-3V3c0-1.65-1.35-3-3-3z\"/></svg>",
  description:
    "Full Todoist REST API v2 coverage: manage tasks, projects, sections, comments, labels, and collaborators.",
  doc,

  credentials: {
    api_token: {
      label: "API Token",
      kind: "password",
      placeholder: "Your Todoist API token",
      help: "Find your API token in Todoist Settings > Integrations > Developer. https://todoist.com/app/settings/integrations/developer",
    },
  },

  actions: {
    // ── Tasks ─────────────────────────────────────────────────────────

    list_tasks: {
      description: "List active tasks, optionally filtered by project, label, filter string, or priority.",
      params: z.object({
        project_id: z.string().optional().describe("Filter by project ID"),
        filter: z.string().optional().describe("Todoist filter string (e.g. 'today', 'overdue')"),
        label: z.string().optional().describe("Filter by label name"),
        priority: z.number().min(1).max(4).optional().describe("Filter by priority (1=normal, 4=urgent)"),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          content: z.string(),
          description: z.string(),
          project_id: z.string(),
          priority: z.number(),
          due: z.object({
            date: z.string(),
            string: z.string(),
            recurring: z.boolean(),
          }).nullable(),
          labels: z.array(z.string()),
          url: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const qs = new URLSearchParams();
        if (params.project_id) qs.set("project_id", params.project_id);
        if (params.filter) qs.set("filter", params.filter);
        if (params.label) qs.set("label", params.label);
        const query = qs.toString();
        const data = await tdFetch(ctx, `${TODOIST_API}/tasks${query ? `?${query}` : ""}`);
        let tasks = (data ?? []).map((t: any) => ({
          id: t.id,
          content: t.content,
          description: t.description ?? "",
          project_id: t.project_id,
          priority: t.priority,
          due: t.due ? { date: t.due.date, string: t.due.string, recurring: t.due.is_recurring } : null,
          labels: t.labels ?? [],
          url: t.url,
        }));
        if (params.priority) {
          tasks = tasks.filter((t: any) => t.priority === params.priority);
        }
        return tasks;
      },
    },

    get_task: {
      description: "Get a single task by ID.",
      params: z.object({
        task_id: z.string().describe("Task ID"),
      }),
      returns: z.object({
        id: z.string(),
        content: z.string(),
        description: z.string(),
        project_id: z.string(),
        section_id: z.string().nullable(),
        parent_id: z.string().nullable(),
        priority: z.number(),
        due: z.object({
          date: z.string(),
          string: z.string(),
          recurring: z.boolean(),
        }).nullable(),
        labels: z.array(z.string()),
        assignee_id: z.string().nullable(),
        is_completed: z.boolean(),
        url: z.string(),
        created_at: z.string(),
      }),
      execute: async (params, ctx) => {
        const t = await tdFetch(ctx, `${TODOIST_API}/tasks/${params.task_id}`);
        return {
          id: t.id,
          content: t.content,
          description: t.description ?? "",
          project_id: t.project_id,
          section_id: t.section_id ?? null,
          parent_id: t.parent_id ?? null,
          priority: t.priority,
          due: t.due ? { date: t.due.date, string: t.due.string, recurring: t.due.is_recurring } : null,
          labels: t.labels ?? [],
          assignee_id: t.assignee_id ?? null,
          is_completed: t.is_completed,
          url: t.url,
          created_at: t.created_at,
        };
      },
    },

    create_task: {
      description: "Create a new task.",
      params: z.object({
        content: z.string().describe("Task content (title)"),
        description: z.string().optional().describe("Task description (markdown)"),
        project_id: z.string().optional().describe("Project to add the task to"),
        section_id: z.string().optional().describe("Section to add the task to"),
        parent_id: z.string().optional().describe("Parent task ID for sub-tasks"),
        priority: z.number().min(1).max(4).default(1).describe("Priority (1=normal, 4=urgent)"),
        due_string: z.string().optional().describe("Natural language due date"),
        due_date: z.string().optional().describe("Due date in YYYY-MM-DD format"),
        labels: z.array(z.string()).optional().describe("Labels to apply"),
        assignee_id: z.string().optional().describe("User ID to assign the task to"),
      }),
      returns: z.object({
        id: z.string(),
        content: z.string(),
        url: z.string(),
      }),
      execute: async (params, ctx) => {
        const payload: any = {
          content: params.content,
          priority: params.priority,
        };
        if (params.description !== undefined) payload.description = params.description;
        if (params.project_id !== undefined) payload.project_id = params.project_id;
        if (params.section_id !== undefined) payload.section_id = params.section_id;
        if (params.parent_id !== undefined) payload.parent_id = params.parent_id;
        if (params.due_string !== undefined) payload.due_string = params.due_string;
        if (params.due_date !== undefined) payload.due_date = params.due_date;
        if (params.labels !== undefined) payload.labels = params.labels;
        if (params.assignee_id !== undefined) payload.assignee_id = params.assignee_id;
        const t = await tdPost(ctx, `${TODOIST_API}/tasks`, payload);
        return { id: t.id, content: t.content, url: t.url };
      },
    },

    update_task: {
      description: "Update an existing task.",
      params: z.object({
        task_id: z.string().describe("Task ID"),
        content: z.string().optional().describe("New task content"),
        description: z.string().optional().describe("New description"),
        priority: z.number().min(1).max(4).optional().describe("New priority (1-4)"),
        due_string: z.string().optional().describe("New natural language due date"),
        due_date: z.string().optional().describe("New due date in YYYY-MM-DD format"),
        labels: z.array(z.string()).optional().describe("Replace labels"),
        assignee_id: z.string().optional().describe("New assignee user ID"),
      }),
      returns: z.object({
        id: z.string(),
        content: z.string(),
        url: z.string(),
      }),
      execute: async (params, ctx) => {
        const payload: any = {};
        if (params.content !== undefined) payload.content = params.content;
        if (params.description !== undefined) payload.description = params.description;
        if (params.priority !== undefined) payload.priority = params.priority;
        if (params.due_string !== undefined) payload.due_string = params.due_string;
        if (params.due_date !== undefined) payload.due_date = params.due_date;
        if (params.labels !== undefined) payload.labels = params.labels;
        if (params.assignee_id !== undefined) payload.assignee_id = params.assignee_id;
        const t = await tdPost(ctx, `${TODOIST_API}/tasks/${params.task_id}`, payload, "POST");
        return { id: t.id, content: t.content, url: t.url };
      },
    },

    close_task: {
      description: "Mark a task as completed.",
      params: z.object({
        task_id: z.string().describe("Task ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await tdPost(ctx, `${TODOIST_API}/tasks/${params.task_id}/close`, {});
        return { success: true };
      },
    },

    reopen_task: {
      description: "Reopen a previously completed task.",
      params: z.object({
        task_id: z.string().describe("Task ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await tdPost(ctx, `${TODOIST_API}/tasks/${params.task_id}/reopen`, {});
        return { success: true };
      },
    },

    delete_task: {
      description: "Permanently delete a task.",
      params: z.object({
        task_id: z.string().describe("Task ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        return tdDelete(ctx, `${TODOIST_API}/tasks/${params.task_id}`);
      },
    },

    // ── Projects ──────────────────────────────────────────────────────

    list_projects: {
      description: "List all projects.",
      params: z.object({}),
      returns: z.array(
        z.object({
          id: z.string(),
          name: z.string(),
          color: z.string(),
          parent_id: z.string().nullable(),
          order: z.number(),
          is_favorite: z.boolean(),
          url: z.string(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await tdFetch(ctx, `${TODOIST_API}/projects`);
        return (data ?? []).map((p: any) => ({
          id: p.id,
          name: p.name,
          color: p.color,
          parent_id: p.parent_id ?? null,
          order: p.order,
          is_favorite: p.is_favorite,
          url: p.url,
        }));
      },
    },

    get_project: {
      description: "Get a single project by ID.",
      params: z.object({
        project_id: z.string().describe("Project ID"),
      }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        color: z.string(),
        parent_id: z.string().nullable(),
        order: z.number(),
        is_favorite: z.boolean(),
        is_inbox_project: z.boolean(),
        url: z.string(),
      }),
      execute: async (params, ctx) => {
        const p = await tdFetch(ctx, `${TODOIST_API}/projects/${params.project_id}`);
        return {
          id: p.id,
          name: p.name,
          color: p.color,
          parent_id: p.parent_id ?? null,
          order: p.order,
          is_favorite: p.is_favorite,
          is_inbox_project: p.is_inbox_project,
          url: p.url,
        };
      },
    },

    create_project: {
      description: "Create a new project.",
      params: z.object({
        name: z.string().describe("Project name"),
        color: z.string().optional().describe("Project color"),
        parent_id: z.string().optional().describe("Parent project ID"),
        is_favorite: z.boolean().default(false).describe("Add to favorites"),
      }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        url: z.string(),
      }),
      execute: async (params, ctx) => {
        const payload: any = { name: params.name, is_favorite: params.is_favorite };
        if (params.color !== undefined) payload.color = params.color;
        if (params.parent_id !== undefined) payload.parent_id = params.parent_id;
        const p = await tdPost(ctx, `${TODOIST_API}/projects`, payload);
        return { id: p.id, name: p.name, url: p.url };
      },
    },

    update_project: {
      description: "Update an existing project.",
      params: z.object({
        project_id: z.string().describe("Project ID"),
        name: z.string().optional().describe("New project name"),
        color: z.string().optional().describe("New project color"),
        is_favorite: z.boolean().optional().describe("Update favorite status"),
      }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        url: z.string(),
      }),
      execute: async (params, ctx) => {
        const payload: any = {};
        if (params.name !== undefined) payload.name = params.name;
        if (params.color !== undefined) payload.color = params.color;
        if (params.is_favorite !== undefined) payload.is_favorite = params.is_favorite;
        const p = await tdPost(ctx, `${TODOIST_API}/projects/${params.project_id}`, payload, "POST");
        return { id: p.id, name: p.name, url: p.url };
      },
    },

    delete_project: {
      description: "Permanently delete a project and all its tasks.",
      params: z.object({
        project_id: z.string().describe("Project ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        return tdDelete(ctx, `${TODOIST_API}/projects/${params.project_id}`);
      },
    },

    // ── Sections ──────────────────────────────────────────────────────

    list_sections: {
      description: "List sections, optionally filtered by project.",
      params: z.object({
        project_id: z.string().optional().describe("Filter by project ID"),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          project_id: z.string(),
          order: z.number(),
          name: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const qs = params.project_id ? `?project_id=${params.project_id}` : "";
        const data = await tdFetch(ctx, `${TODOIST_API}/sections${qs}`);
        return (data ?? []).map((s: any) => ({
          id: s.id,
          project_id: s.project_id,
          order: s.order,
          name: s.name,
        }));
      },
    },

    get_section: {
      description: "Get a single section by ID.",
      params: z.object({
        section_id: z.string().describe("Section ID"),
      }),
      returns: z.object({
        id: z.string(),
        project_id: z.string(),
        order: z.number(),
        name: z.string(),
      }),
      execute: async (params, ctx) => {
        const s = await tdFetch(ctx, `${TODOIST_API}/sections/${params.section_id}`);
        return {
          id: s.id,
          project_id: s.project_id,
          order: s.order,
          name: s.name,
        };
      },
    },

    create_section: {
      description: "Create a new section in a project.",
      params: z.object({
        name: z.string().describe("Section name"),
        project_id: z.string().describe("Project to add section to"),
      }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        project_id: z.string(),
      }),
      execute: async (params, ctx) => {
        const s = await tdPost(ctx, `${TODOIST_API}/sections`, {
          name: params.name,
          project_id: params.project_id,
        });
        return { id: s.id, name: s.name, project_id: s.project_id };
      },
    },

    update_section: {
      description: "Rename a section.",
      params: z.object({
        section_id: z.string().describe("Section ID"),
        name: z.string().describe("New section name"),
      }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
      }),
      execute: async (params, ctx) => {
        const s = await tdPost(
          ctx,
          `${TODOIST_API}/sections/${params.section_id}`,
          { name: params.name },
          "POST",
        );
        return { id: s.id, name: s.name };
      },
    },

    delete_section: {
      description: "Permanently delete a section.",
      params: z.object({
        section_id: z.string().describe("Section ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        return tdDelete(ctx, `${TODOIST_API}/sections/${params.section_id}`);
      },
    },

    // ── Comments ──────────────────────────────────────────────────────

    list_comments: {
      description: "List comments for a task or project.",
      params: z.object({
        task_id: z.string().optional().describe("Task ID (mutually exclusive with project_id)"),
        project_id: z.string().optional().describe("Project ID (mutually exclusive with task_id)"),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          content: z.string(),
          posted_at: z.string(),
          task_id: z.string().nullable(),
          project_id: z.string().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const qs = new URLSearchParams();
        if (params.task_id) qs.set("task_id", params.task_id);
        if (params.project_id) qs.set("project_id", params.project_id);
        const query = qs.toString();
        if (!query) throw new Error("Either task_id or project_id is required");
        const data = await tdFetch(ctx, `${TODOIST_API}/comments?${query}`);
        return (data ?? []).map((c: any) => ({
          id: c.id,
          content: c.content,
          posted_at: c.posted_at,
          task_id: c.task_id ?? null,
          project_id: c.project_id ?? null,
        }));
      },
    },

    get_comment: {
      description: "Get a single comment by ID.",
      params: z.object({
        comment_id: z.string().describe("Comment ID"),
      }),
      returns: z.object({
        id: z.string(),
        content: z.string(),
        posted_at: z.string(),
        task_id: z.string().nullable(),
        project_id: z.string().nullable(),
        attachment: z.object({
          file_name: z.string(),
          file_type: z.string(),
          file_url: z.string(),
        }).nullable(),
      }),
      execute: async (params, ctx) => {
        const c = await tdFetch(ctx, `${TODOIST_API}/comments/${params.comment_id}`);
        return {
          id: c.id,
          content: c.content,
          posted_at: c.posted_at,
          task_id: c.task_id ?? null,
          project_id: c.project_id ?? null,
          attachment: c.attachment ?? null,
        };
      },
    },

    create_comment: {
      description: "Add a comment to a task or project.",
      params: z.object({
        content: z.string().describe("Comment content (markdown)"),
        task_id: z.string().optional().describe("Task ID (mutually exclusive with project_id)"),
        project_id: z.string().optional().describe("Project ID (mutually exclusive with task_id)"),
        attachment: z
          .object({
            file_name: z.string().describe("File name"),
            file_type: z.string().describe("MIME type"),
            file_url: z.string().describe("File URL"),
          })
          .optional()
          .describe("File attachment"),
      }),
      returns: z.object({
        id: z.string(),
        content: z.string(),
        posted_at: z.string(),
      }),
      execute: async (params, ctx) => {
        if (!params.task_id && !params.project_id) {
          throw new Error("Either task_id or project_id is required");
        }
        const payload: any = { content: params.content };
        if (params.task_id !== undefined) payload.task_id = params.task_id;
        if (params.project_id !== undefined) payload.project_id = params.project_id;
        if (params.attachment !== undefined) payload.attachment = params.attachment;
        const c = await tdPost(ctx, `${TODOIST_API}/comments`, payload);
        return { id: c.id, content: c.content, posted_at: c.posted_at };
      },
    },

    update_comment: {
      description: "Update a comment's content.",
      params: z.object({
        comment_id: z.string().describe("Comment ID"),
        content: z.string().describe("New comment content"),
      }),
      returns: z.object({
        id: z.string(),
        content: z.string(),
        posted_at: z.string(),
      }),
      execute: async (params, ctx) => {
        const c = await tdPost(
          ctx,
          `${TODOIST_API}/comments/${params.comment_id}`,
          { content: params.content },
          "POST",
        );
        return { id: c.id, content: c.content, posted_at: c.posted_at };
      },
    },

    delete_comment: {
      description: "Permanently delete a comment.",
      params: z.object({
        comment_id: z.string().describe("Comment ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        return tdDelete(ctx, `${TODOIST_API}/comments/${params.comment_id}`);
      },
    },

    // ── Labels ────────────────────────────────────────────────────────

    list_labels: {
      description: "List all personal labels.",
      params: z.object({}),
      returns: z.array(
        z.object({
          id: z.string(),
          name: z.string(),
          color: z.string(),
          order: z.number(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await tdFetch(ctx, `${TODOIST_API}/labels`);
        return (data ?? []).map((l: any) => ({
          id: l.id,
          name: l.name,
          color: l.color,
          order: l.order,
        }));
      },
    },

    create_label: {
      description: "Create a new personal label.",
      params: z.object({
        name: z.string().describe("Label name"),
        color: z.string().optional().describe("Label color"),
        order: z.number().optional().describe("Display order"),
      }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        color: z.string(),
      }),
      execute: async (params, ctx) => {
        const payload: any = { name: params.name };
        if (params.color !== undefined) payload.color = params.color;
        if (params.order !== undefined) payload.order = params.order;
        const l = await tdPost(ctx, `${TODOIST_API}/labels`, payload);
        return { id: l.id, name: l.name, color: l.color };
      },
    },

    update_label: {
      description: "Update a personal label.",
      params: z.object({
        label_id: z.string().describe("Label ID"),
        name: z.string().optional().describe("New label name"),
        color: z.string().optional().describe("New color"),
        order: z.number().optional().describe("New order"),
      }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        color: z.string(),
      }),
      execute: async (params, ctx) => {
        const payload: any = {};
        if (params.name !== undefined) payload.name = params.name;
        if (params.color !== undefined) payload.color = params.color;
        if (params.order !== undefined) payload.order = params.order;
        const l = await tdPost(
          ctx,
          `${TODOIST_API}/labels/${params.label_id}`,
          payload,
          "POST",
        );
        return { id: l.id, name: l.name, color: l.color };
      },
    },

    delete_label: {
      description: "Permanently delete a personal label.",
      params: z.object({
        label_id: z.string().describe("Label ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        return tdDelete(ctx, `${TODOIST_API}/labels/${params.label_id}`);
      },
    },

    // ── Collaborators ─────────────────────────────────────────────────

    list_collaborators: {
      description: "List collaborators for a shared project.",
      params: z.object({
        project_id: z.string().describe("Project ID"),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          name: z.string(),
          email: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await tdFetch(
          ctx,
          `${TODOIST_API}/projects/${params.project_id}/collaborators`,
        );
        return (data ?? []).map((c: any) => ({
          id: c.id,
          name: c.name,
          email: c.email,
        }));
      },
    },
  },
});
