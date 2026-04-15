import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sfFetch, sfPost, enc } from "../core/client.ts";

export const cases: Record<string, ActionDefinition> = {
  list_cases: {
    description: "List cases with optional filters.",
    params: z.object({
      status: z.string().optional().describe("Filter: New, Working, Escalated, Closed"),
      priority: z.string().optional().describe("Filter: Low, Medium, High, Critical"),
      account_id: z.string().optional().describe("Filter by account"),
      limit: z.number().default(100).describe("Max records to return"),
    }),
    returns: z.array(
      z.object({
        Id: z.string().describe("Case ID"),
        CaseNumber: z.string().describe("Case number"),
        Subject: z.string().nullable().describe("Subject"),
        Status: z.string().describe("Status"),
        Priority: z.string().nullable().describe("Priority"),
        AccountId: z.string().nullable().describe("Account ID"),
        ContactId: z.string().nullable().describe("Contact ID"),
        CreatedDate: z.string().describe("Creation timestamp"),
      }),
    ),
    execute: async (params, ctx) => {
      const clauses: string[] = [];
      if (params.status) clauses.push(`Status = '${params.status}'`);
      if (params.priority) clauses.push(`Priority = '${params.priority}'`);
      if (params.account_id) clauses.push(`AccountId = '${params.account_id}'`);
      const where = clauses.length ? ` WHERE ${clauses.join(" AND ")}` : "";
      const soql = `SELECT Id, CaseNumber, Subject, Status, Priority, AccountId, ContactId, CreatedDate FROM Case${where} LIMIT ${params.limit}`;
      const data = await sfFetch(ctx, `/query?q=${enc(soql)}`);
      return data.records;
    },
  },

  create_case: {
    description: "Create a new case.",
    params: z.object({
      subject: z.string().describe("Case subject"),
      description: z.string().optional().describe("Detailed description"),
      priority: z.string().optional().describe("Low, Medium, High, Critical"),
      status: z.string().optional().describe("Case status"),
      account_id: z.string().optional().describe("Associated account ID"),
      contact_id: z.string().optional().describe("Associated contact ID"),
      fields: z.record(z.unknown()).optional().describe("Additional fields"),
    }),
    returns: z.object({
      id: z.string().describe("New case ID"),
      success: z.boolean().describe("Whether creation succeeded"),
    }),
    execute: async (params, ctx) => {
      const body: any = { Subject: params.subject, ...params.fields };
      if (params.description) body.Description = params.description;
      if (params.priority) body.Priority = params.priority;
      if (params.status) body.Status = params.status;
      if (params.account_id) body.AccountId = params.account_id;
      if (params.contact_id) body.ContactId = params.contact_id;
      const data = await sfPost(ctx, `/sobjects/Case`, body);
      return { id: data.id, success: data.success ?? true };
    },
  },

  update_case: {
    description: "Update an existing case.",
    params: z.object({
      case_id: z.string().describe("Case ID"),
      fields: z.record(z.unknown()).describe("Fields to update"),
    }),
    returns: z.object({
      id: z.string().describe("Case ID"),
      success: z.boolean().describe("Whether update succeeded"),
    }),
    execute: async (params, ctx) => {
      await sfFetch(ctx, `/sobjects/Case/${enc(params.case_id)}`, {
        method: "PATCH",
        body: JSON.stringify(params.fields),
      });
      return { id: params.case_id, success: true };
    },
  },

  close_case: {
    description: "Close a case with an optional resolution note.",
    params: z.object({
      case_id: z.string().describe("Case ID"),
      resolution: z.string().optional().describe("Resolution description"),
    }),
    returns: z.object({
      id: z.string().describe("Case ID"),
      success: z.boolean().describe("Whether close succeeded"),
      status: z.string().describe("Final status (Closed)"),
    }),
    execute: async (params, ctx) => {
      const body: any = { Status: "Closed" };
      if (params.resolution) body.Description = params.resolution;
      await sfFetch(ctx, `/sobjects/Case/${enc(params.case_id)}`, {
        method: "PATCH",
        body: JSON.stringify(body),
      });
      return { id: params.case_id, success: true, status: "Closed" };
    },
  },
};
