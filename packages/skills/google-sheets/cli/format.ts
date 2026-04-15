import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { SHEETS_API, shtFetch, shtPost, parseRange, getSheetId } from "../core/client.ts";

export const format: Record<string, ActionDefinition> = {
  format_cells: {
    description: "Apply formatting to a range of cells.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      range: z.string().describe("A1 notation range to format"),
      bold: z.boolean().optional().describe("Bold text"),
      italic: z.boolean().optional().describe("Italic text"),
      font_size: z.number().optional().describe("Font size in points"),
      background_color: z.string().optional().describe("Hex color code (e.g. #4285F4)"),
    }),
    returns: z.object({ status: z.string() }),
    execute: async (params, ctx) => {
      const sheetId = await getSheetId(ctx, params.spreadsheet_id, params.range);
      const gridRange = parseRange(sheetId, params.range);
      const cellFormat: any = {};
      const fields: string[] = [];
      if (params.bold !== undefined) {
        cellFormat.textFormat = { ...cellFormat.textFormat, bold: params.bold };
        fields.push("userEnteredFormat.textFormat.bold");
      }
      if (params.italic !== undefined) {
        cellFormat.textFormat = { ...cellFormat.textFormat, italic: params.italic };
        fields.push("userEnteredFormat.textFormat.italic");
      }
      if (params.font_size !== undefined) {
        cellFormat.textFormat = { ...cellFormat.textFormat, fontSize: params.font_size };
        fields.push("userEnteredFormat.textFormat.fontSize");
      }
      if (params.background_color) {
        const hex = params.background_color.replace("#", "");
        cellFormat.backgroundColor = {
          red: parseInt(hex.substring(0, 2), 16) / 255,
          green: parseInt(hex.substring(2, 4), 16) / 255,
          blue: parseInt(hex.substring(4, 6), 16) / 255,
        };
        fields.push("userEnteredFormat.backgroundColor");
      }
      await shtPost(ctx, `${SHEETS_API}/${params.spreadsheet_id}:batchUpdate`, {
        requests: [{
          repeatCell: { range: gridRange, cell: { userEnteredFormat: cellFormat }, fields: fields.join(",") },
        }],
      });
      return { status: "formatted" };
    },
  },

  auto_resize: {
    description: "Auto resize columns to fit content.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      sheet_id: z.number().describe("Sheet ID"),
      start_column: z.number().describe("Start column index (0-based)"),
      end_column: z.number().describe("End column index (exclusive)"),
    }),
    returns: z.object({ status: z.string() }),
    execute: async (params, ctx) => {
      await shtPost(ctx, `${SHEETS_API}/${params.spreadsheet_id}:batchUpdate`, {
        requests: [{
          autoResizeDimensions: {
            dimensions: { sheetId: params.sheet_id, dimension: "COLUMNS", startIndex: params.start_column, endIndex: params.end_column },
          },
        }],
      });
      return { status: "resized" };
    },
  },

  merge_cells: {
    description: "Merge a range of cells.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      range: z.string().describe("A1 notation range to merge"),
    }),
    returns: z.object({ status: z.string() }),
    execute: async (params, ctx) => {
      const sheetId = await getSheetId(ctx, params.spreadsheet_id, params.range);
      const gridRange = parseRange(sheetId, params.range);
      await shtPost(ctx, `${SHEETS_API}/${params.spreadsheet_id}:batchUpdate`, {
        requests: [{ mergeCells: { range: gridRange, mergeType: "MERGE_ALL" } }],
      });
      return { status: "merged" };
    },
  },

  unmerge_cells: {
    description: "Unmerge a range of cells.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      range: z.string().describe("A1 notation range to unmerge"),
    }),
    returns: z.object({ status: z.string() }),
    execute: async (params, ctx) => {
      const sheetId = await getSheetId(ctx, params.spreadsheet_id, params.range);
      const gridRange = parseRange(sheetId, params.range);
      await shtPost(ctx, `${SHEETS_API}/${params.spreadsheet_id}:batchUpdate`, {
        requests: [{ unmergeCells: { range: gridRange } }],
      });
      return { status: "unmerged" };
    },
  },

  sort_range: {
    description: "Sort a range by a column.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      range: z.string().describe("A1 notation range to sort"),
      sort_column: z.number().describe("Column index to sort by (0-based)"),
      ascending: z.boolean().default(true).describe("Sort direction"),
    }),
    returns: z.object({ status: z.string() }),
    execute: async (params, ctx) => {
      const sheetId = await getSheetId(ctx, params.spreadsheet_id, params.range);
      const gridRange = parseRange(sheetId, params.range);
      await shtPost(ctx, `${SHEETS_API}/${params.spreadsheet_id}:batchUpdate`, {
        requests: [{
          sortRange: {
            range: gridRange,
            sortSpecs: [{ dimensionIndex: params.sort_column, sortOrder: params.ascending ? "ASCENDING" : "DESCENDING" }],
          },
        }],
      });
      return { status: "sorted" };
    },
  },

  add_filter: {
    description: "Add a filter view to a range.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      range: z.string().describe("A1 notation range for the filter"),
    }),
    returns: z.object({ status: z.string() }),
    execute: async (params, ctx) => {
      const sheetId = await getSheetId(ctx, params.spreadsheet_id, params.range);
      const gridRange = parseRange(sheetId, params.range);
      await shtPost(ctx, `${SHEETS_API}/${params.spreadsheet_id}:batchUpdate`, {
        requests: [{ setBasicFilter: { filter: { range: gridRange } } }],
      });
      return { status: "filter added" };
    },
  },

  remove_filter: {
    description: "Remove the basic filter from a sheet.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      sheet_id: z.number().describe("Sheet ID"),
    }),
    returns: z.object({ status: z.string() }),
    execute: async (params, ctx) => {
      await shtPost(ctx, `${SHEETS_API}/${params.spreadsheet_id}:batchUpdate`, {
        requests: [{ clearBasicFilter: { sheetId: params.sheet_id } }],
      });
      return { status: "filter removed" };
    },
  },
};
