import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gfFetch, gfPost, gfDelete, enc } from "../core/client.ts";

export const alerts: Record<string, ActionDefinition> = {
  list_alert_rules: {
    description: "List all alert rules.",
    params: z.object({}),
    returns: z.array(
      z.object({ uid: z.string(), title: z.string(), condition: z.string(), folder_uid: z.string(), labels: z.any() }),
    ),
    execute: async (_params, ctx) => {
      const data = await gfFetch(ctx, "/v1/provisioning/alert-rules");
      return (Array.isArray(data) ? data : []).map((r: any) => ({
        uid: r.uid, title: r.title, condition: r.condition ?? "", folder_uid: r.folderUID ?? "", labels: r.labels ?? {},
      }));
    },
  },

  get_alert_rule: {
    description: "Get an alert rule by UID.",
    params: z.object({ uid: z.string().describe("Alert rule UID") }),
    returns: z.object({
      uid: z.string(), title: z.string(), condition: z.string(), data: z.any(),
      no_data_state: z.string(), exec_err_state: z.string(), folder_uid: z.string(),
      for: z.string(), labels: z.any(), annotations: z.any(),
    }),
    execute: async (params, ctx) => {
      const r = await gfFetch(ctx, `/v1/provisioning/alert-rules/${enc(params.uid)}`);
      return {
        uid: r.uid, title: r.title, condition: r.condition ?? "", data: r.data ?? [],
        no_data_state: r.noDataState ?? "NoData", exec_err_state: r.execErrState ?? "Error",
        folder_uid: r.folderUID ?? "", for: r.for ?? "0s", labels: r.labels ?? {}, annotations: r.annotations ?? {},
      };
    },
  },

  create_alert_rule: {
    description: "Create a new alert rule.",
    params: z.object({
      title: z.string().describe("Alert rule title"),
      folder_uid: z.string().describe("Folder UID"),
      condition: z.string().describe("Condition ref ID (e.g. C)"),
      data: z.any().describe("Array of query/condition objects"),
      for: z.string().default("5m").describe("Pending duration"),
      no_data_state: z.enum(["NoData", "Alerting", "OK"]).default("NoData"),
      exec_err_state: z.enum(["Error", "Alerting", "OK"]).default("Error"),
      labels: z.any().optional().describe("Additional labels"),
      annotations: z.any().optional().describe("Additional annotations"),
    }),
    returns: z.object({ uid: z.string(), title: z.string() }),
    execute: async (params, ctx) => {
      const body: any = {
        title: params.title, folderUID: params.folder_uid, condition: params.condition,
        data: params.data, for: params.for, noDataState: params.no_data_state, execErrState: params.exec_err_state,
      };
      if (params.labels) body.labels = params.labels;
      if (params.annotations) body.annotations = params.annotations;
      const data = await gfPost(ctx, "/v1/provisioning/alert-rules", body);
      return { uid: data.uid, title: data.title };
    },
  },

  delete_alert_rule: {
    description: "Delete an alert rule by UID.",
    params: z.object({ uid: z.string().describe("Alert rule UID") }),
    returns: z.object({ message: z.string() }),
    execute: async (params, ctx) => {
      await gfDelete(ctx, `/v1/provisioning/alert-rules/${enc(params.uid)}`);
      return { message: "Alert rule deleted" };
    },
  },

  list_alert_instances: {
    description: "List all firing and pending alert instances.",
    params: z.object({}),
    returns: z.array(z.any()),
    execute: async (_params, ctx) => {
      const data = await gfFetch(ctx, "/alertmanager/grafana/api/v2/alerts");
      return Array.isArray(data) ? data : [];
    },
  },

  pause_alert_rule: {
    description: "Pause or resume an alert rule.",
    params: z.object({
      uid: z.string().describe("Alert rule UID"),
      paused: z.boolean().describe("true to pause, false to resume"),
    }),
    returns: z.object({ uid: z.string(), is_paused: z.boolean() }),
    execute: async (params, ctx) => {
      const data = await gfPost(
        ctx,
        `/v1/provisioning/alert-rules/${enc(params.uid)}`,
        { is_paused: params.paused },
        "PATCH",
      );
      return { uid: data.uid ?? params.uid, is_paused: params.paused };
    },
  },
};
