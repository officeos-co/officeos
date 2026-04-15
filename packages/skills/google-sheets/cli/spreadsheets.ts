import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { SHEETS_API, shtFetch, shtPost } from "../core/client.ts";

export const spreadsheets: Record<string, ActionDefinition> = {
  create_spreadsheet: {
    description: "Create a new spreadsheet.",
    params: z.object({
      title: z.string().describe("Spreadsheet title"),
    }),
    returns: z.object({
      spreadsheet_id: z.string(),
      title: z.string(),
      url: z.string(),
      sheets: z.array(z.string()),
    }),
    execute: async (params, ctx) => {
      const res = await shtPost(ctx, SHEETS_API, { properties: { title: params.title } });
      return {
        spreadsheet_id: res.spreadsheetId,
        title: res.properties?.title ?? params.title,
        url: res.spreadsheetUrl ?? "",
        sheets: (res.sheets ?? []).map((s: any) => s.properties?.title ?? ""),
      };
    },
  },

  get_spreadsheet: {
    description: "Get spreadsheet metadata and sheet list.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
    }),
    returns: z.object({
      spreadsheet_id: z.string(),
      title: z.string(),
      url: z.string(),
      locale: z.string().optional(),
      time_zone: z.string().optional(),
      sheets: z.array(z.object({
        sheet_id: z.number(),
        title: z.string(),
        index: z.number(),
        row_count: z.number(),
        column_count: z.number(),
      })),
    }),
    execute: async (params, ctx) => {
      const res = await shtFetch(ctx, `${SHEETS_API}/${params.spreadsheet_id}`);
      return {
        spreadsheet_id: res.spreadsheetId,
        title: res.properties?.title ?? "",
        url: res.spreadsheetUrl ?? "",
        locale: res.properties?.locale,
        time_zone: res.properties?.timeZone,
        sheets: (res.sheets ?? []).map((s: any) => ({
          sheet_id: s.properties?.sheetId ?? 0,
          title: s.properties?.title ?? "",
          index: s.properties?.index ?? 0,
          row_count: s.properties?.gridProperties?.rowCount ?? 0,
          column_count: s.properties?.gridProperties?.columnCount ?? 0,
        })),
      };
    },
  },

  list_sheets: {
    description: "List all sheet tabs in a spreadsheet.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
    }),
    returns: z.array(z.object({
      sheet_id: z.number(),
      title: z.string(),
      index: z.number(),
      row_count: z.number(),
      column_count: z.number(),
    })),
    execute: async (params, ctx) => {
      const res = await shtFetch(ctx, `${SHEETS_API}/${params.spreadsheet_id}?fields=sheets.properties`);
      return (res.sheets ?? []).map((s: any) => ({
        sheet_id: s.properties?.sheetId ?? 0,
        title: s.properties?.title ?? "",
        index: s.properties?.index ?? 0,
        row_count: s.properties?.gridProperties?.rowCount ?? 0,
        column_count: s.properties?.gridProperties?.columnCount ?? 0,
      }));
    },
  },
};
