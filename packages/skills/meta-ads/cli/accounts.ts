import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { metaGet } from "../core/client.ts";

export const accounts: Record<string, ActionDefinition> = {
  list_ad_accounts: {
    description: "List ad accounts accessible to the authenticated user.",
    params: z.object({
      per_page: z.number().min(1).max(100).default(25).describe("Results per page (1-100)"),
    }),
    returns: z.array(
      z.object({
        account_id: z.string().describe("Ad account ID"),
        name: z.string(),
        account_status: z.number().describe("Account status code"),
        currency: z.string(),
        timezone_name: z.string(),
        amount_spent: z.string().describe("Total amount spent"),
        balance: z.string().describe("Account balance"),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await metaGet(ctx, "/me/adaccounts", {
        fields: "account_id,name,account_status,currency,timezone_name,amount_spent,balance",
        limit: String(params.per_page),
      });
      return (data.data ?? []).map((a: any) => ({
        account_id: a.account_id,
        name: a.name,
        account_status: a.account_status,
        currency: a.currency,
        timezone_name: a.timezone_name,
        amount_spent: a.amount_spent ?? "0",
        balance: a.balance ?? "0",
      }));
    },
  },

  get_ad_account: {
    description: "Get detailed information about a single ad account.",
    params: z.object({
      account_id: z.string().describe("Ad account ID (act_*)"),
    }),
    returns: z.object({
      account_id: z.string(),
      name: z.string(),
      account_status: z.number(),
      currency: z.string(),
      timezone_name: z.string(),
      business_name: z.string().nullable(),
      spend_cap: z.string().nullable(),
      amount_spent: z.string(),
      balance: z.string(),
      age: z.number().nullable(),
    }),
    execute: async (params, ctx) => {
      const a = await metaGet(ctx, `/${params.account_id}`, {
        fields: "account_id,name,account_status,currency,timezone_name,business_name,spend_cap,amount_spent,balance,age",
      });
      return {
        account_id: a.account_id,
        name: a.name,
        account_status: a.account_status,
        currency: a.currency,
        timezone_name: a.timezone_name,
        business_name: a.business_name ?? null,
        spend_cap: a.spend_cap ?? null,
        amount_spent: a.amount_spent ?? "0",
        balance: a.balance ?? "0",
        age: a.age ?? null,
      };
    },
  },
};
