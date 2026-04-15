import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sfFetch, sfPost, enc } from "../core/client.ts";

export const contacts: Record<string, ActionDefinition> = {
  list_contacts: {
    description: "List contacts with optional filters.",
    params: z.object({
      account_id: z.string().optional().describe("Filter by account"),
      limit: z.number().default(100).describe("Max records to return"),
    }),
    returns: z.array(
      z.object({
        Id: z.string().describe("Contact ID"),
        Name: z.string().describe("Full name"),
        Email: z.string().nullable().describe("Email"),
        Phone: z.string().nullable().describe("Phone"),
        AccountId: z.string().nullable().describe("Account ID"),
        Title: z.string().nullable().describe("Job title"),
        CreatedDate: z.string().describe("Creation timestamp"),
      }),
    ),
    execute: async (params, ctx) => {
      const where = params.account_id ? ` WHERE AccountId = '${params.account_id}'` : "";
      const soql = `SELECT Id, Name, Email, Phone, AccountId, Title, CreatedDate FROM Contact${where} LIMIT ${params.limit}`;
      const data = await sfFetch(ctx, `/query?q=${enc(soql)}`);
      return data.records;
    },
  },

  create_contact: {
    description: "Create a new contact.",
    params: z.object({
      first_name: z.string().optional().describe("First name"),
      last_name: z.string().describe("Last name"),
      email: z.string().optional().describe("Email address"),
      phone: z.string().optional().describe("Phone number"),
      account_id: z.string().optional().describe("Associated account ID"),
      title: z.string().optional().describe("Job title"),
      fields: z.record(z.unknown()).optional().describe("Additional fields"),
    }),
    returns: z.object({
      id: z.string().describe("New contact ID"),
      success: z.boolean().describe("Whether creation succeeded"),
    }),
    execute: async (params, ctx) => {
      const body: any = { LastName: params.last_name, ...params.fields };
      if (params.first_name) body.FirstName = params.first_name;
      if (params.email) body.Email = params.email;
      if (params.phone) body.Phone = params.phone;
      if (params.account_id) body.AccountId = params.account_id;
      if (params.title) body.Title = params.title;
      const data = await sfPost(ctx, `/sobjects/Contact`, body);
      return { id: data.id, success: data.success ?? true };
    },
  },

  update_contact: {
    description: "Update an existing contact.",
    params: z.object({
      contact_id: z.string().describe("Contact ID"),
      fields: z.record(z.unknown()).describe("Fields to update"),
    }),
    returns: z.object({
      id: z.string().describe("Contact ID"),
      success: z.boolean().describe("Whether update succeeded"),
    }),
    execute: async (params, ctx) => {
      await sfFetch(ctx, `/sobjects/Contact/${enc(params.contact_id)}`, {
        method: "PATCH",
        body: JSON.stringify(params.fields),
      });
      return { id: params.contact_id, success: true };
    },
  },
};
