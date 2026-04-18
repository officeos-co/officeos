import { defineSkill, z } from "@harro/skill-sdk";

import manifest from "./skill.json" with { type: "json" };
function zdBase(subdomain: string) {
  return `https://${subdomain}.zendesk.com/api/v2`;
}

function zdAuth(email: string, api_token: string) {
  return "Basic " + btoa(`${email}/token:${api_token}`);
}

async function zdFetch(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  path: string,
  queryParams?: Record<string, string | number | boolean | undefined>,
): Promise<any> {
  const { subdomain, email, api_token } = ctx.credentials;
  const qs = queryParams
    ? "?" +
      new URLSearchParams(
        Object.fromEntries(
          Object.entries(queryParams)
            .filter(([, v]) => v !== undefined)
            .map(([k, v]) => [k, String(v)]),
        ),
      ).toString()
    : "";
  const res = await ctx.fetch(`${zdBase(subdomain)}${path}${qs}`, {
    headers: {
      Authorization: zdAuth(email, api_token),
      "Content-Type": "application/json",
    },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Zendesk API ${res.status}: ${body}`);
  }
  if (res.status === 204) return null;
  return res.json();
}

async function zdPost(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  path: string,
  body: unknown,
  method = "POST",
): Promise<any> {
  const { subdomain, email, api_token } = ctx.credentials;
  const res = await ctx.fetch(`${zdBase(subdomain)}${path}`, {
    method,
    headers: {
      Authorization: zdAuth(email, api_token),
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Zendesk API ${res.status}: ${text}`);
  }
  if (res.status === 204) return null;
  return res.json();
}

const ticketShape = z.object({
  id: z.number(),
  subject: z.string(),
  status: z.string(),
  priority: z.string().nullable(),
  type: z.string().nullable(),
  requester_id: z.number(),
  assignee_id: z.number().nullable(),
  group_id: z.number().nullable(),
  tags: z.array(z.string()),
  created_at: z.string(),
  updated_at: z.string(),
});

const userShape = z.object({
  id: z.number(),
  name: z.string(),
  email: z.string(),
  role: z.string(),
  active: z.boolean(),
  created_at: z.string(),
});

const orgShape = z.object({
  id: z.number(),
  name: z.string(),
  domain_names: z.array(z.string()),
  created_at: z.string(),
});

function mapTicket(t: any) {
  return {
    id: t.id,
    subject: t.subject ?? "",
    status: t.status,
    priority: t.priority ?? null,
    type: t.type ?? null,
    requester_id: t.requester_id,
    assignee_id: t.assignee_id ?? null,
    group_id: t.group_id ?? null,
    tags: t.tags ?? [],
    created_at: t.created_at,
    updated_at: t.updated_at,
  };
}

