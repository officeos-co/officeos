import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { stripeGet, stripePost, stripeDelete, flattenParams } from "../core/client.ts";

export const subscriptions: Record<string, ActionDefinition> = {
  list_subscriptions: {
    description: "List subscriptions with optional customer and status filters.",
    params: z.object({
      customer: z.string().optional().describe("Filter by customer ID"),
      status: z.enum(["active", "past_due", "canceled", "unpaid", "trialing", "all"]).optional().describe("Subscription status filter"),
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        customer: z.string(),
        status: z.string(),
        current_period_start: z.number(),
        current_period_end: z.number(),
        items: z.any().describe("Subscription items"),
        cancel_at_period_end: z.boolean(),
        created: z.number(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { limit: String(params.limit) };
      if (params.customer) q.customer = params.customer;
      if (params.status) q.status = params.status;
      const data = await stripeGet(ctx, "/subscriptions", q);
      return data.data.map((s: any) => ({
        id: s.id,
        customer: s.customer,
        status: s.status,
        current_period_start: s.current_period_start,
        current_period_end: s.current_period_end,
        items: s.items?.data ?? [],
        cancel_at_period_end: s.cancel_at_period_end,
        created: s.created,
      }));
    },
  },

  get_subscription: {
    description: "Get detailed information about a single subscription.",
    params: z.object({
      subscription_id: z.string().describe("Subscription ID"),
    }),
    returns: z.object({
      id: z.string(),
      customer: z.string(),
      status: z.string(),
      items: z.any().describe("Subscription items"),
      current_period_start: z.number(),
      current_period_end: z.number(),
      cancel_at_period_end: z.boolean(),
      trial_start: z.number().nullable(),
      trial_end: z.number().nullable(),
      default_payment_method: z.string().nullable(),
      latest_invoice: z.string().nullable(),
      metadata: z.record(z.string()),
    }),
    execute: async (params, ctx) => {
      const s = await stripeGet(ctx, `/subscriptions/${params.subscription_id}`);
      return {
        id: s.id,
        customer: s.customer,
        status: s.status,
        items: s.items?.data ?? [],
        current_period_start: s.current_period_start,
        current_period_end: s.current_period_end,
        cancel_at_period_end: s.cancel_at_period_end,
        trial_start: s.trial_start ?? null,
        trial_end: s.trial_end ?? null,
        default_payment_method: s.default_payment_method ?? null,
        latest_invoice: s.latest_invoice ?? null,
        metadata: s.metadata ?? {},
      };
    },
  },

  create_subscription: {
    description: "Create a new subscription for a customer.",
    params: z.object({
      customer: z.string().describe("Customer ID"),
      price_ids: z.array(z.string()).describe("List of price IDs for subscription items"),
      trial_period_days: z.number().optional().describe("Number of trial days"),
      default_payment_method: z.string().optional().describe("Payment method ID"),
      metadata: z.record(z.string()).optional().describe("Key-value pairs"),
    }),
    returns: z.object({
      id: z.string(),
      customer: z.string(),
      status: z.string(),
      current_period_start: z.number(),
      current_period_end: z.number(),
      items: z.any().describe("Subscription items"),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = { customer: params.customer };
      params.price_ids.forEach((pid, i) => {
        body[`items[${i}][price]`] = pid;
      });
      if (params.trial_period_days !== undefined) body.trial_period_days = String(params.trial_period_days);
      if (params.default_payment_method) body.default_payment_method = params.default_payment_method;
      if (params.metadata) Object.assign(body, flattenParams({ metadata: params.metadata }));
      const s = await stripePost(ctx, "/subscriptions", body);
      return {
        id: s.id,
        customer: s.customer,
        status: s.status,
        current_period_start: s.current_period_start,
        current_period_end: s.current_period_end,
        items: s.items?.data ?? [],
      };
    },
  },

  update_subscription: {
    description: "Update an existing subscription (e.g. change price, proration).",
    params: z.object({
      subscription_id: z.string().describe("Subscription ID"),
      price_ids: z.array(z.string()).optional().describe("Updated price IDs (replaces existing items)"),
      proration_behavior: z.enum(["create_prorations", "none", "always_invoice"]).optional().describe("Proration behavior"),
      metadata: z.record(z.string()).optional().describe("Updated key-value pairs"),
    }),
    returns: z.object({
      id: z.string(),
      status: z.string(),
      items: z.any().describe("Subscription items"),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = {};
      if (params.price_ids) {
        params.price_ids.forEach((pid, i) => {
          body[`items[${i}][price]`] = pid;
        });
      }
      if (params.proration_behavior) body.proration_behavior = params.proration_behavior;
      if (params.metadata) Object.assign(body, flattenParams({ metadata: params.metadata }));
      const s = await stripePost(ctx, `/subscriptions/${params.subscription_id}`, body);
      return { id: s.id, status: s.status, items: s.items?.data ?? [] };
    },
  },

  cancel_subscription: {
    description: "Cancel a subscription immediately or at period end.",
    params: z.object({
      subscription_id: z.string().describe("Subscription ID"),
      at_period_end: z.boolean().default(false).describe("Cancel at end of current period"),
    }),
    returns: z.object({
      id: z.string(),
      status: z.string(),
      cancel_at_period_end: z.boolean(),
      canceled_at: z.number().nullable(),
    }),
    execute: async (params, ctx) => {
      if (params.at_period_end) {
        const s = await stripePost(ctx, `/subscriptions/${params.subscription_id}`, {
          cancel_at_period_end: "true",
        });
        return {
          id: s.id,
          status: s.status,
          cancel_at_period_end: s.cancel_at_period_end,
          canceled_at: s.canceled_at ?? null,
        };
      }
      const s = await stripeDelete(ctx, `/subscriptions/${params.subscription_id}`);
      return {
        id: s.id,
        status: s.status,
        cancel_at_period_end: s.cancel_at_period_end ?? false,
        canceled_at: s.canceled_at ?? null,
      };
    },
  },

  pause_subscription: {
    description: "Pause collection on a subscription.",
    params: z.object({
      subscription_id: z.string().describe("Subscription ID"),
      behavior: z.enum(["mark_uncollectible", "keep_as_draft", "void"]).default("mark_uncollectible").describe("Pause behavior"),
    }),
    returns: z.object({
      id: z.string(),
      status: z.string(),
      pause_collection: z.any().describe("Pause collection settings"),
    }),
    execute: async (params, ctx) => {
      const s = await stripePost(ctx, `/subscriptions/${params.subscription_id}`, {
        "pause_collection[behavior]": params.behavior,
      });
      return { id: s.id, status: s.status, pause_collection: s.pause_collection };
    },
  },
};
