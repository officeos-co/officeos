import { defineSkill, z } from "@harro/skill-sdk";

import manifest from "./skill.json" with { type: "json" };
const BASE = "https://api.pipedrive.com/v1";

function qs(token: string, extra?: Record<string, string | number | boolean | undefined>) {
  const params: Record<string, string> = { api_token: token };
  for (const [k, v] of Object.entries(extra ?? {})) {
    if (v !== undefined) params[k] = String(v);
  }
  return "?" + new URLSearchParams(params).toString();
}

async function pdFetch(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  path: string,
  queryExtra?: Record<string, string | number | boolean | undefined>,
): Promise<any> {
  const url = `${BASE}${path}${qs(ctx.credentials.api_token, queryExtra)}`;
  const res = await ctx.fetch(url);
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Pipedrive API ${res.status}: ${body}`);
  }
  const json = await res.json();
  if (!json.success) throw new Error(`Pipedrive error: ${json.error ?? JSON.stringify(json)}`);
  return json.data;
}

async function pdPost(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  path: string,
  body: unknown,
  method = "POST",
): Promise<any> {
  const url = `${BASE}${path}${qs(ctx.credentials.api_token)}`;
  const res = await ctx.fetch(url, {
    method,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Pipedrive API ${res.status}: ${text}`);
  }
  const json = await res.json();
  if (!json.success) throw new Error(`Pipedrive error: ${json.error ?? JSON.stringify(json)}`);
  return json.data;
}

