import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sfFetch, sfPost, enc } from "../core/client.ts";

export const leads: Record<string, ActionDefinition> = {
  list_leads: {
    description: "List leads with optional filters.",
    params: z.object({
      status: z.string().optional().describe("Filter: Open, Working, Closed, Converted"),
      owner: z.string().optional().describe("Filter by owner user ID"),
      limit: z.number().default(100).describe("Max records to return"),
    }),
    returns: z.array(
      z.object({
        Id: z.string().describe("Lead ID"),
        Name: z.string().describe("Full name"),
        Email: z.string().nullable().describe("Email address"),
        Company: z.string().describe("Company name"),
        Status: z.string().describe("Lead status"),
        Phone: z.string().nullable().describe("Phone number"),
        LeadSource: z.string().nullable().describe("Lead source"),
        CreatedDate: z.string().describe("Creation timestamp"),
      }),
    ),
    execute: async (params, ctx) => {
      const clauses: string[] = [];
      if (params.status) clauses.push(`Status = '${params.status}'`);
      if (params.owner) clauses.push(`OwnerId = '${params.owner}'`);
      const where = clauses.length ? ` WHERE ${clauses.join(" AND ")}` : "";
      const soql = `SELECT Id, Name, Email, Company, Status, Phone, LeadSource, CreatedDate FROM Lead${where} LIMIT ${params.limit}`;
      const data = await sfFetch(ctx, `/query?q=${enc(soql)}`);
      return data.records;
    },
  },

  create_lead: {
    description: "Create a new lead.",
    params: z.object({
      first_name: z.string().optional().describe("First name"),
      last_name: z.string().describe("Last name"),
      company: z.string().describe("Company name"),
      email: z.string().optional().describe("Email address"),
      phone: z.string().optional().describe("Phone number"),
      lead_source: z.string().optional().describe("Lead source (e.g. Web, Referral)"),
      status: z.string().optional().describe("Lead status"),
      fields: z.record(z.unknown()).optional().describe("Additional field-value pairs"),
    }),
    returns: z.object({
      id: z.string().describe("New lead ID"),
      success: z.boolean().describe("Whether creation succeeded"),
    }),
    execute: async (params, ctx) => {
      const body: any = {
        LastName: params.last_name,
        Company: params.company,
        ...params.fields,
      };
      if (params.first_name) body.FirstName = params.first_name;
      if (params.email) body.Email = params.email;
      if (params.phone) body.Phone = params.phone;
      if (params.lead_source) body.LeadSource = params.lead_source;
      if (params.status) body.Status = params.status;
      const data = await sfPost(ctx, `/sobjects/Lead`, body);
      return { id: data.id, success: data.success ?? true };
    },
  },

  convert_lead: {
    description: "Convert a lead into a contact, account, and optionally an opportunity.",
    params: z.object({
      lead_id: z.string().describe("Lead ID to convert"),
      account_id: z.string().optional().describe("Existing account to merge into"),
      contact_id: z.string().optional().describe("Existing contact to merge into"),
      opportunity_name: z.string().optional().describe("Name for the new opportunity"),
      converted_status: z.string().describe("Status value for converted leads"),
      do_not_create_opportunity: z.boolean().optional().describe("Skip opportunity creation"),
    }),
    returns: z.object({
      accountId: z.string().describe("Account ID"),
      contactId: z.string().describe("Contact ID"),
      opportunityId: z.string().nullable().describe("Opportunity ID if created"),
      success: z.boolean().describe("Whether conversion succeeded"),
    }),
    execute: async (params, ctx) => {
      const leadConvert: any = {
        leadId: params.lead_id,
        convertedStatus: params.converted_status,
      };
      if (params.account_id) leadConvert.accountId = params.account_id;
      if (params.contact_id) leadConvert.contactId = params.contact_id;
      if (params.opportunity_name) leadConvert.opportunityName = params.opportunity_name;
      if (params.do_not_create_opportunity) leadConvert.doNotCreateOpportunity = true;
      const res = await ctx.fetch(
        `${ctx.credentials.instance_url}/services/data/v59.0/actions/standard/convertLead`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${ctx.credentials.access_token}`,
            "Content-Type": "application/json",
            Accept: "application/json",
          },
          body: JSON.stringify({ inputs: [leadConvert] }),
        },
      );
      if (!res.ok) {
        const body = await res.text();
        throw new Error(`Salesforce API ${res.status}: ${body}`);
      }
      const results = await res.json();
      const r = results[0]?.outputValues ?? results[0] ?? {};
      return {
        accountId: r.accountId ?? "",
        contactId: r.contactId ?? "",
        opportunityId: r.opportunityId ?? null,
        success: true,
      };
    },
  },
};
