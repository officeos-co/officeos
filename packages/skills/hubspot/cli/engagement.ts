import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { hsGet, hsPost } from "../core/client.ts";

export const engagement: Record<string, ActionDefinition> = {
  create_note: {
    description: "Create a note engagement and associate it with CRM objects.",
    params: z.object({
      body: z.string().describe("Note content (supports HTML)"),
      contact_ids: z.array(z.string()).optional().describe("Contact IDs to associate"),
      company_ids: z.array(z.string()).optional().describe("Company IDs to associate"),
      deal_ids: z.array(z.string()).optional().describe("Deal IDs to associate"),
    }),
    returns: z.object({ id: z.string(), createdAt: z.string() }),
    execute: async (params, ctx) => {
      const associations: any[] = [];
      for (const id of params.contact_ids ?? []) {
        associations.push({ to: { id }, types: [{ associationCategory: "HUBSPOT_DEFINED", associationTypeId: 202 }] });
      }
      for (const id of params.company_ids ?? []) {
        associations.push({ to: { id }, types: [{ associationCategory: "HUBSPOT_DEFINED", associationTypeId: 190 }] });
      }
      for (const id of params.deal_ids ?? []) {
        associations.push({ to: { id }, types: [{ associationCategory: "HUBSPOT_DEFINED", associationTypeId: 214 }] });
      }
      const r = await hsPost(ctx, "/crm/v3/objects/notes", {
        properties: { hs_note_body: params.body, hs_timestamp: new Date().toISOString() },
        associations: associations.length > 0 ? associations : undefined,
      });
      return { id: r.id, createdAt: r.createdAt };
    },
  },

  create_task: {
    description: "Create a task engagement and associate it with CRM objects.",
    params: z.object({
      subject: z.string().describe("Task subject"),
      body: z.string().optional().describe("Task body"),
      due_date: z.string().optional().describe("Due date (YYYY-MM-DD)"),
      priority: z.enum(["LOW", "MEDIUM", "HIGH"]).optional().describe("Task priority"),
      status: z.enum(["NOT_STARTED", "IN_PROGRESS", "COMPLETED"]).optional().describe("Task status"),
      contact_ids: z.array(z.string()).optional().describe("Contact IDs to associate"),
      company_ids: z.array(z.string()).optional().describe("Company IDs to associate"),
      deal_ids: z.array(z.string()).optional().describe("Deal IDs to associate"),
    }),
    returns: z.object({ id: z.string(), createdAt: z.string() }),
    execute: async (params, ctx) => {
      const props: Record<string, string> = {
        hs_task_subject: params.subject,
        hs_timestamp: new Date().toISOString(),
      };
      if (params.body) props.hs_task_body = params.body;
      if (params.due_date) props.hs_task_due_date = params.due_date;
      if (params.priority) props.hs_task_priority = params.priority;
      if (params.status) props.hs_task_status = params.status ?? "NOT_STARTED";

      const associations: any[] = [];
      for (const id of params.contact_ids ?? []) {
        associations.push({ to: { id }, types: [{ associationCategory: "HUBSPOT_DEFINED", associationTypeId: 204 }] });
      }
      for (const id of params.company_ids ?? []) {
        associations.push({ to: { id }, types: [{ associationCategory: "HUBSPOT_DEFINED", associationTypeId: 192 }] });
      }
      for (const id of params.deal_ids ?? []) {
        associations.push({ to: { id }, types: [{ associationCategory: "HUBSPOT_DEFINED", associationTypeId: 216 }] });
      }
      const r = await hsPost(ctx, "/crm/v3/objects/tasks", {
        properties: props,
        associations: associations.length > 0 ? associations : undefined,
      });
      return { id: r.id, createdAt: r.createdAt };
    },
  },

  create_email_activity: {
    description: "Log an email activity and associate it with CRM objects.",
    params: z.object({
      subject: z.string().describe("Email subject"),
      body: z.string().describe("Email body (supports HTML)"),
      from_email: z.string().describe("Sender email"),
      to_email: z.string().describe("Recipient email"),
      contact_ids: z.array(z.string()).optional().describe("Contact IDs to associate"),
      deal_ids: z.array(z.string()).optional().describe("Deal IDs to associate"),
    }),
    returns: z.object({ id: z.string(), createdAt: z.string() }),
    execute: async (params, ctx) => {
      const props: Record<string, string> = {
        hs_email_subject: params.subject,
        hs_email_text: params.body,
        hs_email_from: params.from_email,
        hs_email_to: params.to_email,
        hs_timestamp: new Date().toISOString(),
        hs_email_direction: "EMAIL",
      };
      const associations: any[] = [];
      for (const id of params.contact_ids ?? []) {
        associations.push({ to: { id }, types: [{ associationCategory: "HUBSPOT_DEFINED", associationTypeId: 198 }] });
      }
      for (const id of params.deal_ids ?? []) {
        associations.push({ to: { id }, types: [{ associationCategory: "HUBSPOT_DEFINED", associationTypeId: 210 }] });
      }
      const r = await hsPost(ctx, "/crm/v3/objects/emails", {
        properties: props,
        associations: associations.length > 0 ? associations : undefined,
      });
      return { id: r.id, createdAt: r.createdAt };
    },
  },

  list_engagements: {
    description: "List engagement activities for a CRM object.",
    params: z.object({
      object_type: z.enum(["contacts", "companies", "deals", "tickets"]).describe("Object type"),
      object_id: z.string().describe("Object ID"),
      limit: z.number().min(1).max(100).default(10).describe("Results to return"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        type: z.string().describe("Engagement type (NOTE, TASK, EMAIL)"),
        body: z.string().nullable(),
        createdAt: z.string(),
        updatedAt: z.string(),
      }),
    ),
    execute: async (params, ctx) => {
      const results: any[] = [];
      const types = [
        { path: "notes", type: "NOTE", bodyProp: "hs_note_body" },
        { path: "tasks", type: "TASK", bodyProp: "hs_task_body" },
        { path: "emails", type: "EMAIL", bodyProp: "hs_email_text" },
      ];
      for (const t of types) {
        try {
          const data = await hsGet(
            ctx,
            `/crm/v3/objects/${params.object_type}/${params.object_id}/associations/${t.path}`,
          );
          for (const assoc of (data.results ?? []).slice(0, params.limit)) {
            try {
              const obj = await hsGet(ctx, `/crm/v3/objects/${t.path}/${assoc.id}`, {
                properties: t.bodyProp,
              });
              results.push({
                id: obj.id,
                type: t.type,
                body: obj.properties?.[t.bodyProp] ?? null,
                createdAt: obj.createdAt,
                updatedAt: obj.updatedAt,
              });
            } catch { /* skip individual failures */ }
          }
        } catch { /* type may not exist */ }
      }
      return results.slice(0, params.limit);
    },
  },
};
