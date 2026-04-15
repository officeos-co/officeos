import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { SHEETS_API, shtPost } from "../core/client.ts";

export const sheets: Record<string, ActionDefinition> = {
  add_sheet: {
    description: "Add a new sheet tab to a spreadsheet.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      title: z.string().describe("New sheet name"),
    }),
    returns: z.object({ sheet_id: z.number(), title: z.string(), index: z.number() }),
    execute: async (params, ctx) => {
      const res = await shtPost(
        ctx,
        `${SHEETS_API}/${params.spreadsheet_id}:batchUpdate`,
        { requests: [{ addSheet: { properties: { title: params.title } } }] },
      );
      const reply = res.replies?.[0]?.addSheet?.properties ?? {};
      return { sheet_id: reply.sheetId ?? 0, title: reply.title ?? params.title, index: reply.index ?? 0 };
    },
  },

  delete_sheet: {
    description: "Delete a sheet tab from a spreadsheet.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      sheet_id: z.number().describe("Sheet ID (not name)"),
    }),
    returns: z.object({ status: z.string() }),
    execute: async (params, ctx) => {
      await shtPost(
        ctx,
        `${SHEETS_API}/${params.spreadsheet_id}:batchUpdate`,
        { requests: [{ deleteSheet: { sheetId: params.sheet_id } }] },
      );
      return { status: "deleted" };
    },
  },

  rename_sheet: {
    description: "Rename a sheet tab.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      sheet_id: z.number().describe("Sheet ID to rename"),
      title: z.string().describe("New sheet name"),
    }),
    returns: z.object({ sheet_id: z.number(), title: z.string() }),
    execute: async (params, ctx) => {
      await shtPost(
        ctx,
        `${SHEETS_API}/${params.spreadsheet_id}:batchUpdate`,
        {
          requests: [{
            updateSheetProperties: {
              properties: { sheetId: params.sheet_id, title: params.title },
              fields: "title",
            },
          }],
        },
      );
      return { sheet_id: params.sheet_id, title: params.title };
    },
  },

  duplicate_sheet: {
    description: "Duplicate a sheet tab.",
    params: z.object({
      spreadsheet_id: z.string().describe("Spreadsheet ID"),
      sheet_id: z.number().describe("Sheet ID to duplicate"),
      new_title: z.string().optional().describe("Title for the duplicate sheet"),
    }),
    returns: z.object({ sheet_id: z.number(), title: z.string(), index: z.number() }),
    execute: async (params, ctx) => {
      const req: any = { duplicateSheet: { sourceSheetId: params.sheet_id } };
      if (params.new_title) req.duplicateSheet.newSheetName = params.new_title;
      const res = await shtPost(
        ctx,
        `${SHEETS_API}/${params.spreadsheet_id}:batchUpdate`,
        { requests: [req] },
      );
      const reply = res.replies?.[0]?.duplicateSheet?.properties ?? {};
      return { sheet_id: reply.sheetId ?? 0, title: reply.title ?? "", index: reply.index ?? 0 };
    },
  },
};
