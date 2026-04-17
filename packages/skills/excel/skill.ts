import { defineSkill, z } from "@harro/skill-sdk";

import doc from "./SKILL.md";

type Ctx = {
  fetch: typeof globalThis.fetch;
  credentials: Record<string, string>;
};

async function excelProxy(
  ctx: Ctx,
  operation: string,
  workbook_id: string | undefined,
  params: Record<string, unknown>,
) {
  const res = await ctx.fetch(ctx.credentials.proxy_url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ operation, workbook_id, params }),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Excel proxy ${res.status}: ${body}`);
  }
  return res.json();
}

export default defineSkill({
  name: "excel",
  title: "Excel",
  logo: '<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8l-6-6Zm0 2 4 4h-4V4ZM8 12h2l1.5 2L13 12h2l-2.5 3.5L15 19h-2l-1.5-2L10 19H8l2.5-3.5L8 12Z"/></svg>',
  emoji: "\ud83d\udcca",
  description:
    "Create, read, write, format, and export Excel workbooks (.xlsx) with full support for formulas, tables, charts, and data operations via ExcelJS.",
  doc,

  credentials: {
    proxy_url: {
      label: "Proxy URL",
      kind: "text",
      placeholder: "http://localhost:3100/excel",
      help: "URL of the Excel file proxy service that manages workbook state.",
    },
  },

  actions: {
    // ── Workbook ──────────────────────────────────────────────────────

    create_workbook: {
      description: "Create a new empty workbook.",
      params: z.object({}),
      returns: z.object({
        workbook_id: z.string().describe("Handle for subsequent operations"),
      }),
      execute: async (_params, ctx) => {
        return excelProxy(ctx, "create_workbook", undefined, {});
      },
    },

    open_workbook: {
      description: "Open an existing .xlsx workbook from a file path.",
      params: z.object({
        file_path: z.string().describe("Path to .xlsx file"),
      }),
      returns: z.object({
        workbook_id: z.string().describe("Handle for subsequent operations"),
        sheet_count: z.number().describe("Number of sheets"),
        sheet_names: z.array(z.string()).describe("List of sheet names"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "open_workbook", undefined, {
          file_path: params.file_path,
        });
      },
    },

    save_workbook: {
      description: "Save the current workbook to a file path.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        file_path: z.string().describe("Output path for the .xlsx file"),
      }),
      returns: z.object({
        file_path: z.string().describe("Saved file path"),
        file_size: z.number().describe("File size in bytes"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "save_workbook", params.workbook_id, {
          file_path: params.file_path,
        });
      },
    },

    get_workbook_info: {
      description: "Get metadata about the current workbook.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
      }),
      returns: z.object({
        sheet_count: z.number().describe("Number of sheets"),
        sheet_names: z.array(z.string()).describe("List of sheet names"),
        creator: z.string().nullable().describe("Workbook creator"),
        created_at: z.string().nullable().describe("Creation timestamp"),
        modified_at: z.string().nullable().describe("Last modified timestamp"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "get_workbook_info", params.workbook_id, {});
      },
    },

    // ── Sheets ────────────────────────────────────────────────────────

    list_sheets: {
      description: "List all sheets in the workbook.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
      }),
      returns: z.array(
        z.object({
          name: z.string().describe("Sheet name"),
          index: z.number().describe("Sheet index"),
          row_count: z.number().describe("Number of rows with data"),
          column_count: z.number().describe("Number of columns with data"),
          state: z.string().describe("Visibility state (visible/hidden)"),
        }),
      ),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "list_sheets", params.workbook_id, {});
      },
    },

    add_sheet: {
      description: "Add a new sheet to the workbook.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        name: z.string().describe("Sheet name"),
        position: z
          .number()
          .optional()
          .describe("Zero-based index to insert at (default: end)"),
      }),
      returns: z.object({
        name: z.string().describe("Sheet name"),
        index: z.number().describe("Sheet index"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "add_sheet", params.workbook_id, {
          name: params.name,
          position: params.position,
        });
      },
    },

    remove_sheet: {
      description: "Remove a sheet from the workbook.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        name: z.string().describe("Sheet name to remove"),
      }),
      returns: z.object({
        removed: z.boolean().describe("Whether the sheet was removed"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "remove_sheet", params.workbook_id, {
          name: params.name,
        });
      },
    },

    rename_sheet: {
      description: "Rename a sheet.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        old_name: z.string().describe("Current sheet name"),
        new_name: z.string().describe("New sheet name"),
      }),
      returns: z.object({
        old_name: z.string().describe("Previous name"),
        new_name: z.string().describe("New name"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "rename_sheet", params.workbook_id, {
          old_name: params.old_name,
          new_name: params.new_name,
        });
      },
    },

    duplicate_sheet: {
      description: "Duplicate a sheet within the workbook.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        name: z.string().describe("Sheet to duplicate"),
        new_name: z
          .string()
          .optional()
          .describe("Name for the duplicated sheet"),
      }),
      returns: z.object({
        name: z.string().describe("New sheet name"),
        index: z.number().describe("New sheet index"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "duplicate_sheet", params.workbook_id, {
          name: params.name,
          new_name: params.new_name,
        });
      },
    },

    get_sheet_info: {
      description: "Get detailed info about a single sheet.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        name: z.string().describe("Sheet name"),
      }),
      returns: z.object({
        name: z.string().describe("Sheet name"),
        row_count: z.number().describe("Number of rows with data"),
        column_count: z.number().describe("Number of columns with data"),
        state: z.string().describe("Visibility state"),
        merged_cells: z
          .array(z.string())
          .describe("List of merged cell ranges"),
        has_autofilter: z.boolean().describe("Whether autofilter is applied"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "get_sheet_info", params.workbook_id, {
          name: params.name,
        });
      },
    },

    // ── Read ──────────────────────────────────────────────────────────

    get_cell: {
      description: "Get a single cell value.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        cell: z.string().describe("Cell reference (e.g. B5)"),
      }),
      returns: z.object({
        value: z.unknown().describe("Cell value"),
        type: z
          .string()
          .describe("Value type (string, number, date, boolean, formula)"),
        formula: z.string().nullable().describe("Formula if applicable"),
        formatted_value: z.string().describe("Display-formatted value"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "get_cell", params.workbook_id, {
          sheet: params.sheet,
          cell: params.cell,
        });
      },
    },

    get_range: {
      description: "Get a range of cells as a 2D array.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        range: z.string().describe("Cell range (e.g. A1:D10)"),
      }),
      returns: z.object({
        rows: z.array(z.array(z.unknown())).describe("2D array of cell values"),
        columns: z.number().describe("Number of columns"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "get_range", params.workbook_id, {
          sheet: params.sheet,
          range: params.range,
        });
      },
    },

    get_row: {
      description: "Get all values in a row.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        row: z.number().describe("Row number (1-based)"),
      }),
      returns: z.array(z.unknown()).describe("List of cell values in the row"),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "get_row", params.workbook_id, {
          sheet: params.sheet,
          row: params.row,
        });
      },
    },

    get_column: {
      description: "Get all values in a column.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        column: z.string().describe("Column letter (e.g. B)"),
      }),
      returns: z
        .array(z.unknown())
        .describe("List of cell values in the column"),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "get_column", params.workbook_id, {
          sheet: params.sheet,
          column: params.column,
        });
      },
    },

    get_all_data: {
      description: "Get all data from a sheet as a 2D array.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
      }),
      returns: z.object({
        rows: z
          .array(z.array(z.unknown()))
          .describe("2D array of all cell values"),
        row_count: z.number().describe("Number of rows"),
        column_count: z.number().describe("Number of columns"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "get_all_data", params.workbook_id, {
          sheet: params.sheet,
        });
      },
    },

    // ── Write ─────────────────────────────────────────────────────────

    set_cell: {
      description: "Set a single cell value.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        cell: z.string().describe("Cell reference (e.g. B5)"),
        value: z.unknown().describe("Value to set"),
        type: z
          .enum(["string", "number", "date", "boolean", "auto"])
          .default("auto")
          .describe("Value type"),
      }),
      returns: z.object({
        cell: z.string().describe("Cell reference"),
        value: z.unknown().describe("Value that was set"),
        type: z.string().describe("Resolved value type"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "set_cell", params.workbook_id, {
          sheet: params.sheet,
          cell: params.cell,
          value: params.value,
          type: params.type,
        });
      },
    },

    set_range: {
      description: "Set a range of cells from a 2D array.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        range: z.string().describe("Target range (e.g. A1:C2)"),
        values: z.array(z.array(z.unknown())).describe("2D array of values"),
      }),
      returns: z.object({
        range: z.string().describe("Written range"),
        rows_written: z.number().describe("Number of rows written"),
        columns_written: z.number().describe("Number of columns written"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "set_range", params.workbook_id, {
          sheet: params.sheet,
          range: params.range,
          values: params.values,
        });
      },
    },

    insert_row: {
      description: "Insert a new row at a given position.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        row: z.number().describe("Position to insert at (1-based)"),
        values: z
          .array(z.unknown())
          .optional()
          .describe("Values for the new row"),
      }),
      returns: z.object({
        row: z.number().describe("Inserted row number"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "insert_row", params.workbook_id, {
          sheet: params.sheet,
          row: params.row,
          values: params.values,
        });
      },
    },

    insert_column: {
      description: "Insert a new column at a given position.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        column: z.string().describe("Column letter to insert at"),
        values: z
          .array(z.unknown())
          .optional()
          .describe("Values for the new column"),
      }),
      returns: z.object({
        column: z.string().describe("Inserted column letter"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "insert_column", params.workbook_id, {
          sheet: params.sheet,
          column: params.column,
          values: params.values,
        });
      },
    },

    delete_row: {
      description: "Delete a row from a sheet.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        row: z.number().describe("Row number to delete"),
      }),
      returns: z.object({
        deleted: z.boolean().describe("Whether the row was deleted"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "delete_row", params.workbook_id, {
          sheet: params.sheet,
          row: params.row,
        });
      },
    },

    delete_column: {
      description: "Delete a column from a sheet.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        column: z.string().describe("Column letter to delete"),
      }),
      returns: z.object({
        deleted: z.boolean().describe("Whether the column was deleted"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "delete_column", params.workbook_id, {
          sheet: params.sheet,
          column: params.column,
        });
      },
    },

    // ── Format ────────────────────────────────────────────────────────

    format_cell: {
      description: "Apply formatting to a single cell.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        cell: z.string().describe("Cell reference"),
        bold: z.boolean().optional().describe("Bold text"),
        italic: z.boolean().optional().describe("Italic text"),
        font_size: z.number().optional().describe("Font size in points"),
        font_color: z
          .string()
          .optional()
          .describe("Font color (hex, e.g. #FF0000)"),
        bg_color: z.string().optional().describe("Background fill color (hex)"),
        number_format: z
          .string()
          .optional()
          .describe("Number format string (e.g. #,##0.00)"),
        alignment: z
          .enum(["left", "center", "right"])
          .optional()
          .describe("Horizontal alignment"),
      }),
      returns: z.object({
        cell: z.string().describe("Formatted cell"),
        formats_applied: z
          .array(z.string())
          .describe("List of applied formats"),
      }),
      execute: async (params, ctx) => {
        const { workbook_id, ...rest } = params;
        return excelProxy(ctx, "format_cell", workbook_id, rest);
      },
    },

    format_range: {
      description: "Apply formatting to a range of cells.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        range: z.string().describe("Cell range (e.g. A1:D1)"),
        bold: z.boolean().optional().describe("Bold text"),
        italic: z.boolean().optional().describe("Italic text"),
        font_size: z.number().optional().describe("Font size in points"),
        font_color: z.string().optional().describe("Font color (hex)"),
        bg_color: z.string().optional().describe("Background fill color (hex)"),
        number_format: z.string().optional().describe("Number format string"),
        alignment: z
          .enum(["left", "center", "right"])
          .optional()
          .describe("Horizontal alignment"),
      }),
      returns: z.object({
        range: z.string().describe("Formatted range"),
        formats_applied: z
          .array(z.string())
          .describe("List of applied formats"),
      }),
      execute: async (params, ctx) => {
        const { workbook_id, ...rest } = params;
        return excelProxy(ctx, "format_range", workbook_id, rest);
      },
    },

    set_column_width: {
      description: "Set the width of a column.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        column: z.string().describe("Column letter"),
        width: z.number().describe("Width in character units"),
      }),
      returns: z.object({
        column: z.string().describe("Column letter"),
        width: z.number().describe("Applied width"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "set_column_width", params.workbook_id, {
          sheet: params.sheet,
          column: params.column,
          width: params.width,
        });
      },
    },

    set_row_height: {
      description: "Set the height of a row.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        row: z.number().describe("Row number (1-based)"),
        height: z.number().describe("Height in points"),
      }),
      returns: z.object({
        row: z.number().describe("Row number"),
        height: z.number().describe("Applied height"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "set_row_height", params.workbook_id, {
          sheet: params.sheet,
          row: params.row,
          height: params.height,
        });
      },
    },

    merge_cells: {
      description: "Merge a range of cells.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        range: z.string().describe("Range to merge"),
      }),
      returns: z.object({
        range: z.string().describe("Merged range"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "merge_cells", params.workbook_id, {
          sheet: params.sheet,
          range: params.range,
        });
      },
    },

    unmerge_cells: {
      description: "Unmerge a previously merged range.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        range: z.string().describe("Range to unmerge"),
      }),
      returns: z.object({
        range: z.string().describe("Unmerged range"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "unmerge_cells", params.workbook_id, {
          sheet: params.sheet,
          range: params.range,
        });
      },
    },

    // ── Formulas ──────────────────────────────────────────────────────

    set_formula: {
      description: "Set a formula on a cell.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        cell: z.string().describe("Cell reference"),
        formula: z.string().describe("Excel formula (e.g. =SUM(A1:A10))"),
      }),
      returns: z.object({
        cell: z.string().describe("Cell reference"),
        formula: z.string().describe("Applied formula"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "set_formula", params.workbook_id, {
          sheet: params.sheet,
          cell: params.cell,
          formula: params.formula,
        });
      },
    },

    recalculate: {
      description: "Recalculate all formulas in the workbook.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
      }),
      returns: z.object({
        recalculated: z.boolean().describe("Whether recalculation completed"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "recalculate", params.workbook_id, {});
      },
    },

    // ── Tables ────────────────────────────────────────────────────────

    create_table: {
      description: "Create a structured table from a range.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        range: z.string().describe("Data range including headers"),
        columns: z.array(z.string()).describe("Column header names"),
        name: z.string().optional().describe("Table name (must be unique)"),
      }),
      returns: z.object({
        name: z.string().describe("Table name"),
        range: z.string().describe("Table range"),
        column_count: z.number().describe("Number of columns"),
        row_count: z.number().describe("Number of data rows"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "create_table", params.workbook_id, {
          sheet: params.sheet,
          range: params.range,
          columns: params.columns,
          name: params.name,
        });
      },
    },

    add_table_row: {
      description: "Add a row to an existing table.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        name: z.string().describe("Table name"),
        values: z.array(z.unknown()).describe("Values for the new row"),
      }),
      returns: z.object({
        table_name: z.string().describe("Table name"),
        row_count: z.number().describe("New total row count"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "add_table_row", params.workbook_id, {
          sheet: params.sheet,
          name: params.name,
          values: params.values,
        });
      },
    },

    // ── Charts ────────────────────────────────────────────────────────

    create_chart: {
      description: "Create a chart from a data range.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        type: z.enum(["bar", "line", "pie", "scatter"]).describe("Chart type"),
        data_range: z.string().describe("Source data range (e.g. A1:C5)"),
        title: z.string().optional().describe("Chart title"),
        position: z
          .string()
          .default("E2")
          .describe("Top-left cell for chart placement"),
      }),
      returns: z.object({
        chart_type: z.string().describe("Chart type"),
        data_range: z.string().describe("Source data range"),
        position: z.string().describe("Chart position"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "create_chart", params.workbook_id, {
          sheet: params.sheet,
          type: params.type,
          data_range: params.data_range,
          title: params.title,
          position: params.position,
        });
      },
    },

    // ── Export ─────────────────────────────────────────────────────────

    export_csv: {
      description: "Export a sheet to CSV format.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name to export"),
        file_path: z.string().describe("Output CSV file path"),
        delimiter: z.string().default(",").describe("Field delimiter"),
      }),
      returns: z.object({
        file_path: z.string().describe("Output file path"),
        row_count: z.number().describe("Number of rows exported"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "export_csv", params.workbook_id, {
          sheet: params.sheet,
          file_path: params.file_path,
          delimiter: params.delimiter,
        });
      },
    },

    export_json: {
      description: "Export a sheet to JSON format.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name to export"),
        file_path: z.string().describe("Output JSON file path"),
        headers: z
          .boolean()
          .default(true)
          .describe("Use first row as object keys"),
      }),
      returns: z.object({
        file_path: z.string().describe("Output file path"),
        record_count: z.number().describe("Number of records exported"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "export_json", params.workbook_id, {
          sheet: params.sheet,
          file_path: params.file_path,
          headers: params.headers,
        });
      },
    },

    export_pdf: {
      description: "Export a workbook or sheet to PDF format.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        file_path: z.string().describe("Output PDF file path"),
        sheet: z
          .string()
          .optional()
          .describe("Sheet to export (omit for all sheets)"),
        orientation: z
          .enum(["portrait", "landscape"])
          .default("portrait")
          .describe("Page orientation"),
      }),
      returns: z.object({
        file_path: z.string().describe("Output file path"),
        page_count: z.number().describe("Number of pages"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "export_pdf", params.workbook_id, {
          file_path: params.file_path,
          sheet: params.sheet,
          orientation: params.orientation,
        });
      },
    },

    // ── Filters ───────────────────────────────────────────────────────

    add_autofilter: {
      description: "Add autofilter to a header range.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        range: z.string().describe("Header range for autofilter"),
      }),
      returns: z.object({
        range: z.string().describe("Filter range"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "add_autofilter", params.workbook_id, {
          sheet: params.sheet,
          range: params.range,
        });
      },
    },

    sort_column: {
      description: "Sort data by a column.",
      params: z.object({
        workbook_id: z.string().describe("Workbook handle"),
        sheet: z.string().describe("Sheet name"),
        column: z.string().describe("Column letter to sort by"),
        order: z
          .enum(["ascending", "descending"])
          .default("ascending")
          .describe("Sort order"),
        range: z.string().optional().describe("Limit sort to a specific range"),
      }),
      returns: z.object({
        column: z.string().describe("Sorted column"),
        row_count: z.number().describe("Number of rows sorted"),
      }),
      execute: async (params, ctx) => {
        return excelProxy(ctx, "sort_column", params.workbook_id, {
          sheet: params.sheet,
          column: params.column,
          order: params.order,
          range: params.range,
        });
      },
    },
  },
});
