import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { nrRestGet } from "../core/client.ts";

export const applications: Record<string, ActionDefinition> = {
  list_applications: {
    description: "List APM applications in the account.",
    params: z.object({
      filter_name: z.string().optional().describe("Filter by application name"),
      filter_language: z.string().optional().describe("Filter by agent language (ruby, java, python, php, dotnet, nodejs, go)"),
      limit: z.number().int().max(200).default(25).describe("Max results"),
    }),
    returns: z.array(z.object({
      id: z.number(),
      name: z.string(),
      language: z.string().optional(),
      health_status: z.string().optional(),
      reporting: z.boolean().optional(),
      last_reported_at: z.string().optional(),
      application_summary: z.record(z.any()).optional(),
    })),
    execute: async (params, ctx) => {
      const qp: Record<string, string> = {};
      if (params.filter_name) qp["filter[name]"] = params.filter_name;
      if (params.filter_language) qp["filter[language]"] = params.filter_language;
      const data = await nrRestGet(ctx.fetch, ctx.credentials.api_key, "/applications.json", qp);
      return (data.applications ?? []).slice(0, params.limit).map((a: any) => ({
        id: a.id,
        name: a.name,
        language: a.language,
        health_status: a.health_status,
        reporting: a.reporting,
        last_reported_at: a.last_reported_at,
        application_summary: a.application_summary,
      }));
    },
  },

  get_application: {
    description: "Get details of a specific APM application.",
    params: z.object({
      app_id: z.number().int().describe("Application ID"),
    }),
    returns: z.object({
      id: z.number(),
      name: z.string(),
      language: z.string().optional(),
      health_status: z.string().optional(),
      reporting: z.boolean().optional(),
      settings: z.record(z.any()).optional(),
      links: z.record(z.any()).optional(),
      application_summary: z.record(z.any()).optional(),
    }),
    execute: async (params, ctx) => {
      const data = await nrRestGet(ctx.fetch, ctx.credentials.api_key, `/applications/${params.app_id}.json`);
      const a = data.application;
      return {
        id: a.id,
        name: a.name,
        language: a.language,
        health_status: a.health_status,
        reporting: a.reporting,
        settings: a.settings,
        links: a.links,
        application_summary: a.application_summary,
      };
    },
  },
};
