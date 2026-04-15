import { defineSkill, z } from "@harro/skill-sdk";

const BASE = "https://a.klaviyo.com/api";
const REVISION = "2024-10-15";

function kvHeaders(apiKey: string) {
  return {
    Authorization: `Klaviyo-API-Key ${apiKey}`,
    revision: REVISION,
    "Content-Type": "application/json",
    Accept: "application/json",
  };
}

async function kvFetch(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  path: string,
  queryParams?: Record<string, string | number | undefined>,
): Promise<any> {
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
  const res = await ctx.fetch(`${BASE}${path}${qs}`, {
    headers: kvHeaders(ctx.credentials.api_key),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Klaviyo API ${res.status}: ${body}`);
  }
  return res.json();
}

async function kvPost(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  path: string,
  body: unknown,
  method = "POST",
): Promise<any> {
  const res = await ctx.fetch(`${BASE}${path}`, {
    method,
    headers: kvHeaders(ctx.credentials.api_key),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Klaviyo API ${res.status}: ${text}`);
  }
  if (res.status === 202 || res.status === 204) return null;
  return res.json();
}

const profileShape = z.object({
  id: z.string(),
  email: z.string().nullable(),
  first_name: z.string().nullable(),
  last_name: z.string().nullable(),
  phone_number: z.string().nullable(),
  created: z.string(),
  updated: z.string(),
});

function mapProfile(p: any) {
  const a = p.attributes ?? p;
  return {
    id: p.id ?? a.id,
    email: a.email ?? null,
    first_name: a.first_name ?? null,
    last_name: a.last_name ?? null,
    phone_number: a.phone_number ?? null,
    created: a.created ?? "",
    updated: a.updated ?? "",
  };
}

const listShape = z.object({
  id: z.string(),
  name: z.string(),
  created: z.string(),
  updated: z.string(),
});

function mapList(l: any) {
  const a = l.attributes ?? l;
  return { id: l.id, name: a.name, created: a.created ?? "", updated: a.updated ?? "" };
}

import doc from "./SKILL.md";

