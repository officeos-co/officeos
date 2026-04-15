import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gfFetch, gfPost, gfDelete, enc } from "../core/client.ts";

export const datasources: Record<string, ActionDefinition> = {
  list_datasources: {
    description: "List all datasources.",
    params: z.object({}),
    returns: z.array(
      z.object({ uid: z.string(), name: z.string(), type: z.string(), url: z.string(), is_default: z.boolean() }),
    ),
    execute: async (_params, ctx) => {
      const data = await gfFetch(ctx, "/datasources");
      return data.map((d: any) => ({ uid: d.uid, name: d.name, type: d.type, url: d.url, is_default: d.isDefault }));
    },
  },

  get_datasource: {
    description: "Get a datasource by UID.",
    params: z.object({ uid: z.string().describe("Datasource UID") }),
    returns: z.object({
      uid: z.string(),
      name: z.string(),
      type: z.string(),
      url: z.string(),
      access: z.string(),
      is_default: z.boolean(),
      database: z.string(),
      json_data: z.any(),
    }),
    execute: async (params, ctx) => {
      const d = await gfFetch(ctx, `/datasources/uid/${enc(params.uid)}`);
      return {
        uid: d.uid, name: d.name, type: d.type, url: d.url,
        access: d.access, is_default: d.isDefault, database: d.database ?? "", json_data: d.jsonData ?? {},
      };
    },
  },

  create_datasource: {
    description: "Create a new datasource.",
    params: z.object({
      name: z.string().describe("Datasource name"),
      type: z.string().describe("Datasource type (e.g. prometheus, loki, postgres)"),
      url: z.string().describe("Datasource URL"),
      access: z.enum(["proxy", "direct"]).default("proxy").describe("Access mode"),
      is_default: z.boolean().default(false).describe("Set as default datasource"),
      database: z.string().optional().describe("Database name"),
      json_data: z.any().optional().describe("Additional type-specific config"),
    }),
    returns: z.object({ uid: z.string(), name: z.string(), type: z.string(), message: z.string() }),
    execute: async (params, ctx) => {
      const body: any = { name: params.name, type: params.type, url: params.url, access: params.access, isDefault: params.is_default };
      if (params.database) body.database = params.database;
      if (params.json_data) body.jsonData = params.json_data;
      const data = await gfPost(ctx, "/datasources", body);
      return {
        uid: data.datasource?.uid ?? data.uid ?? "",
        name: data.datasource?.name ?? data.name ?? params.name,
        type: data.datasource?.type ?? data.type ?? params.type,
        message: data.message ?? "Datasource created",
      };
    },
  },

  delete_datasource: {
    description: "Delete a datasource by UID.",
    params: z.object({ uid: z.string().describe("Datasource UID") }),
    returns: z.object({ message: z.string() }),
    execute: async (params, ctx) => {
      const data = await gfDelete(ctx, `/datasources/uid/${enc(params.uid)}`);
      return { message: data.message ?? "Datasource deleted" };
    },
  },
};
