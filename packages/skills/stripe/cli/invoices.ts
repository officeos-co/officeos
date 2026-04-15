import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { stripeGet, stripePost, flattenParams } from "../core/client.ts";

export const invoices: Record<string, ActionDefinition> = {
  list_invoices: {
    description: "List invoices with optional customer and status filters.",
    params: z.object({
      customer: z.string().optional().describe("Filter by customer ID"),
      status: z.enum(["draft", "open", "paid", "void", "uncollectible"]).optional().describe("Invoice status filter"),
      limit: z.number().min(1).max(100).default(10).describe("Results to return (1-100)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        customer: z.string(),
        status: z.string(),
        amount_due: z.number(),
        amount_paid: z.number(),
        currency: z.string(),
        due_date: z.number().nullable(),
        created: z.number(),
        hosted_invoice_url: z.string().nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const q: Record<string, string> = { limit: String(params.limit) };
      if (params.customer) q.customer = params.customer;
      if (params.status) q.status = params.status;
      const data = await stripeGet(ctx, "/invoices", q);
      return data.data.map((i: any) => ({
        id: i.id,
        customer: i.customer,
        status: i.status,
        amount_due: i.amount_due,
        amount_paid: i.amount_paid,
        currency: i.currency,
        due_date: i.due_date ?? null,
        created: i.created,
        hosted_invoice_url: i.hosted_invoice_url ?? null,
      }));
    },
  },

  get_invoice: {
    description: "Get detailed information about a single invoice.",
    params: z.object({
      invoice_id: z.string().describe("Invoice ID"),
    }),
    returns: z.object({
      id: z.string(),
      customer: z.string(),
      status: z.string(),
      amount_due: z.number(),
      amount_paid: z.number(),
      amount_remaining: z.number(),
      currency: z.string(),
      lines: z.any().describe("Invoice line items"),
      due_date: z.number().nullable(),
      hosted_invoice_url: z.string().nullable(),
      invoice_pdf: z.string().nullable(),
      metadata: z.record(z.string()),
    }),
    execute: async (params, ctx) => {
      const i = await stripeGet(ctx, `/invoices/${params.invoice_id}`);
      return {
        id: i.id,
        customer: i.customer,
        status: i.status,
        amount_due: i.amount_due,
        amount_paid: i.amount_paid,
        amount_remaining: i.amount_remaining,
        currency: i.currency,
        lines: i.lines?.data ?? [],
        due_date: i.due_date ?? null,
        hosted_invoice_url: i.hosted_invoice_url ?? null,
        invoice_pdf: i.invoice_pdf ?? null,
        metadata: i.metadata ?? {},
      };
    },
  },

  create_invoice: {
    description: "Create a new invoice for a customer.",
    params: z.object({
      customer: z.string().describe("Customer ID"),
      collection_method: z.enum(["charge_automatically", "send_invoice"]).default("charge_automatically").describe("Collection method"),
      description: z.string().optional().describe("Invoice description"),
      due_date: z.number().optional().describe("Unix timestamp (required if send_invoice)"),
      metadata: z.record(z.string()).optional().describe("Key-value pairs"),
    }),
    returns: z.object({
      id: z.string(),
      customer: z.string(),
      status: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, string> = {
        customer: params.customer,
        collection_method: params.collection_method,
      };
      if (params.description) body.description = params.description;
      if (params.due_date !== undefined) body.due_date = String(params.due_date);
      if (params.metadata) Object.assign(body, flattenParams({ metadata: params.metadata }));
      const i = await stripePost(ctx, "/invoices", body);
      return { id: i.id, customer: i.customer, status: i.status };
    },
  },

  send_invoice: {
    description: "Send an invoice to the customer.",
    params: z.object({
      invoice_id: z.string().describe("Invoice ID"),
    }),
    returns: z.object({
      id: z.string(),
      status: z.string(),
      hosted_invoice_url: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const i = await stripePost(ctx, `/invoices/${params.invoice_id}/send`);
      return { id: i.id, status: i.status, hosted_invoice_url: i.hosted_invoice_url ?? null };
    },
  },

  void_invoice: {
    description: "Void an invoice.",
    params: z.object({
      invoice_id: z.string().describe("Invoice ID"),
    }),
    returns: z.object({
      id: z.string(),
      status: z.string().describe("Will be void"),
    }),
    execute: async (params, ctx) => {
      const i = await stripePost(ctx, `/invoices/${params.invoice_id}/void`);
      return { id: i.id, status: i.status };
    },
  },

  finalize_invoice: {
    description: "Finalize a draft invoice for payment.",
    params: z.object({
      invoice_id: z.string().describe("Invoice ID"),
    }),
    returns: z.object({
      id: z.string(),
      status: z.string(),
      hosted_invoice_url: z.string().nullable(),
      invoice_pdf: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const i = await stripePost(ctx, `/invoices/${params.invoice_id}/finalize`);
      return {
        id: i.id,
        status: i.status,
        hosted_invoice_url: i.hosted_invoice_url ?? null,
        invoice_pdf: i.invoice_pdf ?? null,
      };
    },
  },
};
