import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { SHEETS_API, shtFetch, shtPost, authHeaders, enc } from "../core/client.ts";

export const values: Record<string, ActionDefinition> = {
  get_values: {
    description: "Read cell values from a range.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      range: z.string().describe("A1 notation range (e.g. Sheet1!A1:D10)"),
    }),
    returns: z.object({
      range: z.string(),
      major_dimension: z.string(),
      values: z.array(z.array(z.any())),
    }),
    execute: async (params, ctx) => {
      const res = await shtFetch(ctx, `${SHEETS_API}/${params.spreadsheet_id}/values/${enc(params.range)}`);
      return { range: res.range ?? params.range, major_dimension: res.majorDimension ?? "ROWS", values: res.values ?? [] };
    },
  },

  batch_get_values: {
    description: "Read cell values from multiple ranges in a single request.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      ranges: z.array(z.string()).describe("List of A1 notation ranges"),
    }),
    returns: z.array(z.object({
      range: z.string(),
      major_dimension: z.string(),
      values: z.array(z.array(z.any())),
    })),
    execute: async (params, ctx) => {
      const q = params.ranges.map((r) => `ranges=${enc(r)}`).join("&");
      const res = await shtFetch(ctx, `${SHEETS_API}/${params.spreadsheet_id}/values:batchGet?${q}`);
      return (res.valueRanges ?? []).map((vr: any) => ({
        range: vr.range ?? "", major_dimension: vr.majorDimension ?? "ROWS", values: vr.values ?? [],
      }));
    },
  },

  update_values: {
    description: "Write cell values to a range.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      range: z.string().describe("A1 notation range to write to"),
      values: z.string().describe("2D JSON array of values"),
      value_input_option: z.enum(["RAW", "USER_ENTERED"]).default("USER_ENTERED").describe("How values are interpreted"),
    }),
    returns: z.object({
      updated_range: z.string(),
      updated_rows: z.number(),
      updated_columns: z.number(),
      updated_cells: z.number(),
    }),
    execute: async (params, ctx) => {
      const vals = JSON.parse(params.values);
      const res = await shtFetch(
        ctx,
        `${SHEETS_API}/${params.spreadsheet_id}/values/${enc(params.range)}?valueInputOption=${params.value_input_option}`,
        { method: "PUT", headers: authHeaders(ctx.credentials.access_token), body: JSON.stringify({ values: vals }) },
      );
      return {
        updated_range: res.updatedRange ?? "", updated_rows: res.updatedRows ?? 0,
        updated_columns: res.updatedColumns ?? 0, updated_cells: res.updatedCells ?? 0,
      };
    },
  },

  append_values: {
    description: "Append rows after the last data row in a range.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      range: z.string().describe("A1 notation range (rows appended after last data row)"),
      values: z.string().describe("2D JSON array of values to append"),
      value_input_option: z.enum(["RAW", "USER_ENTERED"]).default("USER_ENTERED").describe("How values are interpreted"),
    }),
    returns: z.object({
      updated_range: z.string(),
      updated_rows: z.number(),
      updated_columns: z.number(),
      updated_cells: z.number(),
    }),
    execute: async (params, ctx) => {
      const vals = JSON.parse(params.values);
      const res = await shtPost(
        ctx,
        `${SHEETS_API}/${params.spreadsheet_id}/values/${enc(params.range)}:append?valueInputOption=${params.value_input_option}`,
        { values: vals },
      );
      const updates = res.updates ?? {};
      return {
        updated_range: updates.updatedRange ?? "", updated_rows: updates.updatedRows ?? 0,
        updated_columns: updates.updatedColumns ?? 0, updated_cells: updates.updatedCells ?? 0,
      };
    },
  },

  batch_update_values: {
    description: "Write values to multiple ranges in a single request.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      data: z.string().describe("JSON array of {range, values} objects"),
      value_input_option: z.enum(["RAW", "USER_ENTERED"]).default("USER_ENTERED").describe("How values are interpreted"),
    }),
    returns: z.object({
      total_updated_cells: z.number(),
      total_updated_rows: z.number(),
      total_updated_columns: z.number(),
      responses: z.array(z.any()),
    }),
    execute: async (params, ctx) => {
      const data = JSON.parse(params.data);
      const res = await shtPost(ctx, `${SHEETS_API}/${params.spreadsheet_id}/values:batchUpdate`, {
        valueInputOption: params.value_input_option,
        data,
      });
      return {
        total_updated_cells: res.totalUpdatedCells ?? 0, total_updated_rows: res.totalUpdatedRows ?? 0,
        total_updated_columns: res.totalUpdatedColumns ?? 0, responses: res.responses ?? [],
      };
    },
  },

  clear_values: {
    description: "Clear cell values from a range (preserves formatting).",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      range: z.string().describe("A1 notation range to clear"),
    }),
    returns: z.object({ cleared_range: z.string() }),
    execute: async (params, ctx) => {
      const res = await shtPost(ctx, `${SHEETS_API}/${params.spreadsheet_id}/values/${enc(params.range)}:clear`, {});
      return { cleared_range: res.clearedRange ?? params.range };
    },
  },

  batch_clear_values: {
    description: "Clear cell values from multiple ranges.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      ranges: z.array(z.string()).describe("List of A1 notation ranges"),
    }),
    returns: z.array(z.string()).describe("List of cleared ranges"),
    execute: async (params, ctx) => {
      const res = await shtPost(ctx, `${SHEETS_API}/${params.spreadsheet_id}/values:batchClear`, { ranges: params.ranges });
      return res.clearedRanges ?? [];
    },
  },
};
