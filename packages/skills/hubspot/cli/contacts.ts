import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { hsGet, hsPost, hsDelete } from "../core/client.ts";

export const contacts: Record<string, ActionDefinition> = {
  list_contacts: {
    description: "List CRM contacts with optional property selection.",
    params: z.object({
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
      after: z.string().optional().describe("Pagination cursor"),
      properties: z.array(z.string()).optional().describe("Properties to include"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        properties: z.record(z.string().nullable()).describe("Contact properties"),
        createdAt: z.string(),
        updatedAt: z.string(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { limit: String(params.limit) };
      if (params.after) q.after = params.after;
      if (params.properties) q.properties = params.properties.join(",");
      const data = await hsGet(ctx, "/crm/v3/objects/contacts", q);
      return (data.results ?? []).map((c: any) => ({
        id: c.id,
        properties: c.properties ?? {},
        createdAt: c.createdAt,
        updatedAt: c.updatedAt,
      }));
    },
  },

  get_contact: {
    description: "Get a single CRM contact with properties and associations.",
    params: z.object({
      contact_id: z.string().describe("Contact ID"),
      properties: z.array(z.string()).optional().describe("Properties to include"),
    }),
    returns: z.object({
      id: z.string(),
      properties: z.record(z.string().nullable()),
      createdAt: z.string(),
      updatedAt: z.string(),
      associations: z.any().nullable().describe("Associated objects"),
    }),
    execute: async (params, ctx) => {
      const q: Record<string, string> = {};
      if (params.properties) q.properties = params.properties.join(",");
      const c = await hsGet(ctx, `/crm/v3/objects/contacts/${params.contact_id}`, q);
      return {
        id: c.id,
        properties: c.properties ?? {},
        createdAt: c.createdAt,
        updatedAt: c.updatedAt,
        associations: c.associations ?? null,
      };
    },
  },

  create_contact: {
    description: "Create a new CRM contact.",
    params: z.object({
      email: z.string().describe("Contact email"),
      firstname: z.string().optional().describe("First name"),
      lastname: z.string().optional().describe("Last name"),
      phone: z.string().optional().describe("Phone number"),
      company: z.string().optional().describe("Company name"),
      properties: z.record(z.string()).optional().describe("Additional properties as key-value"),
    }),
    returns: z.object({
      id: z.string(),
      properties: z.record(z.string().nullable()),
      createdAt: z.string(),
    }),
    execute: async (params, ctx) => {
      const props: Record<string, string> = { email: params.email };
      if (params.firstname) props.firstname = params.firstname;
      if (params.lastname) props.lastname = params.lastname;
      if (params.phone) props.phone = params.phone;
      if (params.company) props.company = params.company;
      if (params.properties) Object.assign(props, params.properties);
      const c = await hsPost(ctx, "/crm/v3/objects/contacts", { properties: props });
      return { id: c.id, properties: c.properties ?? {}, createdAt: c.createdAt };
    },
  },

  update_contact: {
    description: "Update an existing CRM contact.",
    params: z.object({
      contact_id: z.string().describe("Contact ID"),
      email: z.string().optional().describe("Updated email"),
      firstname: z.string().optional().describe("Updated first name"),
      lastname: z.string().optional().describe("Updated last name"),
      properties: z.record(z.string()).optional().describe("Additional properties to update"),
    }),
    returns: z.object({
      id: z.string(),
      properties: z.record(z.string().nullable()),
      updatedAt: z.string(),
    }),
    execute: async (params, ctx) => {
      const props: Record<string, string> = {};
      if (params.email) props.email = params.email;
      if (params.firstname) props.firstname = params.firstname;
      if (params.lastname) props.lastname = params.lastname;
      if (params.properties) Object.assign(props, params.properties);
      const c = await hsPost(ctx, `/crm/v3/objects/contacts/${params.contact_id}`, { properties: props }, "PATCH");
      return { id: c.id, properties: c.properties ?? {}, updatedAt: c.updatedAt };
    },
  },

  delete_contact: {
    description: "Delete a CRM contact (moved to recycling bin for 90 days).",
    params: z.object({
      contact_id: z.string().describe("Contact ID"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      await hsDelete(ctx, `/crm/v3/objects/contacts/${params.contact_id}`);
      return { success: true };
    },
  },

  search_contacts: {
    description: "Search contacts with free-text query or filter groups.",
    params: z.object({
      query: z.string().optional().describe("Free-text search query"),
      filter_groups: z.array(z.any()).optional().describe("HubSpot filter groups JSON"),
      sorts: z.array(z.any()).optional().describe("Sort criteria JSON"),
      properties: z.array(z.string()).optional().describe("Properties to return"),
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
    }),
    returns: z.array(z.any()).describe("List of matching contact objects"),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = { limit: params.limit };
      if (params.query) body.query = params.query;
      if (params.filter_groups) body.filterGroups = params.filter_groups;
      if (params.sorts) body.sorts = params.sorts;
      if (params.properties) body.properties = params.properties;
      const data = await hsPost(ctx, "/crm/v3/objects/contacts/search", body);
      return data.results ?? [];
    },
  },
};
