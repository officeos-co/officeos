import { defineSkill, z } from "@harro/skill-sdk";
import doc from "./SKILL.md";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

async function proxyGet(ctx: Ctx, path: string) {
  const res = await ctx.fetch(`${ctx.credentials.proxy_url}${path}`, {
    headers: { "Content-Type": "application/json" },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`CSV proxy ${res.status}: ${body}`);
  }
  return res.json();
}

async function proxyPost(ctx: Ctx, path: string, body: unknown, method = "POST") {
  const res = await ctx.fetch(`${ctx.credentials.proxy_url}${path}`, {
    method,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`CSV proxy ${res.status}: ${text}`);
  }
  return res.json();
}

const parseResult = {
  file_id: z.string(),
  row_count: z.number(),
  column_count: z.number(),
  columns: z.array(z.string()),
};

export default defineSkill({
  name: "csv",
  title: "CSV",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8l-6-6Zm0 2 4 4h-4V4ZM8 13h8v2H8v-2Zm0 4h8v2H8v-2Z\"/></svg>",
  description:
    "Parse, transform, and analyse CSV data via a file-proxy service: filter rows, sort, add columns, merge datasets, compute statistics, and export to JSON.",
  doc,

  credentials: {
    proxy_url: {
      label: "Proxy URL",
      kind: "url",
      placeholder: "https://your-csv-proxy.example.com",
      help: "Base URL of the CSV file-proxy service that wraps PapaParse.",
    },
  },

  actions: {
    // ── Parsing & Serialising ──────────────────────────────────────────────

    parse: {
      description: "Parse raw CSV text into an in-memory dataset.",
      params: z.object({
        content: z.string().describe("Raw CSV text to parse"),
        delimiter: z.string().default(",").describe("Field delimiter character"),
        has_header: z.boolean().default(true).describe("Treat first row as column headers"),
        skip_empty: z.boolean().default(true).describe("Skip empty rows"),
        trim: z.boolean().default(true).describe("Trim whitespace from values"),
      }),
      returns: z.object(parseResult),
      execute: async (params, ctx) => proxyPost(ctx, "/datasets/parse", params),
    },

    parse_file: {
      description: "Download a CSV file from a URL and parse it into an in-memory dataset.",
      params: z.object({
        file_url: z.string().describe("URL of the CSV file to download"),
        delimiter: z.string().default(",").describe("Field delimiter character"),
        has_header: z.boolean().default(true).describe("Treat first row as column headers"),
        skip_empty: z.boolean().default(true).describe("Skip empty rows"),
      }),
      returns: z.object(parseResult),
      execute: async (params, ctx) => proxyPost(ctx, "/datasets/parse-file", params),
    },

    stringify: {
      description: "Serialise an in-memory dataset back to a CSV string.",
      params: z.object({
        file_id: z.string().describe("File ID of in-memory dataset"),
        delimiter: z.string().default(",").describe("Output field delimiter"),
        include_header: z.boolean().default(true).describe("Include header row in output"),
        quote_all: z.boolean().default(false).describe("Force quote all fields"),
      }),
      returns: z.object({
        content: z.string(),
        row_count: z.number(),
        column_count: z.number(),
      }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/datasets/${params.file_id}/stringify`, params),
    },

    // ── Inspection ─────────────────────────────────────────────────────────

    get_columns: {
      description: "List column names and row count of a dataset.",
      params: z.object({
        file_id: z.string().describe("File ID"),
      }),
      returns: z.object({
        columns: z.array(z.string()),
        column_count: z.number(),
        row_count: z.number(),
      }),
      execute: async (params, ctx) => proxyGet(ctx, `/datasets/${params.file_id}/columns`),
    },

    get_rows: {
      description: "Retrieve a paginated slice of rows from a dataset.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        offset: z.number().int().default(0).describe("Row offset (0-based)"),
        limit: z.number().int().default(100).describe("Maximum rows to return"),
      }),
      returns: z.object({
        rows: z.array(z.record(z.string())),
        total_rows: z.number(),
        offset: z.number(),
        limit: z.number(),
      }),
      execute: async (params, ctx) =>
        proxyGet(
          ctx,
          `/datasets/${params.file_id}/rows?offset=${params.offset}&limit=${params.limit}`,
        ),
    },

    stats: {
      description: "Compute descriptive statistics for numeric columns.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        columns: z
          .array(z.string())
          .optional()
          .describe("Columns to compute stats for (omit for all numeric columns)"),
      }),
      returns: z.record(
        z.object({
          min: z.number().nullable(),
          max: z.number().nullable(),
          mean: z.number().nullable(),
          median: z.number().nullable(),
          stddev: z.number().nullable(),
          null_count: z.number(),
          unique_count: z.number(),
        }),
      ),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/datasets/${params.file_id}/stats`, { columns: params.columns }),
    },

    // ── Transformation ─────────────────────────────────────────────────────

    filter_rows: {
      description: "Filter rows by a column condition. Returns a new dataset.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        column: z.string().describe("Column name to filter on"),
        operator: z
          .enum([
            "=",
            "!=",
            ">",
            ">=",
            "<",
            "<=",
            "contains",
            "starts_with",
            "ends_with",
            "is_empty",
            "is_not_empty",
          ])
          .describe("Comparison operator"),
        value: z
          .string()
          .optional()
          .describe("Comparison value (omit for is_empty / is_not_empty)"),
      }),
      returns: z.object({ file_id: z.string(), row_count: z.number(), column_count: z.number() }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/datasets/${params.file_id}/filter`, params),
    },

    sort: {
      description: "Sort rows by a column. Returns a new dataset.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        column: z.string().describe("Column to sort by"),
        direction: z.enum(["asc", "desc"]).default("asc").describe("Sort direction"),
        numeric: z.boolean().default(false).describe("Sort as numbers instead of strings"),
      }),
      returns: z.object({ file_id: z.string(), row_count: z.number() }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/datasets/${params.file_id}/sort`, params),
    },

    add_column: {
      description: "Add a new column to a dataset. Returns a new dataset.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        name: z.string().describe("New column name"),
        expression: z
          .string()
          .optional()
          .describe("Template using {{col_name}} placeholders or a constant"),
        values: z
          .array(z.string())
          .optional()
          .describe("Array of values, one per row (must match row count)"),
        default: z.string().optional().describe("Default value for all rows"),
      }),
      returns: z.object({ file_id: z.string(), columns: z.array(z.string()), row_count: z.number() }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/datasets/${params.file_id}/columns`, params),
    },

    rename_column: {
      description: "Rename a column in a dataset.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        old_name: z.string().describe("Existing column name"),
        new_name: z.string().describe("New column name"),
      }),
      returns: z.object({ file_id: z.string(), columns: z.array(z.string()) }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/datasets/${params.file_id}/columns/rename`, params),
    },

    drop_columns: {
      description: "Remove columns from a dataset. Returns a new dataset.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        columns: z.array(z.string()).describe("Column names to remove"),
      }),
      returns: z.object({ file_id: z.string(), columns: z.array(z.string()), row_count: z.number() }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/datasets/${params.file_id}/columns/drop`, { columns: params.columns }),
    },

    transform: {
      description: "Apply a transformation to all values in a column. Returns a new dataset.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        column: z.string().describe("Column to transform"),
        operation: z
          .enum(["uppercase", "lowercase", "trim", "number", "boolean", "date_iso", "replace"])
          .describe("Transformation operation"),
        find: z.string().optional().describe("For replace: string to find"),
        replace: z.string().optional().describe("For replace: replacement string"),
      }),
      returns: z.object({ file_id: z.string(), row_count: z.number() }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/datasets/${params.file_id}/transform`, params),
    },

    deduplicate: {
      description: "Remove duplicate rows. Returns a new dataset.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        columns: z
          .array(z.string())
          .optional()
          .describe("Columns to consider for deduplication (omit for all columns)"),
        keep: z
          .enum(["first", "last"])
          .default("first")
          .describe("Which duplicate to keep"),
      }),
      returns: z.object({ file_id: z.string(), row_count: z.number(), removed_count: z.number() }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/datasets/${params.file_id}/deduplicate`, params),
    },

    // ── Combining ──────────────────────────────────────────────────────────

    merge: {
      description: "Merge multiple datasets by stacking rows or joining on a column.",
      params: z.object({
        file_ids: z.array(z.string()).describe("File IDs of datasets to merge"),
        how: z
          .enum(["union", "join_inner", "join_left"])
          .default("union")
          .describe("Merge strategy: union stacks rows, join_inner/join_left join on a column"),
        on: z
          .string()
          .optional()
          .describe("Column name to join on (required for join operations)"),
      }),
      returns: z.object({
        file_id: z.string(),
        row_count: z.number(),
        column_count: z.number(),
        columns: z.array(z.string()),
      }),
      execute: async (params, ctx) => proxyPost(ctx, "/datasets/merge", params),
    },

    // ── Export ─────────────────────────────────────────────────────────────

    to_json: {
      description: "Export a dataset as a JSON array.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        orientation: z
          .enum(["records", "columns"])
          .default("records")
          .describe("records = array of objects, columns = object of arrays"),
      }),
      returns: z.object({
        data: z.union([z.array(z.record(z.unknown())), z.record(z.array(z.unknown()))]),
        row_count: z.number(),
        column_count: z.number(),
      }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/datasets/${params.file_id}/to-json`, {
          orientation: params.orientation,
        }),
    },

    download: {
      description: "Get a download URL for a CSV dataset.",
      params: z.object({
        file_id: z.string().describe("File ID"),
      }),
      returns: z.object({
        download_url: z.string(),
        filename: z.string(),
        size_bytes: z.number(),
        expires_at: z.string(),
      }),
      execute: async (params, ctx) => proxyGet(ctx, `/datasets/${params.file_id}/download`),
    },
  },
});
