import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gfFetch, gfPost, gfDelete, qs, enc } from "../core/client.ts";

export const dashboards: Record<string, ActionDefinition> = {
  list_dashboards: {
    description: "Search/list dashboards.",
    params: z.object({
      query: z.string().optional().describe("Search query string"),
      tag: z.string().optional().describe("Filter by tag"),
      folder_id: z.number().optional().describe("Filter by folder ID"),
    }),
    returns: z.array(
      z.object({ uid: z.string(), title: z.string(), url: z.string(), type: z.string(), tags: z.array(z.string()) }),
    ),
    execute: async (params, ctx) => {
      const data = await gfFetch(
        ctx,
        `/search${qs({ query: params.query, tag: params.tag, folderIds: params.folder_id, type: "dash-db" })}`,
      );
      return data.map((d: any) => ({ uid: d.uid, title: d.title, url: d.url, type: d.type, tags: d.tags ?? [] }));
    },
  },

  get_dashboard: {
    description: "Get a dashboard by UID.",
    params: z.object({ uid: z.string().describe("Dashboard UID") }),
    returns: z.object({
      uid: z.string(),
      title: z.string(),
      url: z.string(),
      tags: z.array(z.string()),
      panels: z.array(z.any()),
      version: z.number(),
      folder_id: z.number(),
      folder_title: z.string(),
    }),
    execute: async (params, ctx) => {
      const data = await gfFetch(ctx, `/dashboards/uid/${enc(params.uid)}`);
      const dash = data.dashboard;
      const meta = data.meta;
      return {
        uid: dash.uid,
        title: dash.title,
        url: meta.url,
        tags: dash.tags ?? [],
        panels: dash.panels ?? [],
        version: dash.version,
        folder_id: meta.folderId ?? 0,
        folder_title: meta.folderTitle ?? "",
      };
    },
  },

  create_dashboard: {
    description: "Create a new dashboard.",
    params: z.object({
      title: z.string().describe("Dashboard title"),
      folder_id: z.number().default(0).describe("Target folder ID (0 = General)"),
      panels: z.any().default([]).describe("Array of panel definitions"),
      tags: z.array(z.string()).default([]).describe("Tags"),
      overwrite: z.boolean().default(false).describe("Overwrite existing"),
    }),
    returns: z.object({ uid: z.string(), url: z.string(), status: z.string(), version: z.number() }),
    execute: async (params, ctx) => {
      const data = await gfPost(ctx, "/dashboards/db", {
        dashboard: { id: null, uid: null, title: params.title, tags: params.tags, panels: params.panels, schemaVersion: 36, version: 0 },
        folderId: params.folder_id,
        overwrite: params.overwrite,
      });
      return { uid: data.uid, url: data.url, status: data.status, version: data.version };
    },
  },

  update_dashboard: {
    description: "Update an existing dashboard.",
    params: z.object({
      uid: z.string().describe("Dashboard UID"),
      version: z.number().describe("Current version for conflict detection"),
      title: z.string().optional().describe("New title"),
      panels: z.any().optional().describe("Updated panel definitions"),
      tags: z.array(z.string()).optional().describe("Updated tags"),
      folder_id: z.number().optional().describe("Move to folder"),
      overwrite: z.boolean().default(false).describe("Overwrite without version check"),
    }),
    returns: z.object({ uid: z.string(), url: z.string(), status: z.string(), version: z.number() }),
    execute: async (params, ctx) => {
      const current = await gfFetch(ctx, `/dashboards/uid/${enc(params.uid)}`);
      const dash = current.dashboard;
      if (params.title !== undefined) dash.title = params.title;
      if (params.panels !== undefined) dash.panels = params.panels;
      if (params.tags !== undefined) dash.tags = params.tags;
      dash.version = params.version;
      const data = await gfPost(ctx, "/dashboards/db", {
        dashboard: dash,
        folderId: params.folder_id ?? current.meta.folderId ?? 0,
        overwrite: params.overwrite,
      });
      return { uid: data.uid, url: data.url, status: data.status, version: data.version };
    },
  },

  delete_dashboard: {
    description: "Delete a dashboard by UID.",
    params: z.object({ uid: z.string().describe("Dashboard UID") }),
    returns: z.object({ title: z.string(), message: z.string() }),
    execute: async (params, ctx) => {
      const data = await gfDelete(ctx, `/dashboards/uid/${enc(params.uid)}`);
      return { title: data.title ?? "", message: data.message ?? "Dashboard deleted" };
    },
  },

  get_panel_data: {
    description: "Get rendered data for a specific panel.",
    params: z.object({
      dashboard_uid: z.string().describe("Dashboard UID"),
      panel_id: z.number().describe("Panel ID"),
      from: z.string().default("now-1h").describe("Start time"),
      to: z.string().default("now").describe("End time"),
    }),
    returns: z.object({ results: z.any() }),
    execute: async (params, ctx) => {
      const dashData = await gfFetch(ctx, `/dashboards/uid/${enc(params.dashboard_uid)}`);
      const panel = dashData.dashboard.panels?.find((p: any) => p.id === params.panel_id);
      if (!panel) throw new Error(`Panel ${params.panel_id} not found in dashboard ${params.dashboard_uid}`);
      const queries = (panel.targets ?? []).map((t: any, i: number) => ({
        refId: t.refId ?? String.fromCharCode(65 + i),
        datasource: t.datasource ?? panel.datasource,
        ...t,
      }));
      const data = await gfPost(ctx, "/ds/query", { queries, from: params.from, to: params.to });
      return { results: data.results };
    },
  },

  search: {
    description: "Search dashboards and folders.",
    params: z.object({
      query: z.string().optional().describe("Search query"),
      type: z.enum(["dash-db", "dash-folder"]).optional().describe("Filter by type"),
      tag: z.string().optional().describe("Filter by tag"),
    }),
    returns: z.array(
      z.object({ uid: z.string(), title: z.string(), url: z.string(), type: z.string(), tags: z.array(z.string()) }),
    ),
    execute: async (params, ctx) => {
      const data = await gfFetch(ctx, `/search${qs({ query: params.query, type: params.type, tag: params.tag })}`);
      return data.map((d: any) => ({ uid: d.uid ?? "", title: d.title, url: d.url, type: d.type, tags: d.tags ?? [] }));
    },
  },
};
