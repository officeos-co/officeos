import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { hsGet } from "../core/client.ts";

export const pipelines: Record<string, ActionDefinition> = {
  list_pipelines: {
    description: "List pipelines for deals or tickets.",
    params: z.object({
      object_type: z.enum(["deals", "tickets"]).default("deals").describe("Object type"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        label: z.string(),
        displayOrder: z.number(),
        stages: z.array(z.any()).describe("Pipeline stages"),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await hsGet(ctx, `/crm/v3/pipelines/${params.object_type}`);
      return (data.results ?? []).map((p: any) => ({
        id: p.id,
        label: p.label,
        displayOrder: p.displayOrder,
        stages: p.stages ?? [],
      }));
    },
  },

  get_pipeline: {
    description: "Get a single pipeline with its stages.",
    params: z.object({
      pipeline_id: z.string().describe("Pipeline ID"),
      object_type: z.enum(["deals", "tickets"]).default("deals").describe("Object type"),
    }),
    returns: z.object({
      id: z.string(),
      label: z.string(),
      displayOrder: z.number(),
      stages: z.array(
        z.object({
          id: z.string(),
          label: z.string(),
          displayOrder: z.number(),
        }),
      ),
    }),
    execute: async (params, ctx) => {
      const p = await hsGet(ctx, `/crm/v3/pipelines/${params.object_type}/${params.pipeline_id}`);
      return {
        id: p.id,
        label: p.label,
        displayOrder: p.displayOrder,
        stages: (p.stages ?? []).map((s: any) => ({
          id: s.id,
          label: s.label,
          displayOrder: s.displayOrder,
        })),
      };
    },
  },

  list_pipeline_stages: {
    description: "List stages for a specific pipeline.",
    params: z.object({
      pipeline_id: z.string().describe("Pipeline ID"),
      object_type: z.enum(["deals", "tickets"]).default("deals").describe("Object type"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        label: z.string(),
        displayOrder: z.number(),
        metadata: z.any().nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await hsGet(ctx, `/crm/v3/pipelines/${params.object_type}/${params.pipeline_id}/stages`);
      return (data.results ?? []).map((s: any) => ({
        id: s.id,
        label: s.label,
        displayOrder: s.displayOrder,
        metadata: s.metadata ?? null,
      }));
    },
  },
};
