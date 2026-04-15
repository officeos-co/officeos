import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sfFetch, sfPost, enc } from "../core/client.ts";

export const accounts: Record<string, ActionDefinition> = {
  list_accounts: {
    description: "List accounts with optional filters.",
    params: z.object({
      type: z.string().optional().describe("Filter by account type"),
      owner: z.string().optional().describe("Filter by owner user ID"),
      limit: z.number().default(100).describe("Max records to return"),
    }),
    returns: z.array(
      z.object({
        Id: z.string().describe("Account ID"),
        Name: z.string().describe("Account name"),
        Type: z.string().nullable().describe("Account type"),
        Industry: z.string().nullable().describe("Industry"),
        AnnualRevenue: z.number().nullable().describe("Annual revenue"),
        NumberOfEmployees: z.number().nullable().describe("Number of employees"),
        Website: z.string().nullable().describe("Website URL"),
        CreatedDate: z.string().describe("Creation timestamp"),
      }),
    ),
    execute: async (params, ctx) => {
      const clauses: string[] = [];
      if (params.type) clauses.push(`Type = '${params.type}'`);
      if (params.owner) clauses.push(`OwnerId = '${params.owner}'`);
      const where = clauses.length ? ` WHERE ${clauses.join(" AND ")}` : "";
      const soql = `SELECT Id, Name, Type, Industry, AnnualRevenue, NumberOfEmployees, Website, CreatedDate FROM Account${where} LIMIT ${params.limit}`;
      const data = await sfFetch(ctx, `/query?q=${enc(soql)}`);
      return data.records;
    },
  },

  create_account: {
    description: "Create a new account.",
    params: z.object({
      name: z.string().describe("Account name"),
      type: z.string().optional().describe("Account type"),
      industry: z.string().optional().describe("Industry"),
      website: z.string().optional().describe("Website URL"),
      phone: z.string().optional().describe("Phone number"),
      fields: z.record(z.unknown()).optional().describe("Additional fields"),
    }),
    returns: z.object({
      id: z.string().describe("New account ID"),
      success: z.boolean().describe("Whether creation succeeded"),
    }),
    execute: async (params, ctx) => {
      const body: any = { Name: params.name, ...params.fields };
      if (params.type) body.Type = params.type;
      if (params.industry) body.Industry = params.industry;
      if (params.website) body.Website = params.website;
      if (params.phone) body.Phone = params.phone;
      const data = await sfPost(ctx, `/sobjects/Account`, body);
      return { id: data.id, success: data.success ?? true };
    },
  },

  update_account: {
    description: "Update an existing account.",
    params: z.object({
      account_id: z.string().describe("Account ID"),
      fields: z.record(z.unknown()).describe("Fields to update"),
    }),
    returns: z.object({
      id: z.string().describe("Account ID"),
      success: z.boolean().describe("Whether update succeeded"),
    }),
    execute: async (params, ctx) => {
      await sfFetch(ctx, `/sobjects/Account/${enc(params.account_id)}`, {
        method: "PATCH",
        body: JSON.stringify(params.fields),
      });
      return { id: params.account_id, success: true };
    },
  },
};
