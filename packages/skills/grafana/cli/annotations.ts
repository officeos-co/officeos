import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gfFetch, gfPost, gfDelete, qs } from "../core/client.ts";

export const annotations: Record<string, ActionDefinition> = {
  list_annotations: {
    description: "List annotations.",
    params: z.object({
      from: z.number().optional().describe("Epoch ms start"),
      to: z.number().optional().describe("Epoch ms end"),
      dashboard_id: z.number().optional().describe("Filter by dashboard ID"),
      panel_id: z.number().optional().describe("Filter by panel ID"),
      tags: z.array(z.string()).optional().describe("Filter by tags"),
    }),
    returns: z.array(
      z.object({ id: z.number(), dashboard_id: z.number(), text: z.string(), tags: z.array(z.string()), time: z.number() }),
    ),
    execute: async (params, ctx) => {
      const q = qs({ from: params.from, to: params.to, dashboardId: params.dashboard_id, panelId: params.panel_id, tags: params.tags });
      const data = await gfFetch(ctx, `/annotations${q}`);
      return (Array.isArray(data) ? data : []).map((a: any) => ({
        id: a.id, dashboard_id: a.dashboardId ?? 0, text: a.text ?? "", tags: a.tags ?? [], time: a.time,
      }));
    },
  },

  create_annotation: {
    description: "Create an annotation.",
    params: z.object({
      text: z.string().describe("Annotation text"),
      tags: z.array(z.string()).optional().describe("Tags"),
      dashboard_id: z.number().optional().describe("Associate with dashboard"),
      panel_id: z.number().optional().describe("Associate with panel"),
      time: z.number().optional().describe("Epoch ms (default: now)"),
      time_end: z.number().optional().describe("Epoch ms end (for region annotations)"),
    }),
    returns: z.object({ id: z.number(), message: z.string() }),
    execute: async (params, ctx) => {
      const body: any = { text: params.text };
      if (params.tags) body.tags = params.tags;
      if (params.dashboard_id !== undefined) body.dashboardId = params.dashboard_id;
      if (params.panel_id !== undefined) body.panelId = params.panel_id;
      if (params.time !== undefined) body.time = params.time;
      if (params.time_end !== undefined) body.timeEnd = params.time_end;
      const data = await gfPost(ctx, "/annotations", body);
      return { id: data.id, message: data.message ?? "Annotation created" };
    },
  },

  delete_annotation: {
    description: "Delete an annotation by ID.",
    params: z.object({ id: z.number().describe("Annotation ID") }),
    returns: z.object({ message: z.string() }),
    execute: async (params, ctx) => {
      const data = await gfDelete(ctx, `/annotations/${params.id}`);
      return { message: data.message ?? "Annotation deleted" };
    },
  },
};
