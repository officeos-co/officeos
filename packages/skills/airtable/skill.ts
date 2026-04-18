import { defineSkill, z } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

const AIRTABLE_API = "https://api.airtable.com/v0";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

function headers(apiKey: string) {
  return {
    Authorization: `Bearer ${apiKey}`,
    "Content-Type": "application/json",
  };
}

async function atGet(ctx: Ctx, path: string, params?: Record<string, string | string[]>) {
  let url = `${AIRTABLE_API}${path}`;
  if (params) {
    const qs = new URLSearchParams();
    for (const [k, v] of Object.entries(params)) {
      if (Array.isArray(v)) {
        v.forEach((val) => qs.append(k, val));
      } else {
        qs.set(k, v);
      }
    }
    const qstr = qs.toString();
    if (qstr) url += "?" + qstr;
  }
  const res = await ctx.fetch(url, { headers: headers(ctx.credentials.api_key) });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Airtable ${res.status}: ${body}`);
  }
  return res.json();
}

async function atPost(ctx: Ctx, path: string, body: unknown, method = "POST") {
  const res = await ctx.fetch(`${AIRTABLE_API}${path}`, {
    method,
    headers: headers(ctx.credentials.api_key),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Airtable ${res.status}: ${text}`);
  }
  return res.json();
}

