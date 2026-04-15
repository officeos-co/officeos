import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sfFetch, sfPost, enc } from "../core/client.ts";

export const opportunities: Record<string, ActionDefinition> = {
  list_opportunities: {
    description: "List opportunities with optional filters.",
    params: z.object({
      stage: z.string().optional().describe("Filter by stage name"),
      account_id: z.string().optional().describe("Filter by account"),
      close_date_range: z.object({
        start: z.string().describe("Start date (YYYY-MM-DD)"),
        end: z.string().describe("End date (YYYY-MM-DD)"),
      }).optional().describe("Filter by close date range"),
      limit: z.number().default(100).describe("Max records to return"),
    }),
    returns: z.array(
      z.object({
        Id: z.string().describe("Opportunity ID"),
        Name: z.string().describe("Opportunity name"),
        StageName: z.string().describe("Stage"),
        Amount: z.number().nullable().describe("Deal amount"),
        CloseDate: z.string().describe("Close date"),
        AccountId: z.string().nullable().describe("Account ID"),
        Probability: z.number().nullable().describe("Win probability"),
        CreatedDate: z.string().describe("Creation timestamp"),
      }),
    ),
    execute: async (params, ctx) => {
      const clauses: string[] = [];
      if (params.stage) clauses.push(`StageName = '${params.stage}'`);
      if (params.account_id) clauses.push(`AccountId = '${params.account_id}'`);
      if (params.close_date_range) {
        clauses.push(`CloseDate >= ${params.close_date_range.start}`);
        clauses.push(`CloseDate <= ${params.close_date_range.end}`);
      }
      const where = clauses.length ? ` WHERE ${clauses.join(" AND ")}` : "";
      const soql = `SELECT Id, Name, StageName, Amount, CloseDate, AccountId, Probability, CreatedDate FROM Opportunity${where} LIMIT ${params.limit}`;
      const data = await sfFetch(ctx, `/query?q=${enc(soql)}`);
      return data.records;
    },
  },

  create_opportunity: {
    description: "Create a new opportunity.",
    params: z.object({
      name: z.string().describe("Opportunity name"),
      stage: z.string().describe("Stage name"),
      close_date: z.string().describe("Close date (YYYY-MM-DD)"),
      amount: z.number().optional().describe("Deal amount"),
      account_id: z.string().optional().describe("Associated account ID"),
      fields: z.record(z.unknown()).optional().describe("Additional fields"),
    }),
    returns: z.object({
      id: z.string().describe("New opportunity ID"),
      success: z.boolean().describe("Whether creation succeeded"),
    }),
    execute: async (params, ctx) => {
      const body: any = {
        Name: params.name,
        StageName: params.stage,
        CloseDate: params.close_date,
        ...params.fields,
      };
      if (params.amount !== undefined) body.Amount = params.amount;
      if (params.account_id) body.AccountId = params.account_id;
      const data = await sfPost(ctx, `/sobjects/Opportunity`, body);
      return { id: data.id, success: data.success ?? true };
    },
  },

  update_opportunity: {
    description: "Update an existing opportunity.",
    params: z.object({
      opportunity_id: z.string().describe("Opportunity ID"),
      fields: z.record(z.unknown()).describe("Fields to update"),
    }),
    returns: z.object({
      id: z.string().describe("Opportunity ID"),
      success: z.boolean().describe("Whether update succeeded"),
    }),
    execute: async (params, ctx) => {
      await sfFetch(ctx, `/sobjects/Opportunity/${enc(params.opportunity_id)}`, {
        method: "PATCH",
        body: JSON.stringify(params.fields),
      });
      return { id: params.opportunity_id, success: true };
    },
  },
};
