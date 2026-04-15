import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ddGet, DEFAULT_SITE } from "../core/client.ts";

export const dashboards: Record<string, ActionDefinition> = {
  list_dashboards: {
    description: "List all dashboards in the organization.",
    params: z.object({
      filter_shared: z.boolean().optional().describe("Return only shared dashboards"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      title: z.string(),
      layout_type: z.string(),
      url: z.string().optional(),
      created_at: z.string().optional(),
      modified_at: z.string().optional(),
      author_handle: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const qp: Record<string, string> = {};
      if (params.filter_shared !== undefined) qp.filter_shared = String(params.filter_shared);
      const data = await ddGet(ctx.fetch, api_key, app_key, site, "/api/v1/dashboard", qp);
      return (data.dashboards ?? []).map((d: any) => ({
        id: d.id,
        title: d.title,
        layout_type: d.layout_type,
        url: d.url,
        created_at: d.created_at,
        modified_at: d.modified_at,
        author_handle: d.author_handle,
      }));
    },
  },

  get_dashboard: {
    description: "Get the full definition of a dashboard.",
    params: z.object({
      dashboard_id: z.string().describe("Dashboard ID"),
    }),
    returns: z.object({
      id: z.string(),
      title: z.string(),
      description: z.string().optional(),
      layout_type: z.string(),
      url: z.string().optional(),
      widgets: z.array(z.any()),
      template_variables: z.array(z.any()),
      created_at: z.string().optional(),
      modified_at: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key, app_key, site = DEFAULT_SITE } = ctx.credentials;
      const d = await ddGet(ctx.fetch, api_key, app_key, site, `/api/v1/dashboard/${params.dashboard_id}`);
      return {
        id: d.id,
        title: d.title,
        description: d.description,
        layout_type: d.layout_type,
        url: d.url,
        widgets: d.widgets ?? [],
        template_variables: d.template_variables ?? [],
        created_at: d.created_at,
        modified_at: d.modified_at,
      };
    },
  },
};
