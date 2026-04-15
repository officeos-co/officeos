import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { doGet, doPost, doDelete, qs } from "../core/client.ts";

export const apps: Record<string, ActionDefinition> = {
  list_apps: {
    description: "List App Platform apps.",
    params: z.object({
      per_page: z.number().min(1).max(200).default(20).describe("Results per page (1-200)"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("App ID"),
      default_ingress: z.string().nullable().describe("Default ingress URL"),
      live_url: z.string().nullable().describe("Live URL"),
      active_deployment: z.any().nullable().describe("Active deployment"),
      spec: z.any().describe("App spec"),
      created_at: z.string().describe("Creation timestamp"),
      updated_at: z.string().describe("Last updated timestamp"),
    })),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/apps${qs({ per_page: params.per_page })}`);
      return (data.apps ?? []).map((a: any) => ({
        id: a.id,
        default_ingress: a.default_ingress ?? null,
        live_url: a.live_url ?? null,
        active_deployment: a.active_deployment ?? null,
        spec: a.spec,
        created_at: a.created_at,
        updated_at: a.updated_at,
      }));
    },
  },

  get_app: {
    description: "Get detailed info about an App Platform app.",
    params: z.object({
      app_id: z.string().describe("App ID"),
    }),
    returns: z.object({
      id: z.string().describe("App ID"),
      default_ingress: z.string().nullable().describe("Default ingress URL"),
      live_url: z.string().nullable().describe("Live URL"),
      active_deployment: z.any().nullable().describe("Active deployment"),
      spec: z.any().describe("App spec"),
      created_at: z.string().describe("Creation timestamp"),
      updated_at: z.string().describe("Last updated timestamp"),
      last_deployment_active_at: z.string().nullable().describe("Last deployment active time"),
    }),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/apps/${params.app_id}`);
      const a = data.app;
      return {
        id: a.id,
        default_ingress: a.default_ingress ?? null,
        live_url: a.live_url ?? null,
        active_deployment: a.active_deployment ?? null,
        spec: a.spec,
        created_at: a.created_at,
        updated_at: a.updated_at,
        last_deployment_active_at: a.last_deployment_active_at ?? null,
      };
    },
  },

  create_app: {
    description: "Create an App Platform app.",
    params: z.object({
      spec: z.string().describe("JSON app spec (App Platform format)"),
    }),
    returns: z.object({
      id: z.string().describe("App ID"),
      default_ingress: z.string().nullable().describe("Default ingress URL"),
      live_url: z.string().nullable().describe("Live URL"),
      created_at: z.string().describe("Creation timestamp"),
    }),
    execute: async (params, ctx) => {
      const data = await doPost(ctx, "/apps", { spec: JSON.parse(params.spec) });
      const a = data.app;
      return {
        id: a.id,
        default_ingress: a.default_ingress ?? null,
        live_url: a.live_url ?? null,
        created_at: a.created_at,
      };
    },
  },

  delete_app: {
    description: "Delete an App Platform app.",
    params: z.object({
      app_id: z.string().describe("App ID"),
    }),
    returns: z.object({
      deleted: z.boolean().describe("Whether deletion succeeded"),
    }),
    execute: async (params, ctx) => {
      await doDelete(ctx, `/apps/${params.app_id}`);
      return { deleted: true };
    },
  },

  list_deployments: {
    description: "List deployments for an App Platform app.",
    params: z.object({
      app_id: z.string().describe("App ID"),
      per_page: z.number().min(1).max(200).default(20).describe("Results per page (1-200)"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Deployment ID"),
      cause: z.string().describe("Deployment cause"),
      phase: z.string().describe("Deployment phase"),
      created_at: z.string().describe("Creation timestamp"),
      updated_at: z.string().describe("Last updated timestamp"),
      progress: z.any().nullable().describe("Deployment progress"),
    })),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/apps/${params.app_id}/deployments${qs({ per_page: params.per_page })}`);
      return (data.deployments ?? []).map((d: any) => ({
        id: d.id,
        cause: d.cause ?? "",
        phase: d.phase ?? "",
        created_at: d.created_at,
        updated_at: d.updated_at,
        progress: d.progress ?? null,
      }));
    },
  },
};
