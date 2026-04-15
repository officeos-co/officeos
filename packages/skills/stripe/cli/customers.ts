import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { stripeGet, stripePost, stripeDelete, flattenParams } from "../core/client.ts";

export const customers: Record<string, ActionDefinition> = {
  list_customers: {
    description: "List customers with optional email and date filters.",
    params: z.object({
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
      email: z.string().optional().describe("Filter by exact email"),
      created: z.record(z.number()).optional().describe("Date filter with gt, gte, lt, lte as Unix timestamps"),
    }),
    returns: z.array(
      z.object({
        id: z.string().describe("Customer ID"),
        email: z.string().nullable().describe("Customer email"),
        name: z.string().nullable().describe("Customer name"),
        phone: z.string().nullable().describe("Phone number"),
        created: z.number().describe("Created timestamp"),
        metadata: z.record(z.string()).describe("Key-value metadata"),
        default_source: z.string().nullable().describe("Default payment source"),
        currency: z.string().nullable().describe("Default currency"),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { limit: String(params.limit) };
      if (params.email) q.email = params.email;
      if (params.created) Object.assign(q, flattenParams({ created: params.created }));
      const data = await stripeGet(ctx, "/customers", q);
      return data.data.map((c: any) => ({
        id: c.id,
        email: c.email ?? null,
        name: c.name ?? null,
        phone: c.phone ?? null,
        created: c.created,
        metadata: c.metadata ?? {},
        default_source: c.default_source ?? null,
        currency: c.currency ?? null,
      }));
    },
  },

  get_customer: {
    description: "Get detailed information about a single customer.",
    params: z.object({
      customer_id: z.string().describe("Customer ID"),
    }),
    returns: z.object({
      id: z.string(),
      email: z.string().nullable(),
      name: z.string().nullable(),
      phone: z.string().nullable(),
      description: z.string().nullable(),
      created: z.number(),
      metadata: z.record(z.string()),
      subscriptions: z.any().describe("Active subscriptions"),
      default_source: z.string().nullable(),
      balance: z.number().describe("Account balance in cents"),
      currency: z.string().nullable(),
      delinquent: z.boolean(),
    }),
    execute: async (params, ctx) => {
      const c = await stripeGet(ctx, `/customers/${params.customer_id}`);
      return {
        id: c.id,
        email: c.email ?? null,
        name: c.name ?? null,
        phone: c.phone ?? null,
        description: c.description ?? null,
        created: c.created,
        metadata: c.metadata ?? {},
        subscriptions: c.subscriptions?.data ?? [],
        default_source: c.default_source ?? null,
        balance: c.balance ?? 0,
        currency: c.currency ?? null,
        delinquent: c.delinquent ?? false,
      };
    },
  },

  create_customer: {
    description: "Create a new customer.",
    params: z.object({
      email: z.string().optional().describe("Customer email"),
      name: z.string().optional().describe("Customer full name"),
      phone: z.string().optional().describe("Customer phone number"),
      description: z.string().optional().describe("Internal description"),
      metadata: z.record(z.string()).optional().describe("Key-value pairs (max 50 keys)"),
    }),
    returns: z.object({
      id: z.string(),
      email: z.string().nullable(),
      name: z.string().nullable(),
      created: z.number(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = {};
      if (params.email) body.email = params.email;
      if (params.name) body.name = params.name;
      if (params.phone) body.phone = params.phone;
      if (params.description) body.description = params.description;
      if (params.metadata) Object.assign(body, flattenParams({ metadata: params.metadata }));
      const c = await stripePost(ctx, "/customers", body);
      return { id: c.id, email: c.email ?? null, name: c.name ?? null, created: c.created };
    },
  },

  update_customer: {
    description: "Update an existing customer.",
    params: z.object({
      customer_id: z.string().describe("Customer ID"),
      email: z.string().optional().describe("Updated email"),
      name: z.string().optional().describe("Updated name"),
      metadata: z.record(z.string()).optional().describe("Updated key-value pairs"),
    }),
    returns: z.object({
      id: z.string(),
      email: z.string().nullable(),
      name: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = {};
      if (params.email) body.email = params.email;
      if (params.name) body.name = params.name;
      if (params.metadata) Object.assign(body, flattenParams({ metadata: params.metadata }));
      const c = await stripePost(ctx, `/customers/${params.customer_id}`, body);
      return { id: c.id, email: c.email ?? null, name: c.name ?? null };
    },
  },

  delete_customer: {
    description: "Permanently delete a customer and cancel all their subscriptions.",
    params: z.object({
      customer_id: z.string().describe("Customer ID"),
    }),
    returns: z.object({
      id: z.string(),
      deleted: z.boolean(),
    }),
    execute: async (params, ctx) => {
      const r = await stripeDelete(ctx, `/customers/${params.customer_id}`);
      return { id: r.id, deleted: r.deleted };
    },
  },

  search_customers: {
    description: "Search customers using Stripe search query syntax.",
    params: z.object({
      query: z.string().describe("Stripe search query syntax"),
      limit: z.number().min(1).max(100).optional().describe("Results to return (1-100)"),
    }),
    returns: z.array(z.any()).describe("List of matching customer objects"),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { query: params.query };
      if (params.limit) q.limit = String(params.limit);
      const data = await stripeGet(ctx, "/customers/search", q);
      return data.data;
    },
  },
};