import doc from "./SKILL.md";

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Tickets ────────────────────────────────────────────────────────

    list_tickets: {
      description: "List tickets with optional status filter and pagination.",
      params: z.object({
        status: z
          .enum(["new", "open", "pending", "hold", "solved", "closed"])
          .optional()
          .describe("Filter by ticket status"),
        page: z.number().min(1).default(1).describe("Page number"),
        per_page: z.number().min(1).max(100).default(25).describe("Results per page"),
        sort_by: z.string().default("created_at").describe("Field to sort by"),
        sort_order: z.enum(["asc", "desc"]).default("desc").describe("Sort direction"),
      }),
      returns: z.array(ticketShape),
      execute: async (params, ctx) => {
        const json = await zdFetch(ctx, "/tickets", {
          status: params.status,
          page: params.page,
          per_page: params.per_page,
          sort_by: params.sort_by,
          sort_order: params.sort_order,
        });
        return (json.tickets ?? []).map(mapTicket);
      },
    },

    get_ticket: {
      description: "Get a single ticket by ID.",
      params: z.object({ id: z.number().describe("Ticket ID") }),
      returns: ticketShape,
      execute: async (params, ctx) => {
        const json = await zdFetch(ctx, `/tickets/${params.id}`);
        return mapTicket(json.ticket);
      },
    },

    create_ticket: {
      description: "Create a new support ticket.",
      params: z.object({
        subject: z.string().describe("Ticket subject"),
        body: z.string().describe("Initial comment body (plain text or HTML)"),
        requester_email: z.string().optional().describe("Requester email address"),
        requester_name: z.string().optional().describe("Requester display name"),
        priority: z
          .enum(["urgent", "high", "normal", "low"])
          .optional()
          .describe("Ticket priority"),
        type: z
          .enum(["problem", "incident", "question", "task"])
          .optional()
          .describe("Ticket type"),
        tags: z.array(z.string()).optional().describe("Tags to apply"),
        assignee_id: z.number().optional().describe("Assignee user ID"),
        group_id: z.number().optional().describe("Group ID"),
      }),
      returns: ticketShape,
      execute: async (params, ctx) => {
        const ticket: any = {
          subject: params.subject,
          comment: { body: params.body },
          priority: params.priority,
          type: params.type,
          tags: params.tags,
          assignee_id: params.assignee_id,
          group_id: params.group_id,
        };
        if (params.requester_email) {
          ticket.requester = { email: params.requester_email, name: params.requester_name };
        }
        const json = await zdPost(ctx, "/tickets", { ticket });
        return mapTicket(json.ticket);
      },
    },

    update_ticket: {
      description: "Update a ticket's fields or status.",
      params: z.object({
        id: z.number().describe("Ticket ID"),
        status: z
          .enum(["new", "open", "pending", "hold", "solved", "closed"])
          .optional()
          .describe("New status"),
        priority: z
          .enum(["urgent", "high", "normal", "low"])
          .optional()
          .describe("Priority"),
        assignee_id: z.number().optional().describe("Assignee user ID"),
        tags: z.array(z.string()).optional().describe("Tags (replaces existing)"),
        subject: z.string().optional().describe("Ticket subject"),
        comment: z.string().optional().describe("Add a public comment"),
      }),
      returns: ticketShape,
      execute: async (params, ctx) => {
        const { id, comment, ...rest } = params;
        const ticket: any = { ...rest };
        if (comment) ticket.comment = { body: comment, public: true };
        const json = await zdPost(ctx, `/tickets/${id}`, { ticket }, "PUT");
        return mapTicket(json.ticket);
      },
    },

    delete_ticket: {
      description: "Delete (soft-delete) a ticket.",
      params: z.object({ id: z.number().describe("Ticket ID") }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await zdPost(ctx, `/tickets/${params.id}`, {}, "DELETE");
        return { success: true };
      },
    },

    // ── Comments ───────────────────────────────────────────────────────

    list_comments: {
      description: "List all comments on a ticket.",
      params: z.object({ ticket_id: z.number().describe("Ticket ID") }),
      returns: z.array(
        z.object({
          id: z.number(),
          type: z.string(),
          author_id: z.number(),
          body: z.string(),
          public: z.boolean(),
          created_at: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const json = await zdFetch(ctx, `/tickets/${params.ticket_id}/comments`);
        return (json.comments ?? []).map((c: any) => ({
          id: c.id,
          type: c.type,
          author_id: c.author_id,
          body: c.body ?? "",
          public: c.public,
          created_at: c.created_at,
        }));
      },
    },

    add_comment: {
      description: "Add a public comment or internal note to a ticket.",
      params: z.object({
        ticket_id: z.number().describe("Ticket ID"),
        body: z.string().describe("Comment body"),
        public: z.boolean().default(true).describe("Public comment (true) or internal note"),
      }),
      returns: ticketShape,
      execute: async (params, ctx) => {
        const json = await zdPost(
          ctx,
          `/tickets/${params.ticket_id}`,
          { ticket: { comment: { body: params.body, public: params.public } } },
          "PUT",
        );
        return mapTicket(json.ticket);
      },
    },

    // ── Users ──────────────────────────────────────────────────────────

    list_users: {
      description: "List users with optional role filter.",
      params: z.object({
        role: z
          .enum(["end-user", "agent", "admin"])
          .optional()
          .describe("Filter by role"),
        page: z.number().min(1).default(1).describe("Page number"),
        per_page: z.number().min(1).max(100).default(25).describe("Results per page"),
      }),
      returns: z.array(userShape),
      execute: async (params, ctx) => {
        const json = await zdFetch(ctx, "/users", {
          role: params.role,
          page: params.page,
          per_page: params.per_page,
        });
        return (json.users ?? []).map((u: any) => ({
          id: u.id,
          name: u.name,
          email: u.email,
          role: u.role,
          active: u.active,
          created_at: u.created_at,
        }));
      },
    },

    get_user: {
      description: "Get a user by ID.",
      params: z.object({ id: z.number().describe("User ID") }),
      returns: userShape,
      execute: async (params, ctx) => {
        const json = await zdFetch(ctx, `/users/${params.id}`);
        const u = json.user;
        return {
          id: u.id,
          name: u.name,
          email: u.email,
          role: u.role,
          active: u.active,
          created_at: u.created_at,
        };
      },
    },

    create_user: {
      description: "Create or update a user (upsert by email).",
      params: z.object({
        name: z.string().describe("Display name"),
        email: z.string().describe("Email address"),
        role: z.enum(["end-user", "agent"]).default("end-user").describe("User role"),
        phone: z.string().optional().describe("Phone number"),
        organization_id: z.number().optional().describe("Organization ID"),
      }),
      returns: userShape,
      execute: async (params, ctx) => {
        const json = await zdPost(ctx, "/users/create_or_update", { user: params });
        const u = json.user;
        return {
          id: u.id,
          name: u.name,
          email: u.email,
          role: u.role,
          active: u.active,
          created_at: u.created_at,
        };
      },
    },

    // ── Organizations ──────────────────────────────────────────────────

    list_organizations: {
      description: "List organizations.",
      params: z.object({
        page: z.number().min(1).default(1).describe("Page number"),
        per_page: z.number().min(1).max(100).default(25).describe("Results per page"),
      }),
      returns: z.array(orgShape),
      execute: async (params, ctx) => {
        const json = await zdFetch(ctx, "/organizations", {
          page: params.page,
          per_page: params.per_page,
        });
        return (json.organizations ?? []).map((o: any) => ({
          id: o.id,
          name: o.name,
          domain_names: o.domain_names ?? [],
          created_at: o.created_at,
        }));
      },
    },

    get_organization: {
      description: "Get an organization by ID.",
      params: z.object({ id: z.number().describe("Organization ID") }),
      returns: orgShape,
      execute: async (params, ctx) => {
        const json = await zdFetch(ctx, `/organizations/${params.id}`);
        const o = json.organization;
        return {
          id: o.id,
          name: o.name,
          domain_names: o.domain_names ?? [],
          created_at: o.created_at,
        };
      },
    },

    // ── Search ─────────────────────────────────────────────────────────

    search: {
      description: "Search across tickets, users, and organizations using Zendesk query syntax.",
      params: z.object({
        query: z
          .string()
          .describe(
            "Zendesk search query. Supports type:ticket, field filters, e.g. 'status:open priority:high'",
          ),
        page: z.number().min(1).default(1).describe("Page number"),
        per_page: z.number().min(1).max(100).default(25).describe("Results per page"),
        sort_by: z.string().optional().describe("Field to sort by"),
        sort_order: z.enum(["asc", "desc"]).optional().describe("Sort direction"),
      }),
      returns: z.array(z.object({ result_type: z.string(), item: z.record(z.unknown()) })),
      execute: async (params, ctx) => {
        const json = await zdFetch(ctx, "/search", {
          query: params.query,
          page: params.page,
          per_page: params.per_page,
          sort_by: params.sort_by,
          sort_order: params.sort_order,
        });
        return (json.results ?? []).map((r: any) => ({
          result_type: r.result_type,
          item: r,
        }));
      },
    },

    // ── Views ──────────────────────────────────────────────────────────

    list_views: {
      description: "List all views (saved ticket filters).",
      params: z.object({}),
      returns: z.array(
        z.object({
          id: z.number(),
          title: z.string(),
          active: z.boolean(),
          position: z.number(),
        }),
      ),
      execute: async (_params, ctx) => {
        const json = await zdFetch(ctx, "/views");
        return (json.views ?? []).map((v: any) => ({
          id: v.id,
          title: v.title,
          active: v.active,
          position: v.position,
        }));
      },
    },

    execute_view: {
      description: "Get tickets matching a saved view.",
      params: z.object({
        view_id: z.number().describe("View ID"),
        page: z.number().min(1).default(1).describe("Page number"),
        per_page: z.number().min(1).max(100).default(25).describe("Results per page"),
      }),
      returns: z.array(ticketShape),
      execute: async (params, ctx) => {
        const json = await zdFetch(ctx, `/views/${params.view_id}/tickets`, {
          page: params.page,
          per_page: params.per_page,
        });
        return (json.tickets ?? []).map(mapTicket);
      },
    },

    // ── Macros ─────────────────────────────────────────────────────────

    list_macros: {
      description: "List available macros.",
      params: z.object({
        active: z.boolean().default(true).describe("Filter by active status"),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          title: z.string(),
          active: z.boolean(),
          actions: z.array(z.object({ field: z.string(), value: z.unknown() })),
        }),
      ),
      execute: async (params, ctx) => {
        const json = await zdFetch(ctx, "/macros", { active: params.active });
        return (json.macros ?? []).map((m: any) => ({
          id: m.id,
          title: m.title,
          active: m.active,
          actions: m.actions ?? [],
        }));
      },
    },

    apply_macro: {
      description: "Apply a macro to a ticket.",
      params: z.object({
        ticket_id: z.number().describe("Ticket ID"),
        macro_id: z.number().describe("Macro ID"),
      }),
      returns: ticketShape,
      execute: async (params, ctx) => {
        const json = await zdPost(ctx, `/tickets/${params.ticket_id}/macros/${params.macro_id}/apply`, {});
        return mapTicket(json.result.ticket);
      },
    },
  },
});
