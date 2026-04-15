import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { stripeGet, stripePost, flattenParams } from "../core/client.ts";

export const payments: Record<string, ActionDefinition> = {
  list_payment_intents: {
    description: "List payment intents with optional customer and date filters.",
    params: z.object({
      customer: z.string().optional().describe("Filter by customer ID"),
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
      created: z.record(z.number()).optional().describe("Date filter with Unix timestamps"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        amount: z.number().describe("Amount in smallest currency unit"),
        currency: z.string(),
        status: z.string(),
        customer: z.string().nullable(),
        payment_method: z.string().nullable(),
        created: z.number(),
        description: z.string().nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { limit: String(params.limit) };
      if (params.customer) q.customer = params.customer;
      if (params.created) Object.assign(q, flattenParams({ created: params.created }));
      const data = await stripeGet(ctx, "/payment_intents", q);
      return data.data.map((p: any) => ({
        id: p.id,
        amount: p.amount,
        currency: p.currency,
        status: p.status,
        customer: p.customer ?? null,
        payment_method: p.payment_method ?? null,
        created: p.created,
        description: p.description ?? null,
      }));
    },
  },

  get_payment_intent: {
    description: "Get detailed information about a single payment intent.",
    params: z.object({
      payment_intent_id: z.string().describe("Payment intent ID"),
    }),
    returns: z.object({
      id: z.string(),
      amount: z.number(),
      currency: z.string(),
      status: z.string(),
      customer: z.string().nullable(),
      payment_method: z.string().nullable(),
      payment_method_types: z.array(z.string()),
      description: z.string().nullable(),
      metadata: z.record(z.string()),
      charges: z.any().describe("Associated charges"),
      created: z.number(),
      canceled_at: z.number().nullable(),
      cancellation_reason: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const p = await stripeGet(ctx, `/payment_intents/${params.payment_intent_id}`);
      return {
        id: p.id,
        amount: p.amount,
        currency: p.currency,
        status: p.status,
        customer: p.customer ?? null,
        payment_method: p.payment_method ?? null,
        payment_method_types: p.payment_method_types ?? [],
        description: p.description ?? null,
        metadata: p.metadata ?? {},
        charges: p.charges?.data ?? [],
        created: p.created,
        canceled_at: p.canceled_at ?? null,
        cancellation_reason: p.cancellation_reason ?? null,
      };
    },
  },

  create_payment_intent: {
    description: "Create a new payment intent.",
    params: z.object({
      amount: z.number().describe("Amount in smallest currency unit (cents for USD)"),
      currency: z.string().describe("Three-letter ISO currency code"),
      customer: z.string().optional().describe("Customer ID to attach to"),
      payment_method_types: z.array(z.string()).default(["card"]).describe("Accepted payment methods"),
      description: z.string().optional().describe("Internal description"),
      metadata: z.record(z.string()).optional().describe("Key-value pairs"),
    }),
    returns: z.object({
      id: z.string(),
      client_secret: z.string(),
      amount: z.number(),
      currency: z.string(),
      status: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = {
        amount: String(params.amount),
        currency: params.currency,
      };
      if (params.customer) body.customer = params.customer;
      if (params.description) body.description = params.description;
      if (params.payment_method_types) {
        Object.assign(body, flattenParams({ payment_method_types: params.payment_method_types }));
      }
      if (params.metadata) Object.assign(body, flattenParams({ metadata: params.metadata }));
      const p = await stripePost(ctx, "/payment_intents", body);
      return {
        id: p.id,
        client_secret: p.client_secret,
        amount: p.amount,
        currency: p.currency,
        status: p.status,
      };
    },
  },

  confirm_payment: {
    description: "Confirm a payment intent to initiate the charge.",
    params: z.object({
      payment_intent_id: z.string().describe("Payment intent ID"),
      payment_method: z.string().optional().describe("Payment method ID"),
    }),
    returns: z.object({
      id: z.string(),
      status: z.string(),
      amount: z.number(),
      currency: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = {};
      if (params.payment_method) body.payment_method = params.payment_method;
      const p = await stripePost(ctx, `/payment_intents/${params.payment_intent_id}/confirm`, body);
      return { id: p.id, status: p.status, amount: p.amount, currency: p.currency };
    },
  },

  cancel_payment: {
    description: "Cancel a payment intent.",
    params: z.object({
      payment_intent_id: z.string().describe("Payment intent ID"),
      cancellation_reason: z.enum(["duplicate", "fraudulent", "requested_by_customer", "abandoned"]).optional().describe("Reason for cancellation"),
    }),
    returns: z.object({
      id: z.string(),
      status: z.string(),
      cancellation_reason: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = {};
      if (params.cancellation_reason) body.cancellation_reason = params.cancellation_reason;
      const p = await stripePost(ctx, `/payment_intents/${params.payment_intent_id}/cancel`, body);
      return { id: p.id, status: p.status, cancellation_reason: p.cancellation_reason ?? null };
    },
  },

  list_charges: {
    description: "List charges with optional customer and date filters.",
    params: z.object({
      customer: z.string().optional().describe("Filter by customer ID"),
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
      created: z.record(z.number()).optional().describe("Date filter with Unix timestamps"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        amount: z.number(),
        currency: z.string(),
        status: z.string(),
        customer: z.string().nullable(),
        description: z.string().nullable(),
        payment_method: z.string().nullable(),
        created: z.number(),
        refunded: z.boolean(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { limit: String(params.limit) };
      if (params.customer) q.customer = params.customer;
      if (params.created) Object.assign(q, flattenParams({ created: params.created }));
      const data = await stripeGet(ctx, "/charges", q);
      return data.data.map((c: any) => ({
        id: c.id,
        amount: c.amount,
        currency: c.currency,
        status: c.status,
        customer: c.customer ?? null,
        description: c.description ?? null,
        payment_method: c.payment_method ?? null,
        created: c.created,
        refunded: c.refunded ?? false,
      }));
    },
  },

  get_charge: {
    description: "Get detailed information about a single charge.",
    params: z.object({
      charge_id: z.string().describe("Charge ID"),
    }),
    returns: z.object({
      id: z.string(),
      amount: z.number(),
      currency: z.string(),
      status: z.string(),
      customer: z.string().nullable(),
      description: z.string().nullable(),
      payment_method_details: z.any().describe("Payment method details"),
      receipt_url: z.string().nullable(),
      refunded: z.boolean(),
      refunds: z.any().describe("Refund data"),
      metadata: z.record(z.string()),
    }),
    execute: async (params, ctx) => {
      const c = await stripeGet(ctx, `/charges/${params.charge_id}`);
      return {
        id: c.id,
        amount: c.amount,
        currency: c.currency,
        status: c.status,
        customer: c.customer ?? null,
        description: c.description ?? null,
        payment_method_details: c.payment_method_details ?? null,
        receipt_url: c.receipt_url ?? null,
        refunded: c.refunded ?? false,
        refunds: c.refunds?.data ?? [],
        metadata: c.metadata ?? {},
      };
    },
  },

  create_charge: {
    description: "Create a new charge.",
    params: z.object({
      amount: z.number().describe("Amount in smallest currency unit"),
      currency: z.string().describe("Three-letter ISO currency code"),
      customer: z.string().optional().describe("Customer ID (required if no source)"),
      source: z.string().optional().describe("Payment source token (required if no customer)"),
      description: z.string().optional().describe("Charge description"),
      metadata: z.record(z.string()).optional().describe("Key-value pairs"),
    }),
    returns: z.object({
      id: z.string(),
      amount: z.number(),
      currency: z.string(),
      status: z.string(),
      receipt_url: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = {
        amount: String(params.amount),
        currency: params.currency,
      };
      if (params.customer) body.customer = params.customer;
      if (params.source) body.source = params.source;
      if (params.description) body.description = params.description;
      if (params.metadata) Object.assign(body, flattenParams({ metadata: params.metadata }));
      const c = await stripePost(ctx, "/charges", body);
      return {
        id: c.id,
        amount: c.amount,
        currency: c.currency,
        status: c.status,
        receipt_url: c.receipt_url ?? null,
      };
    },
  },
};