const recordShape = z.object({
  id: z.string(),
  fields: z.record(z.unknown()),
  created_time: z.string(),
});

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Bases ──────────────────────────────────────────────────────────────

    list_bases: {
      description: "List all Airtable bases accessible to the API key.",
      params: z.object({}),
      returns: z.array(
        z.object({ id: z.string(), name: z.string(), permission_level: z.string() }),
      ),
      execute: async (_params, ctx) => {
        const data = await atGet(ctx, "/meta/bases");
        return data.bases;
      },
    },

    get_schema: {
      description: "Get the full schema (tables, fields, views) for a base.",
      params: z.object({
        base_id: z.string().describe("Base ID (starts with app)"),
      }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        tables: z.array(z.record(z.unknown())),
      }),
      execute: async (params, ctx) => atGet(ctx, `/meta/bases/${params.base_id}/tables`),
    },

    // ── Tables ─────────────────────────────────────────────────────────────

    list_tables: {
      description: "List all tables in a base.",
      params: z.object({
        base_id: z.string().describe("Base ID"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const data = await atGet(ctx, `/meta/bases/${params.base_id}/tables`);
        return data.tables;
      },
    },

    create_table: {
      description: "Create a new table in a base.",
      params: z.object({
        base_id: z.string().describe("Base ID"),
        name: z.string().describe("Table name"),
        description: z.string().optional().describe("Table description"),
        fields: z
          .array(z.record(z.unknown()))
          .optional()
          .describe("Initial field definitions (array of field objects)"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) =>
        atPost(ctx, `/meta/bases/${params.base_id}/tables`, {
          name: params.name,
          description: params.description,
          fields: params.fields,
        }),
    },

    update_table: {
      description: "Rename or update the description of a table.",
      params: z.object({
        base_id: z.string().describe("Base ID"),
        table_id: z.string().describe("Table ID"),
        name: z.string().optional().describe("New table name"),
        description: z.string().optional().describe("Updated description"),
      }),
      returns: z.object({ id: z.string(), name: z.string(), description: z.string().nullable() }),
      execute: async (params, ctx) =>
        atPost(ctx, `/meta/bases/${params.base_id}/tables/${params.table_id}`, {
          name: params.name,
          description: params.description,
        }, "PATCH"),
    },

    // ── Fields ─────────────────────────────────────────────────────────────

    create_field: {
      description: "Add a new field to a table.",
      params: z.object({
        base_id: z.string().describe("Base ID"),
        table_id: z.string().describe("Table ID"),
        name: z.string().describe("Field name"),
        type: z
          .string()
          .describe(
            "Field type: singleLineText, multilineText, number, currency, percent, date, dateTime, checkbox, singleSelect, multipleSelects, email, url, phoneNumber, rating, duration, autoNumber",
          ),
        options: z.record(z.unknown()).optional().describe("Type-specific options"),
        description: z.string().optional().describe("Field description"),
      }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        type: z.string(),
        options: z.record(z.unknown()).optional(),
        description: z.string().nullable().optional(),
      }),
      execute: async (params, ctx) =>
        atPost(ctx, `/meta/bases/${params.base_id}/tables/${params.table_id}/fields`, {
          name: params.name,
          type: params.type,
          options: params.options,
          description: params.description,
        }),
    },

    update_field: {
      description: "Rename or update the description of a field.",
      params: z.object({
        base_id: z.string().describe("Base ID"),
        table_id: z.string().describe("Table ID"),
        field_id: z.string().describe("Field ID"),
        name: z.string().optional().describe("New field name"),
        description: z.string().optional().describe("Updated description"),
      }),
      returns: z.object({ id: z.string(), name: z.string(), type: z.string(), description: z.string().nullable().optional() }),
      execute: async (params, ctx) =>
        atPost(
          ctx,
          `/meta/bases/${params.base_id}/tables/${params.table_id}/fields/${params.field_id}`,
          { name: params.name, description: params.description },
          "PATCH",
        ),
    },

    // ── Records ────────────────────────────────────────────────────────────

    list_records: {
      description: "List records from a table with optional filtering, sorting, and pagination.",
      params: z.object({
        base_id: z.string().describe("Base ID"),
        table_id: z.string().describe("Table ID or table name"),
        view: z.string().optional().describe("View name or ID to use"),
        fields: z.array(z.string()).optional().describe("Field names to include (omit for all)"),
        filter: z.string().optional().describe("Airtable formula filter, e.g. {Status}=\"Active\""),
        sort: z
          .array(z.object({ field: z.string(), direction: z.enum(["asc", "desc"]) }))
          .optional()
          .describe("Sort spec array"),
        max_records: z.number().int().default(100).describe("Maximum records to return"),
        offset: z.string().optional().describe("Pagination cursor from previous response"),
      }),
      returns: z.object({
        records: z.array(recordShape),
        offset: z.string().optional(),
      }),
      execute: async (params, ctx) => {
        const qParams: Record<string, string | string[]> = {};
        if (params.view) qParams.view = params.view;
        if (params.filter) qParams.filterByFormula = params.filter;
        if (params.max_records) qParams.maxRecords = String(params.max_records);
        if (params.offset) qParams.offset = params.offset;
        if (params.fields) qParams["fields[]"] = params.fields;
        if (params.sort) {
          params.sort.forEach((s, i) => {
            qParams[`sort[${i}][field]`] = s.field;
            qParams[`sort[${i}][direction]`] = s.direction;
          });
        }
        return atGet(ctx, `/${params.base_id}/${encodeURIComponent(params.table_id)}`, qParams);
      },
    },

    get_record: {
      description: "Get a single record by ID.",
      params: z.object({
        base_id: z.string().describe("Base ID"),
        table_id: z.string().describe("Table ID"),
        record_id: z.string().describe("Record ID"),
      }),
      returns: recordShape,
      execute: async (params, ctx) =>
        atGet(ctx, `/${params.base_id}/${params.table_id}/${params.record_id}`),
    },

    create_record: {
      description: "Create a new record in a table.",
      params: z.object({
        base_id: z.string().describe("Base ID"),
        table_id: z.string().describe("Table ID"),
        fields: z.record(z.unknown()).describe("Field name/value pairs for the new record"),
      }),
      returns: recordShape,
      execute: async (params, ctx) =>
        atPost(ctx, `/${params.base_id}/${params.table_id}`, { fields: params.fields }),
    },

    update_record: {
      description: "Update fields on an existing record (PATCH by default, PUT if merge=false).",
      params: z.object({
        base_id: z.string().describe("Base ID"),
        table_id: z.string().describe("Table ID"),
        record_id: z.string().describe("Record ID"),
        fields: z.record(z.unknown()).describe("Field values to update"),
        merge: z
          .boolean()
          .default(true)
          .describe("true = PATCH (merge); false = PUT (replace all fields)"),
      }),
      returns: recordShape,
      execute: async (params, ctx) =>
        atPost(
          ctx,
          `/${params.base_id}/${params.table_id}/${params.record_id}`,
          { fields: params.fields },
          params.merge ? "PATCH" : "PUT",
        ),
    },

    delete_record: {
      description: "Permanently delete a record.",
      params: z.object({
        base_id: z.string().describe("Base ID"),
        table_id: z.string().describe("Table ID"),
        record_id: z.string().describe("Record ID"),
      }),
      returns: z.object({ id: z.string(), deleted: z.boolean() }),
      execute: async (params, ctx) =>
        atPost(ctx, `/${params.base_id}/${params.table_id}/${params.record_id}`, {}, "DELETE"),
    },

    // ── Batch operations ───────────────────────────────────────────────────

    batch_create: {
      description: "Create multiple records in a single call (auto-batched in groups of 10).",
      params: z.object({
        base_id: z.string().describe("Base ID"),
        table_id: z.string().describe("Table ID"),
        records: z
          .array(z.record(z.unknown()))
          .describe("Array of field objects for each new record"),
      }),
      returns: z.array(recordShape),
      execute: async (params, ctx) => {
        const BATCH = 10;
        const all: unknown[] = [];
        for (let i = 0; i < params.records.length; i += BATCH) {
          const chunk = params.records.slice(i, i + BATCH);
          const res = await atPost(ctx, `/${params.base_id}/${params.table_id}`, {
            records: chunk.map((f) => ({ fields: f })),
          });
          all.push(...res.records);
        }
        return all;
      },
    },

    batch_update: {
      description: "Update multiple records in a single call (auto-batched in groups of 10).",
      params: z.object({
        base_id: z.string().describe("Base ID"),
        table_id: z.string().describe("Table ID"),
        records: z
          .array(z.object({ id: z.string(), fields: z.record(z.unknown()) }))
          .describe("Array of {id, fields} objects"),
        merge: z.boolean().default(true).describe("true = PATCH; false = PUT"),
      }),
      returns: z.array(recordShape),
      execute: async (params, ctx) => {
        const BATCH = 10;
        const all: unknown[] = [];
        const method = params.merge ? "PATCH" : "PUT";
        for (let i = 0; i < params.records.length; i += BATCH) {
          const chunk = params.records.slice(i, i + BATCH);
          const res = await atPost(ctx, `/${params.base_id}/${params.table_id}`, { records: chunk }, method);
          all.push(...res.records);
        }
        return all;
      },
    },

    batch_delete: {
      description: "Delete multiple records in a single call (auto-batched in groups of 10).",
      params: z.object({
        base_id: z.string().describe("Base ID"),
        table_id: z.string().describe("Table ID"),
        record_ids: z.array(z.string()).describe("Record IDs to delete"),
      }),
      returns: z.array(z.object({ id: z.string(), deleted: z.boolean() })),
      execute: async (params, ctx) => {
        const BATCH = 10;
        const all: unknown[] = [];
        for (let i = 0; i < params.record_ids.length; i += BATCH) {
          const chunk = params.record_ids.slice(i, i + BATCH);
          const qs = chunk.map((id) => `records[]=${id}`).join("&");
          const res = await ctx.fetch(
            `${AIRTABLE_API}/${params.base_id}/${params.table_id}?${qs}`,
            { method: "DELETE", headers: headers(ctx.credentials.api_key) },
          );
          if (!res.ok) {
            const text = await res.text();
            throw new Error(`Airtable ${res.status}: ${text}`);
          }
          const data = await res.json();
          all.push(...data.records);
        }
        return all;
      },
    },
  },
});
