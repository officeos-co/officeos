import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { stripeGet, stripePost, flattenParams } from "../core/client.ts";

export const refunds: Record<string, ActionDefinition> = {
  list_refunds: {
    description: "List refunds with optional charge or payment intent filters.",
    params: z.object({
      charge: z.string().optional().describe("Filter by charge ID"),
      payment_intent: z.string().optional().describe("Filter by payment intent"),
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        amount: z.number(),
        currency: z.string(),
        status: z.string(),
        charge: z.string(),
        payment_intent: z.string().nullable(),
        reason: z.string().nullable(),
        created: z.number(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { limit: String(params.limit) };
      if (params.charge) q.charge = params.charge;
      if (params.payment_intent) q.payment_intent = params.payment_intent;
      const data = await stripeGet(ctx, "/refunds", q);
      return data.data.map((r: any) => ({
        id: r.id,
        amount: r.amount,
        currency: r.currency,
        status: r.status,
        charge: r.charge,
        payment_intent: r.payment_intent ?? null,
        reason: r.reason ?? null,
        created: r.created,
      }));
    },
  },

  create_refund: {
    description: "Create a refund for a charge or payment intent.",
    params: z.object({
      charge: z.string().optional().describe("Charge ID (required if no payment_intent)"),
      payment_intent: z.string().optional().describe("Payment intent ID (required if no charge)"),
      amount: z.number().optional().describe("Partial refund amount in cents (omit for full refund)"),
      reason: z.enum(["duplicate", "fraudulent", "requested_by_customer"]).optional().describe("Reason for refund"),
      metadata: z.record(z.string()).optional().describe("Key-value pairs"),
    }),
    returns: z.object({
      id: z.string(),
      amount: z.number(),
      currency: z.string(),
      status: z.string(),
      charge: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = {};
      if (params.charge) body.charge = params.charge;
      if (params.payment_intent) body.payment_intent = params.payment_intent;
      if (params.amount !== undefined) body.amount = String(params.amount);
      if (params.reason) body.reason = params.reason;
      if (params.metadata) Object.assign(body, flattenParams({ metadata: params.metadata }));
      const r = await stripePost(ctx, "/refunds", body);
      return { id: r.id, amount: r.amount, currency: r.currency, status: r.status, charge: r.charge };
    },
  },
};