export default defineSkill({
  name: "klaviyo",
  title: "Klaviyo",
  emoji: "📧",
  description:
    "Manage Klaviyo email marketing: profiles, lists, segments, campaigns, flows, templates, and events.",
  doc,

  credentials: {
    api_key: {
      label: "Private API Key",
      kind: "password",
      placeholder: "pk_...",
      help: "Found in Klaviyo Settings → API Keys. Use a private key (starts with pk_).",
    },
  },

  actions: {
    // ── Profiles ───────────────────────────────────────────────────────

    list_profiles: {
      description: "List profiles with cursor-based pagination.",
      params: z.object({
        page_size: z.number().min(1).max(100).default(20).describe("Results per page"),
        page_cursor: z
          .string()
          .optional()
          .describe("Cursor from a previous response for next page"),
        sort: z
          .string()
          .optional()
          .describe("Sort field e.g. 'created' or '-created' (prefix - for descending)"),
      }),
      returns: z.object({
        profiles: z.array(profileShape),
        next_cursor: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, "/profiles", {
          "page[size]": params.page_size,
          "page[cursor]": params.page_cursor,
          sort: params.sort,
        });
        return {
          profiles: (json.data ?? []).map(mapProfile),
          next_cursor: json.links?.next
            ? new URL(json.links.next).searchParams.get("page[cursor]")
            : null,
        };
      },
    },

    get_profile: {
      description: "Get a single profile by ID.",
      params: z.object({ id: z.string().describe("Profile ID") }),
      returns: profileShape,
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, `/profiles/${params.id}`);
        return mapProfile(json.data);
      },
    },

    create_profile: {
      description: "Create a new profile (subscribe a contact).",
      params: z.object({
        email: z.string().email().describe("Email address"),
        first_name: z.string().optional().describe("First name"),
        last_name: z.string().optional().describe("Last name"),
        phone_number: z.string().optional().describe("E.164 phone number e.g. +15551234567"),
        properties: z.record(z.unknown()).optional().describe("Custom profile properties"),
      }),
      returns: profileShape,
      execute: async (params, ctx) => {
        const { email, first_name, last_name, phone_number, properties } = params;
        const json = await kvPost(ctx, "/profiles", {
          data: {
            type: "profile",
            attributes: { email, first_name, last_name, phone_number, properties },
          },
        });
        return mapProfile(json.data);
      },
    },

    update_profile: {
      description: "Update a profile's attributes.",
      params: z.object({
        id: z.string().describe("Profile ID"),
        email: z.string().email().optional().describe("Email address"),
        first_name: z.string().optional().describe("First name"),
        last_name: z.string().optional().describe("Last name"),
        phone_number: z.string().optional().describe("E.164 phone number"),
        properties: z.record(z.unknown()).optional().describe("Custom profile properties"),
      }),
      returns: profileShape,
      execute: async (params, ctx) => {
        const { id, ...attrs } = params;
        const json = await kvPost(
          ctx,
          `/profiles/${id}`,
          { data: { type: "profile", id, attributes: attrs } },
          "PATCH",
        );
        return mapProfile(json.data);
      },
    },

    search_profiles: {
      description: "Search profiles by exact email address.",
      params: z.object({
        email: z.string().email().describe("Exact email address to search for"),
      }),
      returns: z.array(profileShape),
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, "/profiles", {
          "filter": `equals(email,"${params.email}")`,
        });
        return (json.data ?? []).map(mapProfile);
      },
    },

    // ── Lists ──────────────────────────────────────────────────────────

    list_lists: {
      description: "List all Klaviyo lists.",
      params: z.object({
        page_size: z.number().min(1).max(100).default(20).describe("Results per page"),
        page_cursor: z.string().optional().describe("Pagination cursor"),
      }),
      returns: z.object({
        lists: z.array(listShape),
        next_cursor: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, "/lists", {
          "page[size]": params.page_size,
          "page[cursor]": params.page_cursor,
        });
        return {
          lists: (json.data ?? []).map(mapList),
          next_cursor: json.links?.next
            ? new URL(json.links.next).searchParams.get("page[cursor]")
            : null,
        };
      },
    },

    get_list: {
      description: "Get a list by ID.",
      params: z.object({ id: z.string().describe("List ID") }),
      returns: listShape,
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, `/lists/${params.id}`);
        return mapList(json.data);
      },
    },

    create_list: {
      description: "Create a new list.",
      params: z.object({ name: z.string().describe("List name") }),
      returns: listShape,
      execute: async (params, ctx) => {
        const json = await kvPost(ctx, "/lists", {
          data: { type: "list", attributes: { name: params.name } },
        });
        return mapList(json.data);
      },
    },

    add_profiles_to_list: {
      description: "Add profiles to a list by email addresses (subscribes them).",
      params: z.object({
        list_id: z.string().describe("List ID"),
        emails: z.array(z.string().email()).describe("Email addresses to add"),
      }),
      returns: z.object({ subscribed: z.number() }),
      execute: async (params, ctx) => {
        // First get/create profiles, then add relationships
        const relationships = params.emails.map((email) => ({
          type: "profile",
          attributes: { email },
        }));
        await kvPost(ctx, `/lists/${params.list_id}/relationships/profiles`, {
          data: relationships,
        });
        return { subscribed: params.emails.length };
      },
    },

    // ── Segments ───────────────────────────────────────────────────────

    list_segments: {
      description: "List all segments.",
      params: z.object({
        page_size: z.number().min(1).max(100).default(20).describe("Results per page"),
        page_cursor: z.string().optional().describe("Pagination cursor"),
      }),
      returns: z.object({
        segments: z.array(
          z.object({ id: z.string(), name: z.string(), created: z.string(), updated: z.string() }),
        ),
        next_cursor: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, "/segments", {
          "page[size]": params.page_size,
          "page[cursor]": params.page_cursor,
        });
        return {
          segments: (json.data ?? []).map((s: any) => ({
            id: s.id,
            name: s.attributes?.name ?? "",
            created: s.attributes?.created ?? "",
            updated: s.attributes?.updated ?? "",
          })),
          next_cursor: json.links?.next
            ? new URL(json.links.next).searchParams.get("page[cursor]")
            : null,
        };
      },
    },

    get_segment: {
      description: "Get a segment by ID.",
      params: z.object({ id: z.string().describe("Segment ID") }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        created: z.string(),
        updated: z.string(),
      }),
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, `/segments/${params.id}`);
        const s = json.data;
        return {
          id: s.id,
          name: s.attributes?.name ?? "",
          created: s.attributes?.created ?? "",
          updated: s.attributes?.updated ?? "",
        };
      },
    },

    // ── Campaigns ──────────────────────────────────────────────────────

    list_campaigns: {
      description: "List campaigns filtered by channel.",
      params: z.object({
        channel: z.enum(["email", "sms"]).default("email").describe("Channel filter"),
        page_size: z.number().min(1).max(100).default(20).describe("Results per page"),
        page_cursor: z.string().optional().describe("Pagination cursor"),
      }),
      returns: z.object({
        campaigns: z.array(
          z.object({
            id: z.string(),
            name: z.string(),
            status: z.string(),
            channel: z.string(),
            created_at: z.string(),
            updated_at: z.string(),
          }),
        ),
        next_cursor: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, "/campaigns", {
          "filter": `equals(messages.channel,"${params.channel}")`,
          "page[size]": params.page_size,
          "page[cursor]": params.page_cursor,
        });
        return {
          campaigns: (json.data ?? []).map((c: any) => ({
            id: c.id,
            name: c.attributes?.name ?? "",
            status: c.attributes?.status ?? "",
            channel: params.channel,
            created_at: c.attributes?.created_at ?? "",
            updated_at: c.attributes?.updated_at ?? "",
          })),
          next_cursor: json.links?.next
            ? new URL(json.links.next).searchParams.get("page[cursor]")
            : null,
        };
      },
    },

    get_campaign: {
      description: "Get a campaign by ID.",
      params: z.object({ id: z.string().describe("Campaign ID") }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        status: z.string(),
        send_time: z.string().nullable(),
        created_at: z.string(),
        updated_at: z.string(),
      }),
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, `/campaigns/${params.id}`);
        const c = json.data;
        return {
          id: c.id,
          name: c.attributes?.name ?? "",
          status: c.attributes?.status ?? "",
          send_time: c.attributes?.send_time ?? null,
          created_at: c.attributes?.created_at ?? "",
          updated_at: c.attributes?.updated_at ?? "",
        };
      },
    },

    create_campaign: {
      description: "Create a new email campaign.",
      params: z.object({
        name: z.string().describe("Campaign name"),
        subject: z.string().describe("Email subject line"),
        from_email: z.string().email().describe("Sender email address"),
        from_label: z.string().describe("Sender display name"),
        list_ids: z.array(z.string()).describe("List IDs to send to"),
        template_id: z.string().optional().describe("Template ID to use"),
      }),
      returns: z.object({ id: z.string(), name: z.string(), status: z.string() }),
      execute: async (params, ctx) => {
        const json = await kvPost(ctx, "/campaigns", {
          data: {
            type: "campaign",
            attributes: {
              name: params.name,
              audiences: {
                included: params.list_ids.map((id) => ({ type: "list", id })),
              },
              send_options: { use_smart_sending: true },
              tracking_options: { is_tracking_clicks: true, is_tracking_opens: true },
              message: {
                channel: "email",
                label: params.name,
                content: {
                  subject: params.subject,
                  from_email: params.from_email,
                  from_label: params.from_label,
                  template_id: params.template_id,
                },
              },
            },
          },
        });
        const c = json.data;
        return {
          id: c.id,
          name: c.attributes?.name ?? "",
          status: c.attributes?.status ?? "draft",
        };
      },
    },

    // ── Flows ──────────────────────────────────────────────────────────

    list_flows: {
      description: "List automation flows.",
      params: z.object({
        page_size: z.number().min(1).max(100).default(20).describe("Results per page"),
        page_cursor: z.string().optional().describe("Pagination cursor"),
      }),
      returns: z.object({
        flows: z.array(
          z.object({
            id: z.string(),
            name: z.string(),
            status: z.string(),
            created: z.string(),
            updated: z.string(),
          }),
        ),
        next_cursor: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, "/flows", {
          "page[size]": params.page_size,
          "page[cursor]": params.page_cursor,
        });
        return {
          flows: (json.data ?? []).map((f: any) => ({
            id: f.id,
            name: f.attributes?.name ?? "",
            status: f.attributes?.status ?? "",
            created: f.attributes?.created ?? "",
            updated: f.attributes?.updated ?? "",
          })),
          next_cursor: json.links?.next
            ? new URL(json.links.next).searchParams.get("page[cursor]")
            : null,
        };
      },
    },

    get_flow: {
      description: "Get a flow by ID.",
      params: z.object({ id: z.string().describe("Flow ID") }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        status: z.string(),
        created: z.string(),
        updated: z.string(),
      }),
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, `/flows/${params.id}`);
        const f = json.data;
        return {
          id: f.id,
          name: f.attributes?.name ?? "",
          status: f.attributes?.status ?? "",
          created: f.attributes?.created ?? "",
          updated: f.attributes?.updated ?? "",
        };
      },
    },

    // ── Templates ──────────────────────────────────────────────────────

    list_templates: {
      description: "List email templates.",
      params: z.object({
        page_size: z.number().min(1).max(100).default(20).describe("Results per page"),
        page_cursor: z.string().optional().describe("Pagination cursor"),
      }),
      returns: z.object({
        templates: z.array(
          z.object({
            id: z.string(),
            name: z.string(),
            created: z.string(),
            updated: z.string(),
          }),
        ),
        next_cursor: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, "/templates", {
          "page[size]": params.page_size,
          "page[cursor]": params.page_cursor,
        });
        return {
          templates: (json.data ?? []).map((t: any) => ({
            id: t.id,
            name: t.attributes?.name ?? "",
            created: t.attributes?.created ?? "",
            updated: t.attributes?.updated ?? "",
          })),
          next_cursor: json.links?.next
            ? new URL(json.links.next).searchParams.get("page[cursor]")
            : null,
        };
      },
    },

    get_template: {
      description: "Get a template by ID including HTML and text content.",
      params: z.object({ id: z.string().describe("Template ID") }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        html: z.string().nullable(),
        text: z.string().nullable(),
        created: z.string(),
        updated: z.string(),
      }),
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, `/templates/${params.id}`);
        const t = json.data;
        return {
          id: t.id,
          name: t.attributes?.name ?? "",
          html: t.attributes?.html ?? null,
          text: t.attributes?.text ?? null,
          created: t.attributes?.created ?? "",
          updated: t.attributes?.updated ?? "",
        };
      },
    },

    // ── Events ─────────────────────────────────────────────────────────

    track_event: {
      description: "Track a custom event for a profile.",
      params: z.object({
        event: z.string().describe("Event name (metric name)"),
        email: z.string().email().describe("Profile email address"),
        properties: z.record(z.unknown()).optional().describe("Event properties"),
        value: z.number().optional().describe("Numeric event value e.g. purchase amount"),
        time: z.string().optional().describe("ISO 8601 timestamp, defaults to now"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await kvPost(ctx, "/events", {
          data: {
            type: "event",
            attributes: {
              metric: { data: { type: "metric", attributes: { name: params.event } } },
              profile: { data: { type: "profile", attributes: { email: params.email } } },
              properties: params.properties ?? {},
              value: params.value,
              time: params.time ?? new Date().toISOString(),
            },
          },
        });
        return { success: true };
      },
    },

    list_events: {
      description: "List recent events.",
      params: z.object({
        page_size: z.number().min(1).max(100).default(20).describe("Results per page"),
        page_cursor: z.string().optional().describe("Pagination cursor"),
        sort: z.string().default("-datetime").describe("Sort field e.g. '-datetime'"),
      }),
      returns: z.object({
        events: z.array(
          z.object({
            id: z.string(),
            metric_id: z.string().nullable(),
            profile_id: z.string().nullable(),
            datetime: z.string(),
            value: z.number().nullable(),
          }),
        ),
        next_cursor: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, "/events", {
          "page[size]": params.page_size,
          "page[cursor]": params.page_cursor,
          sort: params.sort,
        });
        return {
          events: (json.data ?? []).map((e: any) => ({
            id: e.id,
            metric_id: e.relationships?.metric?.data?.id ?? null,
            profile_id: e.relationships?.profile?.data?.id ?? null,
            datetime: e.attributes?.datetime ?? "",
            value: e.attributes?.value ?? null,
          })),
          next_cursor: json.links?.next
            ? new URL(json.links.next).searchParams.get("page[cursor]")
            : null,
        };
      },
    },

    // ── Metrics ────────────────────────────────────────────────────────

    list_metrics: {
      description: "List available metrics (event types tracked in Klaviyo).",
      params: z.object({
        page_size: z.number().min(1).max(100).default(50).describe("Results per page"),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          name: z.string(),
          integration: z.string().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const json = await kvFetch(ctx, "/metrics", { "page[size]": params.page_size });
        return (json.data ?? []).map((m: any) => ({
          id: m.id,
          name: m.attributes?.name ?? "",
          integration: m.attributes?.integration?.name ?? null,
        }));
      },
    },
  },
});