async function pdDelete(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  path: string,
): Promise<any> {
  const url = `${BASE}${path}${qs(ctx.credentials.api_token)}`;
  const res = await ctx.fetch(url, { method: "DELETE" });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Pipedrive API ${res.status}: ${text}`);
  }
  const json = await res.json();
  return json.data ?? { success: json.success };
}

const dealShape = z.object({
  id: z.number(),
  title: z.string(),
  status: z.string(),
  value: z.number().nullable(),
  currency: z.string().nullable(),
  stage_id: z.number().nullable(),
  pipeline_id: z.number().nullable(),
  add_time: z.string(),
  update_time: z.string().nullable(),
});

const personShape = z.object({
  id: z.number(),
  name: z.string(),
  email: z.array(z.object({ value: z.string(), primary: z.boolean() })),
  phone: z.array(z.object({ value: z.string(), primary: z.boolean() })),
  org_id: z.number().nullable(),
  add_time: z.string(),
});

const orgShape = z.object({
  id: z.number(),
  name: z.string(),
  address: z.string().nullable(),
  people_count: z.number(),
  add_time: z.string(),
});

const activityShape = z.object({
  id: z.number(),
  subject: z.string(),
  type: z.string(),
  done: z.boolean(),
  due_date: z.string().nullable(),
  due_time: z.string().nullable(),
  deal_id: z.number().nullable(),
  person_id: z.number().nullable(),
  note: z.string().nullable(),
});

import doc from "./SKILL.md";

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Deals ──────────────────────────────────────────────────────────

    list_deals: {
      description: "List deals with optional status filter and pagination.",
      params: z.object({
        status: z
          .enum(["open", "won", "lost", "all"])
          .default("open")
          .describe("Filter by deal status"),
        start: z.number().min(0).default(0).describe("Pagination start offset"),
        limit: z.number().min(1).max(500).default(50).describe("Max results per page"),
        sort: z.string().optional().describe("Sort expression e.g. 'add_time DESC'"),
      }),
      returns: z.array(dealShape),
      execute: async (params, ctx) => {
        const data = await pdFetch(ctx, "/deals", {
          status: params.status,
          start: params.start,
          limit: params.limit,
          sort: params.sort,
        });
        return (data ?? []).map((d: any) => ({
          id: d.id,
          title: d.title,
          status: d.status,
          value: d.value,
          currency: d.currency,
          stage_id: d.stage_id,
          pipeline_id: d.pipeline_id,
          add_time: d.add_time,
          update_time: d.update_time,
        }));
      },
    },

    get_deal: {
      description: "Get a single deal by ID.",
      params: z.object({
        id: z.number().describe("Deal ID"),
      }),
      returns: dealShape,
      execute: async (params, ctx) => {
        const d = await pdFetch(ctx, `/deals/${params.id}`);
        return {
          id: d.id,
          title: d.title,
          status: d.status,
          value: d.value,
          currency: d.currency,
          stage_id: d.stage_id,
          pipeline_id: d.pipeline_id,
          add_time: d.add_time,
          update_time: d.update_time,
        };
      },
    },

    create_deal: {
      description: "Create a new deal in Pipedrive.",
      params: z.object({
        title: z.string().describe("Deal title"),
        value: z.number().optional().describe("Deal value"),
        currency: z.string().optional().describe("Currency code e.g. USD"),
        person_id: z.number().optional().describe("Associated person ID"),
        org_id: z.number().optional().describe("Associated organization ID"),
        pipeline_id: z.number().optional().describe("Pipeline ID"),
        stage_id: z.number().optional().describe("Stage ID"),
        status: z.enum(["open", "won", "lost"]).optional().describe("Deal status"),
        expected_close_date: z.string().optional().describe("Expected close date YYYY-MM-DD"),
      }),
      returns: dealShape,
      execute: async (params, ctx) => {
        const d = await pdPost(ctx, "/deals", params);
        return {
          id: d.id,
          title: d.title,
          status: d.status,
          value: d.value,
          currency: d.currency,
          stage_id: d.stage_id,
          pipeline_id: d.pipeline_id,
          add_time: d.add_time,
          update_time: d.update_time,
        };
      },
    },

    update_deal: {
      description: "Update an existing deal.",
      params: z.object({
        id: z.number().describe("Deal ID"),
        title: z.string().optional().describe("Deal title"),
        value: z.number().optional().describe("Deal value"),
        currency: z.string().optional().describe("Currency code"),
        person_id: z.number().optional().describe("Associated person ID"),
        org_id: z.number().optional().describe("Associated organization ID"),
        pipeline_id: z.number().optional().describe("Pipeline ID"),
        stage_id: z.number().optional().describe("Stage ID"),
        status: z.enum(["open", "won", "lost"]).optional().describe("Deal status"),
        expected_close_date: z.string().optional().describe("Expected close date YYYY-MM-DD"),
      }),
      returns: dealShape,
      execute: async (params, ctx) => {
        const { id, ...body } = params;
        const d = await pdPost(ctx, `/deals/${id}`, body, "PUT");
        return {
          id: d.id,
          title: d.title,
          status: d.status,
          value: d.value,
          currency: d.currency,
          stage_id: d.stage_id,
          pipeline_id: d.pipeline_id,
          add_time: d.add_time,
          update_time: d.update_time,
        };
      },
    },

    delete_deal: {
      description: "Delete a deal by ID.",
      params: z.object({
        id: z.number().describe("Deal ID"),
      }),
      returns: z.object({ id: z.number(), success: z.boolean() }),
      execute: async (params, ctx) => {
        const data = await pdDelete(ctx, `/deals/${params.id}`);
        return { id: data?.id ?? params.id, success: true };
      },
    },

    search_deals: {
      description: "Search deals by term.",
      params: z.object({
        term: z.string().describe("Search term"),
        exact_match: z.boolean().default(false).describe("Require exact match"),
        limit: z.number().min(1).max(500).default(50).describe("Max results"),
      }),
      returns: z.array(
        z.object({
          result_score: z.number(),
          item: z.object({ id: z.number(), title: z.string(), status: z.string() }),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await pdFetch(ctx, "/deals/search", {
          term: params.term,
          exact_match: params.exact_match,
          limit: params.limit,
        });
        return (data?.items ?? []).map((i: any) => ({
          result_score: i.result_score,
          item: { id: i.item.id, title: i.item.title, status: i.item.status },
        }));
      },
    },

    // ── Persons ────────────────────────────────────────────────────────

    list_persons: {
      description: "List persons (contacts) in Pipedrive.",
      params: z.object({
        start: z.number().min(0).default(0).describe("Pagination start offset"),
        limit: z.number().min(1).max(500).default(50).describe("Max results"),
      }),
      returns: z.array(personShape),
      execute: async (params, ctx) => {
        const data = await pdFetch(ctx, "/persons", { start: params.start, limit: params.limit });
        return (data ?? []).map((p: any) => ({
          id: p.id,
          name: p.name,
          email: p.email ?? [],
          phone: p.phone ?? [],
          org_id: p.org_id?.value ?? null,
          add_time: p.add_time,
        }));
      },
    },

    get_person: {
      description: "Get a person (contact) by ID.",
      params: z.object({ id: z.number().describe("Person ID") }),
      returns: personShape,
      execute: async (params, ctx) => {
        const p = await pdFetch(ctx, `/persons/${params.id}`);
        return {
          id: p.id,
          name: p.name,
          email: p.email ?? [],
          phone: p.phone ?? [],
          org_id: p.org_id?.value ?? null,
          add_time: p.add_time,
        };
      },
    },

    create_person: {
      description: "Create a new person (contact).",
      params: z.object({
        name: z.string().describe("Full name"),
        email: z.string().optional().describe("Email address"),
        phone: z.string().optional().describe("Phone number"),
        org_id: z.number().optional().describe("Associated organization ID"),
      }),
      returns: personShape,
      execute: async (params, ctx) => {
        const p = await pdPost(ctx, "/persons", {
          name: params.name,
          email: params.email ? [{ value: params.email, primary: true }] : undefined,
          phone: params.phone ? [{ value: params.phone, primary: true }] : undefined,
          org_id: params.org_id,
        });
        return {
          id: p.id,
          name: p.name,
          email: p.email ?? [],
          phone: p.phone ?? [],
          org_id: p.org_id?.value ?? null,
          add_time: p.add_time,
        };
      },
    },

    update_person: {
      description: "Update a person.",
      params: z.object({
        id: z.number().describe("Person ID"),
        name: z.string().optional().describe("Full name"),
        email: z.string().optional().describe("Email address"),
        phone: z.string().optional().describe("Phone number"),
        org_id: z.number().optional().describe("Associated organization ID"),
      }),
      returns: personShape,
      execute: async (params, ctx) => {
        const { id, email, phone, ...rest } = params;
        const body: any = { ...rest };
        if (email) body.email = [{ value: email, primary: true }];
        if (phone) body.phone = [{ value: phone, primary: true }];
        const p = await pdPost(ctx, `/persons/${id}`, body, "PUT");
        return {
          id: p.id,
          name: p.name,
          email: p.email ?? [],
          phone: p.phone ?? [],
          org_id: p.org_id?.value ?? null,
          add_time: p.add_time,
        };
      },
    },

    // ── Organizations ──────────────────────────────────────────────────

    list_organizations: {
      description: "List organizations in Pipedrive.",
      params: z.object({
        start: z.number().min(0).default(0).describe("Pagination start offset"),
        limit: z.number().min(1).max(500).default(50).describe("Max results"),
      }),
      returns: z.array(orgShape),
      execute: async (params, ctx) => {
        const data = await pdFetch(ctx, "/organizations", {
          start: params.start,
          limit: params.limit,
        });
        return (data ?? []).map((o: any) => ({
          id: o.id,
          name: o.name,
          address: o.address ?? null,
          people_count: o.people_count ?? 0,
          add_time: o.add_time,
        }));
      },
    },

    get_organization: {
      description: "Get an organization by ID.",
      params: z.object({ id: z.number().describe("Organization ID") }),
      returns: orgShape,
      execute: async (params, ctx) => {
        const o = await pdFetch(ctx, `/organizations/${params.id}`);
        return {
          id: o.id,
          name: o.name,
          address: o.address ?? null,
          people_count: o.people_count ?? 0,
          add_time: o.add_time,
        };
      },
    },

    create_organization: {
      description: "Create a new organization.",
      params: z.object({
        name: z.string().describe("Organization name"),
        address: z.string().optional().describe("Address"),
      }),
      returns: orgShape,
      execute: async (params, ctx) => {
        const o = await pdPost(ctx, "/organizations", params);
        return {
          id: o.id,
          name: o.name,
          address: o.address ?? null,
          people_count: o.people_count ?? 0,
          add_time: o.add_time,
        };
      },
    },

    // ── Activities ─────────────────────────────────────────────────────

    list_activities: {
      description: "List activities with optional filters.",
      params: z.object({
        start: z.number().min(0).default(0).describe("Pagination start offset"),
        limit: z.number().min(1).max(500).default(50).describe("Max results"),
        done: z.boolean().optional().describe("Filter by done status"),
        type: z.string().optional().describe("Activity type: call, meeting, task, etc."),
      }),
      returns: z.array(activityShape),
      execute: async (params, ctx) => {
        const data = await pdFetch(ctx, "/activities", {
          start: params.start,
          limit: params.limit,
          done: params.done !== undefined ? (params.done ? 1 : 0) : undefined,
          type: params.type,
        });
        return (data ?? []).map((a: any) => ({
          id: a.id,
          subject: a.subject,
          type: a.type,
          done: !!a.done,
          due_date: a.due_date ?? null,
          due_time: a.due_time ?? null,
          deal_id: a.deal_id ?? null,
          person_id: a.person_id ?? null,
          note: a.note ?? null,
        }));
      },
    },

    create_activity: {
      description: "Create a new activity.",
      params: z.object({
        subject: z.string().describe("Activity subject"),
        type: z
          .enum(["call", "meeting", "task", "deadline", "email", "lunch"])
          .describe("Activity type"),
        due_date: z.string().optional().describe("Due date YYYY-MM-DD"),
        due_time: z.string().optional().describe("Due time HH:MM"),
        duration: z.string().optional().describe("Duration HH:MM"),
        deal_id: z.number().optional().describe("Associated deal ID"),
        person_id: z.number().optional().describe("Associated person ID"),
        note: z.string().optional().describe("Note text"),
      }),
      returns: activityShape,
      execute: async (params, ctx) => {
        const a = await pdPost(ctx, "/activities", params);
        return {
          id: a.id,
          subject: a.subject,
          type: a.type,
          done: !!a.done,
          due_date: a.due_date ?? null,
          due_time: a.due_time ?? null,
          deal_id: a.deal_id ?? null,
          person_id: a.person_id ?? null,
          note: a.note ?? null,
        };
      },
    },

    update_activity: {
      description: "Update an activity (e.g. mark as done).",
      params: z.object({
        id: z.number().describe("Activity ID"),
        subject: z.string().optional().describe("Activity subject"),
        type: z
          .enum(["call", "meeting", "task", "deadline", "email", "lunch"])
          .optional()
          .describe("Activity type"),
        done: z.boolean().optional().describe("Mark as done"),
        due_date: z.string().optional().describe("Due date YYYY-MM-DD"),
        due_time: z.string().optional().describe("Due time HH:MM"),
        note: z.string().optional().describe("Note text"),
      }),
      returns: activityShape,
      execute: async (params, ctx) => {
        const { id, ...body } = params;
        const a = await pdPost(ctx, `/activities/${id}`, body, "PUT");
        return {
          id: a.id,
          subject: a.subject,
          type: a.type,
          done: !!a.done,
          due_date: a.due_date ?? null,
          due_time: a.due_time ?? null,
          deal_id: a.deal_id ?? null,
          person_id: a.person_id ?? null,
          note: a.note ?? null,
        };
      },
    },

    // ── Pipelines & Stages ─────────────────────────────────────────────

    list_pipelines: {
      description: "List all pipelines.",
      params: z.object({}),
      returns: z.array(
        z.object({
          id: z.number(),
          name: z.string(),
          active: z.boolean(),
          order_nr: z.number(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await pdFetch(ctx, "/pipelines");
        return (data ?? []).map((p: any) => ({
          id: p.id,
          name: p.name,
          active: p.active,
          order_nr: p.order_nr,
        }));
      },
    },

    list_stages: {
      description: "List stages, optionally filtered by pipeline.",
      params: z.object({
        pipeline_id: z.number().optional().describe("Filter by pipeline ID"),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          name: z.string(),
          pipeline_id: z.number(),
          order_nr: z.number(),
          active_flag: z.boolean(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await pdFetch(ctx, "/stages", { pipeline_id: params.pipeline_id });
        return (data ?? []).map((s: any) => ({
          id: s.id,
          name: s.name,
          pipeline_id: s.pipeline_id,
          order_nr: s.order_nr,
          active_flag: s.active_flag,
        }));
      },
    },

    // ── Search ─────────────────────────────────────────────────────────

    search: {
      description: "Search across all Pipedrive item types.",
      params: z.object({
        term: z.string().min(2).describe("Search term (min 2 chars)"),
        item_type: z
          .enum(["deal", "person", "organization", "activity"])
          .optional()
          .describe("Limit search to this item type"),
        exact_match: z.boolean().default(false).describe("Require exact match"),
        limit: z.number().min(1).max(500).default(50).describe("Max results"),
      }),
      returns: z.array(
        z.object({
          result_score: z.number(),
          item_type: z.string(),
          item: z.record(z.unknown()),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await pdFetch(ctx, "/itemSearch", {
          term: params.term,
          item_types: params.item_type,
          exact_match: params.exact_match,
          limit: params.limit,
        });
        return (data?.items ?? []).map((i: any) => ({
          result_score: i.result_score,
          item_type: i.item?.type ?? "unknown",
          item: i.item,
        }));
      },
    },
  },
});
