import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { stripeGet, stripePost, flattenParams } from "../core/client.ts";

export const balance: Record<string, ActionDefinition> = {
  get_balance: {
    description: "Get the current account balance.",
    params: z.object({}),
    returns: z.object({
      available: z.array(z.object({ amount: z.number(), currency: z.string() })).describe("Available balance by currency"),
      pending: z.array(z.object({ amount: z.number(), currency: z.string() })).describe("Pending balance by currency"),
      connect_reserved: z.any().nullable().describe("Connect reserved balance"),
    }),
    execute: async (_params, ctx) => {
      const b = await stripeGet(ctx, "/balance");
      return {
        available: (b.available ?? []).map((a: any) => ({ amount: a.amount, currency: a.currency })),
        pending: (b.pending ?? []).map((a: any) => ({ amount: a.amount, currency: a.currency })),
        connect_reserved: b.connect_reserved ?? null,
      };
    },
  },

  list_balance_transactions: {
    description: "List balance transactions with optional type and date filters.",
    params: z.object({
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
      type: z.enum(["charge", "refund", "adjustment", "payout", "transfer"]).optional().describe("Transaction type filter"),
      created: z.record(z.number()).optional().describe("Date filter with Unix timestamps"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        amount: z.number(),
        currency: z.string(),
        type: z.string(),
        description: z.string().nullable(),
        fee: z.number(),
        net: z.number(),
        source: z.string().nullable(),
        created: z.number(),
        available_on: z.number(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { limit: String(params.limit) };
      if (params.type) q.type = params.type;
      if (params.created) Object.assign(q, flattenParams({ created: params.created }));
      const data = await stripeGet(ctx, "/balance_transactions", q);
      return data.data.map((t: any) => ({
        id: t.id,
        amount: t.amount,
        currency: t.currency,
        type: t.type,
        description: t.description ?? null,
        fee: t.fee,
        net: t.net,
        source: t.source ?? null,
        created: t.created,
        available_on: t.available_on,
      }));
    },
  },

  list_payouts: {
    description: "List payouts with optional status filter.",
    params: z.object({
      status: z.enum(["paid", "pending", "in_transit", "canceled", "failed"]).optional().describe("Payout status filter"),
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        amount: z.number(),
        currency: z.string(),
        status: z.string(),
        arrival_date: z.number(),
        method: z.string(),
        created: z.number(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { limit: String(params.limit) };
      if (params.status) q.status = params.status;
      const data = await stripeGet(ctx, "/payouts", q);
      return data.data.map((p: any) => ({
        id: p.id,
        amount: p.amount,
        currency: p.currency,
        status: p.status,
        arrival_date: p.arrival_date,
        method: p.method,
        created: p.created,
      }));
    },
  },

  get_payout: {
    description: "Get detailed information about a single payout.",
    params: z.object({
      payout_id: z.string().describe("Payout ID"),
    }),
    returns: z.object({
      id: z.string(),
      amount: z.number(),
      currency: z.string(),
      status: z.string(),
      arrival_date: z.number(),
      method: z.string(),
      description: z.string().nullable(),
      metadata: z.record(z.string()),
    }),
    execute: async (params, ctx) => {
      const p = await stripeGet(ctx, `/payouts/${params.payout_id}`);
      return {
        id: p.id,
        amount: p.amount,
        currency: p.currency,
        status: p.status,
        arrival_date: p.arrival_date,
        method: p.method,
        description: p.description ?? null,
        metadata: p.metadata ?? {},
      };
    },
  },

  create_payout: {
    description: "Create a new payout to your bank account.",
    params: z.object({
      amount: z.number().describe("Amount in smallest currency unit"),
      currency: z.string().describe("Three-letter ISO currency code"),
      description: z.string().optional().describe("Payout description"),
      method: z.enum(["standard", "instant"]).optional().describe("Payout method"),
      metadata: z.record(z.string()).optional().describe("Key-value pairs"),
    }),
    returns: z.object({
      id: z.string(),
      amount: z.number(),
      currency: z.string(),
      status: z.string(),
      arrival_date: z.number(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = {
        amount: String(params.amount),
        currency: params.currency,
      };
      if (params.description) body.description = params.description;
      if (params.method) body.method = params.method;
      if (params.metadata) Object.assign(body, flattenParams({ metadata: params.metadata }));
      const p = await stripePost(ctx, "/payouts", body);
      return {
        id: p.id,
        amount: p.amount,
        currency: p.currency,
        status: p.status,
        arrival_date: p.arrival_date,
      };
    },
  },
};
