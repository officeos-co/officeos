import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { stripeGet, stripePost, flattenParams } from "../core/client.ts";

export const products: Record<string, ActionDefinition> = {
  list_products: {
    description: "List products with optional active filter.",
    params: z.object({
      active: z.boolean().optional().describe("Filter by active status"),
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        name: z.string(),
        description: z.string().nullable(),
        active: z.boolean(),
        default_price: z.string().nullable(),
        images: z.array(z.string()),
        metadata: z.record(z.string()),
        created: z.number(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { limit: String(params.limit) };
      if (params.active !== undefined) q.active = String(params.active);
      const data = await stripeGet(ctx, "/products", q);
      return data.data.map((p: any) => ({
        id: p.id,
        name: p.name,
        description: p.description ?? null,
        active: p.active,
        default_price: p.default_price ?? null,
        images: p.images ?? [],
        metadata: p.metadata ?? {},
        created: p.created,
      }));
    },
  },

  get_product: {
    description: "Get detailed information about a single product.",
    params: z.object({
      product_id: z.string().describe("Product ID"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      description: z.string().nullable(),
      active: z.boolean(),
      default_price: z.string().nullable(),
      images: z.array(z.string()),
      metadata: z.record(z.string()),
      created: z.number(),
      updated: z.number(),
    }),
    execute: async (params, ctx) => {
      const p = await stripeGet(ctx, `/products/${params.product_id}`);
      return {
        id: p.id,
        name: p.name,
        description: p.description ?? null,
        active: p.active,
        default_price: p.default_price ?? null,
        images: p.images ?? [],
        metadata: p.metadata ?? {},
        created: p.created,
        updated: p.updated,
      };
    },
  },

  create_product: {
    description: "Create a new product.",
    params: z.object({
      name: z.string().describe("Product name"),
      description: z.string().optional().describe("Product description"),
      images: z.array(z.string()).optional().describe("List of image URLs (max 8)"),
      metadata: z.record(z.string()).optional().describe("Key-value pairs"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      active: z.boolean(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = { name: params.name };
      if (params.description) body.description = params.description;
      if (params.images) Object.assign(body, flattenParams({ images: params.images }));
      if (params.metadata) Object.assign(body, flattenParams({ metadata: params.metadata }));
      const p = await stripePost(ctx, "/products", body);
      return { id: p.id, name: p.name, active: p.active };
    },
  },

  update_product: {
    description: "Update an existing product.",
    params: z.object({
      product_id: z.string().describe("Product ID"),
      name: z.string().optional().describe("Updated name"),
      description: z.string().optional().describe("Updated description"),
      metadata: z.record(z.string()).optional().describe("Updated key-value pairs"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      active: z.boolean(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = {};
      if (params.name) body.name = params.name;
      if (params.description) body.description = params.description;
      if (params.metadata) Object.assign(body, flattenParams({ metadata: params.metadata }));
      const p = await stripePost(ctx, `/products/${params.product_id}`, body);
      return { id: p.id, name: p.name, active: p.active };
    },
  },

  archive_product: {
    description: "Archive a product by setting active to false.",
    params: z.object({
      product_id: z.string().describe("Product ID"),
    }),
    returns: z.object({
      id: z.string(),
      active: z.boolean().describe("Will be false"),
    }),
    execute: async (params, ctx) => {
      const p = await stripePost(ctx, `/products/${params.product_id}`, { active: "false" });
      return { id: p.id, active: p.active };
    },
  },

  list_prices: {
    description: "List prices with optional product and active filters.",
    params: z.object({
      product: z.string().optional().describe("Filter by product ID"),
      active: z.boolean().optional().describe("Filter by active status"),
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        product: z.string(),
        unit_amount: z.number().nullable(),
        currency: z.string(),
        recurring: z.any().nullable().describe("Recurring pricing details"),
        active: z.boolean(),
        type: z.string(),
        created: z.number(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { limit: String(params.limit) };
      if (params.product) q.product = params.product;
      if (params.active !== undefined) q.active = String(params.active);
      const data = await stripeGet(ctx, "/prices", q);
      return data.data.map((p: any) => ({
        id: p.id,
        product: p.product,
        unit_amount: p.unit_amount ?? null,
        currency: p.currency,
        recurring: p.recurring ?? null,
        active: p.active,
        type: p.type,
        created: p.created,
      }));
    },
  },

  get_price: {
    description: "Get detailed information about a single price.",
    params: z.object({
      price_id: z.string().describe("Price ID"),
    }),
    returns: z.object({
      id: z.string(),
      product: z.string(),
      unit_amount: z.number().nullable(),
      currency: z.string(),
      recurring: z.any().nullable(),
      active: z.boolean(),
      type: z.string(),
      billing_scheme: z.string(),
      metadata: z.record(z.string()),
    }),
    execute: async (params, ctx) => {
      const p = await stripeGet(ctx, `/prices/${params.price_id}`);
      return {
        id: p.id,
        product: p.product,
        unit_amount: p.unit_amount ?? null,
        currency: p.currency,
        recurring: p.recurring ?? null,
        active: p.active,
        type: p.type,
        billing_scheme: p.billing_scheme,
        metadata: p.metadata ?? {},
      };
    },
  },

  create_price: {
    description: "Create a new price for a product.",
    params: z.object({
      product: z.string().describe("Product ID"),
      unit_amount: z.number().describe("Price in smallest currency unit"),
      currency: z.string().describe("Three-letter ISO currency code"),
      recurring_interval: z.enum(["day", "week", "month", "year"]).optional().describe("Recurring interval (omit for one-time)"),
      recurring_interval_count: z.number().optional().describe("Number of intervals between billings (default 1)"),
      metadata: z.record(z.string()).optional().describe("Key-value pairs"),
    }),
    returns: z.object({
      id: z.string(),
      product: z.string(),
      unit_amount: z.number().nullable(),
      currency: z.string(),
      recurring: z.any().nullable(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = {
        product: params.product,
        unit_amount: String(params.unit_amount),
        currency: params.currency,
      };
      if (params.recurring_interval) {
        body["recurring[interval]"] = params.recurring_interval;
        if (params.recurring_interval_count) {
          body["recurring[interval_count]"] = String(params.recurring_interval_count);
        }
      }
      if (params.metadata) Object.assign(body, flattenParams({ metadata: params.metadata }));
      const p = await stripePost(ctx, "/prices", body);
      return {
        id: p.id,
        product: p.product,
        unit_amount: p.unit_amount ?? null,
        currency: p.currency,
        recurring: p.recurring ?? null,
      };
    },
  },
};
