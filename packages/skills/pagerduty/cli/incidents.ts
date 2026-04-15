import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { pdGet, pdPost } from "../core/client.ts";

const incidentRef = z.object({
  id: z.string(),
  summary: z.string().optional(),
  html_url: z.string().optional(),
});

export const incidents: Record<string, ActionDefinition> = {
  list_incidents: {
    description: "List incidents, optionally filtered by status, service, team, or urgency.",
    params: z.object({
      statuses: z.array(z.enum(["triggered", "acknowledged", "resolved"])).optional().describe("Filter by status"),
      service_ids: z.array(z.string()).optional().describe("Filter by service IDs"),
      team_ids: z.array(z.string()).optional().describe("Filter by team IDs"),
      urgency: z.enum(["high", "low"]).optional().describe("Filter by urgency"),
      limit: z.number().int().min(1).max(100).default(25).describe("Max results"),
      offset: z.number().int().default(0).describe("Pagination offset"),
      since: z.string().optional().describe("ISO 8601 start timestamp"),
      until: z.string().optional().describe("ISO 8601 end timestamp"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      title: z.string(),
      status: z.string(),
      urgency: z.string().optional(),
      service: incidentRef.optional(),
      created_at: z.string().optional(),
      html_url: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const qp: Record<string, string | string[]> = {
        limit: String(params.limit),
        offset: String(params.offset),
      };
      if (params.statuses) qp["statuses"] = params.statuses;
      if (params.service_ids) qp["service_ids"] = params.service_ids;
      if (params.team_ids) qp["team_ids"] = params.team_ids;
      if (params.urgency) qp.urgency = params.urgency;
      if (params.since) qp.since = params.since;
      if (params.until) qp.until = params.until;
      const data = await pdGet(ctx.fetch, ctx.credentials.api_key, "/incidents", qp);
      return (data.incidents ?? []).map((i: any) => ({
        id: i.id,
        title: i.title,
        status: i.status,
        urgency: i.urgency,
        service: i.service ? { id: i.service.id, summary: i.service.summary, html_url: i.service.html_url } : undefined,
        created_at: i.created_at,
        html_url: i.html_url,
      }));
    },
  },

  get_incident: {
    description: "Get the full details of an incident.",
    params: z.object({
      incident_id: z.string().describe("Incident ID"),
    }),
    returns: z.object({
      id: z.string(),
      title: z.string(),
      status: z.string(),
      urgency: z.string().optional(),
      service: incidentRef.optional(),
      escalation_policy: incidentRef.optional(),
      assigned_to: z.array(incidentRef).optional(),
      description: z.string().optional(),
      incident_key: z.string().optional(),
      created_at: z.string().optional(),
      last_status_change_at: z.string().optional(),
      html_url: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const data = await pdGet(ctx.fetch, ctx.credentials.api_key, `/incidents/${params.incident_id}`);
      const i = data.incident;
      return {
        id: i.id,
        title: i.title,
        status: i.status,
        urgency: i.urgency,
        service: i.service,
        escalation_policy: i.escalation_policy,
        assigned_to: i.assignments?.map((a: any) => a.assignee) ?? [],
        description: i.body?.details,
        incident_key: i.incident_key,
        created_at: i.created_at,
        last_status_change_at: i.last_status_change_at,
        html_url: i.html_url,
      };
    },
  },

  create_incident: {
    description: "Create a new incident and notify the on-call responder.",
    params: z.object({
      title: z.string().describe("Incident title"),
      service_id: z.string().describe("Service ID to create the incident for"),
      urgency: z.enum(["high", "low"]).optional().describe("Incident urgency"),
      body: z.string().optional().describe("Incident details/description"),
      incident_key: z.string().optional().describe("Dedup key to prevent duplicate incidents"),
      escalation_policy_id: z.string().optional().describe("Override the service's escalation policy"),
    }),
    returns: z.object({ id: z.string(), title: z.string(), status: z.string(), html_url: z.string().optional() }),
    execute: async (params, ctx) => {
      const payload: Record<string, any> = {
        incident: {
          type: "incident",
          title: params.title,
          service: { id: params.service_id, type: "service_reference" },
        },
      };
      if (params.urgency) payload.incident.urgency = params.urgency;
      if (params.body) payload.incident.body = { type: "incident_body", details: params.body };
      if (params.incident_key) payload.incident.incident_key = params.incident_key;
      if (params.escalation_policy_id) {
        payload.incident.escalation_policy = { id: params.escalation_policy_id, type: "escalation_policy_reference" };
      }
      const data = await pdPost(ctx.fetch, ctx.credentials.api_key, "/incidents", payload);
      const i = data.incident;
      return { id: i.id, title: i.title, status: i.status, html_url: i.html_url };
    },
  },

  acknowledge_incident: {
    description: "Acknowledge a triggered incident.",
    params: z.object({ incident_id: z.string().describe("Incident ID") }),
    returns: z.object({ id: z.string(), status: z.string() }),
    execute: async (params, ctx) => {
      const data = await pdPost(ctx.fetch, ctx.credentials.api_key, `/incidents/${params.incident_id}`, {
        incident: { type: "incident", status: "acknowledged" },
      }, "PUT");
      return { id: data.incident.id, status: data.incident.status };
    },
  },

  resolve_incident: {
    description: "Resolve an acknowledged or triggered incident.",
    params: z.object({ incident_id: z.string().describe("Incident ID") }),
    returns: z.object({ id: z.string(), status: z.string(), resolved_at: z.string().optional() }),
    execute: async (params, ctx) => {
      const data = await pdPost(ctx.fetch, ctx.credentials.api_key, `/incidents/${params.incident_id}`, {
        incident: { type: "incident", status: "resolved" },
      }, "PUT");
      const i = data.incident;
      return { id: i.id, status: i.status, resolved_at: i.last_status_change_at };
    },
  },

  add_note: {
    description: "Add a note to an incident.",
    params: z.object({
      incident_id: z.string().describe("Incident ID"),
      content: z.string().describe("Note text"),
    }),
    returns: z.object({ id: z.string(), content: z.string(), created_at: z.string().optional() }),
    execute: async (params, ctx) => {
      const data = await pdPost(ctx.fetch, ctx.credentials.api_key, `/incidents/${params.incident_id}/notes`, {
        note: { content: params.content },
      });
      const n = data.note;
      return { id: n.id, content: n.content, created_at: n.created_at };
    },
  },

  list_alerts: {
    description: "List alerts associated with an incident.",
    params: z.object({ incident_id: z.string().describe("Incident ID") }),
    returns: z.array(z.object({
      id: z.string(),
      status: z.string().optional(),
      severity: z.string().optional(),
      summary: z.string().optional(),
      created_at: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const data = await pdGet(ctx.fetch, ctx.credentials.api_key, `/incidents/${params.incident_id}/alerts`);
      return (data.alerts ?? []).map((a: any) => ({
        id: a.id,
        status: a.status,
        severity: a.severity,
        summary: a.summary,
        created_at: a.created_at,
      }));
    },
  },
};
