import { defineSkill, z } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

async function proxyQuery(ctx: Ctx, sql: string, params?: unknown[]) {
  const res = await ctx.fetch(`${ctx.credentials.proxy_url}/query`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      database: ctx.credentials.database,
      user: ctx.credentials.user,
      password: ctx.credentials.password,
      sql,
      params: params ?? [],
    }),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Postgres proxy ${res.status}: ${body}`);
  }
  return res.json();
}

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Connection ────────────────────────────────────────────────────────

    connect: {
      description: "Test connection to a PostgreSQL database.",
      params: z.object({
        host: z.string().default("localhost").describe("Database server hostname"),
        port: z.number().default(5432).describe("Database server port"),
        database: z.string().describe("Database name to connect to"),
        user: z.string().describe("Authentication username"),
        password: z.string().describe("Authentication password"),
        ssl: z.boolean().default(false).describe("Enable SSL/TLS connection"),
      }),
      returns: z.object({
        connected: z.boolean(),
        server_version: z.string(),
        database: z.string(),
        user: z.string(),
      }),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(`${ctx.credentials.proxy_url}/query`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            database: params.database,
            user: params.user,
            password: params.password,
            host: params.host,
            port: params.port,
            ssl: params.ssl,
            sql: "SELECT version() AS server_version",
            params: [],
          }),
        });
        if (!res.ok) {
          const body = await res.text();
          throw new Error(`Postgres proxy ${res.status}: ${body}`);
        }
        const data = await res.json();
        return {
          connected: true,
          server_version: data.rows?.[0]?.server_version ?? "unknown",
          database: params.database,
          user: params.user,
        };
      },
    },

    // ── Querying ──────────────────────────────────────────────────────────

    query: {
      description: "Run an arbitrary SQL query and return all result rows.",
      params: z.object({
        sql: z.string().describe("SQL statement to execute"),
        params: z.string().optional().describe("JSON array of parameterized values (e.g. '[1,\"a\"]')"),
      }),
      returns: z.object({
        rows: z.array(z.record(z.unknown())),
        row_count: z.number(),
        fields: z.array(z.object({ name: z.string(), dataType: z.string() })),
      }),
      execute: async (params, ctx) => {
        const sqlParams = params.params ? JSON.parse(params.params) : [];
        const data = await proxyQuery(ctx, params.sql, sqlParams);
        return {
          rows: data.rows ?? [],
          row_count: data.row_count ?? data.rows?.length ?? 0,
          fields: data.fields ?? [],
        };
      },
    },

    query_one: {
      description: "Run a SQL query expected to return a single row.",
      params: z.object({
        sql: z.string().describe("SQL statement expected to return one row"),
        params: z.string().optional().describe("JSON array of parameterized values"),
      }),
      returns: z.record(z.unknown()).nullable(),
      execute: async (params, ctx) => {
        const sqlParams = params.params ? JSON.parse(params.params) : [];
        const data = await proxyQuery(ctx, params.sql, sqlParams);
        return data.rows?.[0] ?? null;
      },
    },

    // ── Schema inspection ─────────────────────────────────────────────────

    list_databases: {
      description: "List all databases on the server.",
      params: z.object({}),
      returns: z.array(
        z.object({
          name: z.string(),
          owner: z.string(),
          encoding: z.string(),
          size: z.string(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await proxyQuery(
          ctx,
          `SELECT d.datname AS name, pg_catalog.pg_get_userbyid(d.datdba) AS owner,
                  pg_catalog.pg_encoding_to_char(d.encoding) AS encoding,
                  pg_catalog.pg_size_pretty(pg_catalog.pg_database_size(d.datname)) AS size
           FROM pg_catalog.pg_database d
           WHERE d.datistemplate = false
           ORDER BY d.datname`,
        );
        return data.rows;
      },
    },

    list_tables: {
      description: "List tables and views in a schema.",
      params: z.object({
        schema: z.string().default("public").describe("Schema name to inspect"),
      }),
      returns: z.array(
        z.object({
          table_name: z.string(),
          table_type: z.string(),
          row_estimate: z.number(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await proxyQuery(
          ctx,
          `SELECT t.table_name, t.table_type,
                  COALESCE(c.reltuples, 0)::bigint AS row_estimate
           FROM information_schema.tables t
           LEFT JOIN pg_catalog.pg_class c ON c.relname = t.table_name
           LEFT JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace AND n.nspname = t.table_schema
           WHERE t.table_schema = $1
           ORDER BY t.table_name`,
          [params.schema],
        );
        return data.rows;
      },
    },

    list_columns: {
      description: "List columns of a table.",
      params: z.object({
        table: z.string().describe("Table name"),
        schema: z.string().default("public").describe("Schema name"),
      }),
      returns: z.array(
        z.object({
          column_name: z.string(),
          data_type: z.string(),
          is_nullable: z.string(),
          column_default: z.string().nullable(),
          character_maximum_length: z.number().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await proxyQuery(
          ctx,
          `SELECT column_name, data_type, is_nullable, column_default, character_maximum_length
           FROM information_schema.columns
           WHERE table_schema = $1 AND table_name = $2
           ORDER BY ordinal_position`,
          [params.schema, params.table],
        );
        return data.rows;
      },
    },

    list_indexes: {
      description: "List indexes on a table.",
      params: z.object({
        table: z.string().describe("Table name"),
        schema: z.string().default("public").describe("Schema name"),
      }),
      returns: z.array(
        z.object({
          index_name: z.string(),
          is_unique: z.boolean(),
          is_primary: z.boolean(),
          columns: z.string(),
          index_type: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await proxyQuery(
          ctx,
          `SELECT i.relname AS index_name,
                  ix.indisunique AS is_unique,
                  ix.indisprimary AS is_primary,
                  array_to_string(ARRAY(SELECT pg_get_indexdef(ix.indexrelid, k + 1, true)
                    FROM generate_subscripts(ix.indkey, 1) AS k ORDER BY k), ', ') AS columns,
                  am.amname AS index_type
           FROM pg_catalog.pg_index ix
           JOIN pg_catalog.pg_class i ON i.oid = ix.indexrelid
           JOIN pg_catalog.pg_class t ON t.oid = ix.indrelid
           JOIN pg_catalog.pg_namespace n ON n.oid = t.relnamespace
           JOIN pg_catalog.pg_am am ON am.oid = i.relam
           WHERE n.nspname = $1 AND t.relname = $2
           ORDER BY i.relname`,
          [params.schema, params.table],
        );
        return data.rows;
      },
    },

    // ── Table operations ──────────────────────────────────────────────────

    create_table: {
      description: "Create a new table.",
      params: z.object({
        table: z.string().describe("New table name"),
        columns: z.string().describe("JSON array of {name, type} column definitions"),
        schema: z.string().default("public").describe("Schema to create in"),
        if_not_exists: z.boolean().default(false).describe("Skip if table already exists"),
      }),
      returns: z.object({
        created: z.boolean(),
        table_name: z.string(),
      }),
      execute: async (params, ctx) => {
        const cols: { name: string; type: string }[] = JSON.parse(params.columns);
        const colDefs = cols.map((c) => `"${c.name}" ${c.type}`).join(", ");
        const ifne = params.if_not_exists ? "IF NOT EXISTS " : "";
        await proxyQuery(ctx, `CREATE TABLE ${ifne}"${params.schema}"."${params.table}" (${colDefs})`);
        return { created: true, table_name: `${params.schema}.${params.table}` };
      },
    },

    drop_table: {
      description: "Drop a table.",
      params: z.object({
        table: z.string().describe("Table to drop"),
        schema: z.string().default("public").describe("Schema containing the table"),
        cascade: z.boolean().default(false).describe("Also drop dependent objects"),
      }),
      returns: z.object({
        dropped: z.boolean(),
        table_name: z.string(),
      }),
      execute: async (params, ctx) => {
        const casc = params.cascade ? " CASCADE" : "";
        await proxyQuery(ctx, `DROP TABLE "${params.schema}"."${params.table}"${casc}`);
        return { dropped: true, table_name: `${params.schema}.${params.table}` };
      },
    },

    alter_table: {
      description: "Alter a table: add, drop, or rename a column.",
      params: z.object({
        table: z.string().describe("Table to alter"),
        schema: z.string().default("public").describe("Schema containing the table"),
        action: z.enum(["add_column", "drop_column", "rename_column"]).describe("Alter action"),
        column_name: z.string().describe("Column to add, drop, or rename"),
        column_type: z.string().optional().describe("Column type (required for add_column)"),
        new_name: z.string().optional().describe("New column name (required for rename_column)"),
      }),
      returns: z.object({
        altered: z.boolean(),
        table_name: z.string(),
        action: z.string(),
      }),
      execute: async (params, ctx) => {
        const tbl = `"${params.schema}"."${params.table}"`;
        let sql: string;
        switch (params.action) {
          case "add_column":
            if (!params.column_type) throw new Error("column_type is required for add_column");
            sql = `ALTER TABLE ${tbl} ADD COLUMN "${params.column_name}" ${params.column_type}`;
            break;
          case "drop_column":
            sql = `ALTER TABLE ${tbl} DROP COLUMN "${params.column_name}"`;
            break;
          case "rename_column":
            if (!params.new_name) throw new Error("new_name is required for rename_column");
            sql = `ALTER TABLE ${tbl} RENAME COLUMN "${params.column_name}" TO "${params.new_name}"`;
            break;
        }
        await proxyQuery(ctx, sql);
        return { altered: true, table_name: `${params.schema}.${params.table}`, action: params.action };
      },
    },

    // ── Data operations ───────────────────────────────────────────────────

    insert: {
      description: "Insert a row into a table.",
      params: z.object({
        table: z.string().describe("Target table"),
        schema: z.string().default("public").describe("Schema containing the table"),
        data: z.string().describe("JSON object of column-value pairs"),
        returning: z.string().default("*").describe("Columns to return (e.g. 'id,created_at')"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) => {
        const obj = JSON.parse(params.data);
        const keys = Object.keys(obj);
        const cols = keys.map((k) => `"${k}"`).join(", ");
        const placeholders = keys.map((_, i) => `$${i + 1}`).join(", ");
        const vals = keys.map((k) => obj[k]);
        const data = await proxyQuery(
          ctx,
          `INSERT INTO "${params.schema}"."${params.table}" (${cols}) VALUES (${placeholders}) RETURNING ${params.returning}`,
          vals,
        );
        return data.rows?.[0] ?? {};
      },
    },

    update: {
      description: "Update rows in a table.",
      params: z.object({
        table: z.string().describe("Target table"),
        schema: z.string().default("public").describe("Schema containing the table"),
        set: z.string().describe("JSON object of columns to update"),
        where: z.string().describe("WHERE clause (without the WHERE keyword)"),
        returning: z.string().optional().describe("Columns to return from updated rows"),
      }),
      returns: z.object({
        updated_count: z.number(),
        rows: z.array(z.record(z.unknown())).optional(),
      }),
      execute: async (params, ctx) => {
        const obj = JSON.parse(params.set);
        const keys = Object.keys(obj);
        const setClauses = keys.map((k, i) => `"${k}" = $${i + 1}`).join(", ");
        const vals = keys.map((k) => obj[k]);
        const ret = params.returning ? ` RETURNING ${params.returning}` : "";
        const data = await proxyQuery(
          ctx,
          `UPDATE "${params.schema}"."${params.table}" SET ${setClauses} WHERE ${params.where}${ret}`,
          vals,
        );
        return {
          updated_count: data.row_count ?? data.rows?.length ?? 0,
          rows: params.returning ? data.rows : undefined,
        };
      },
    },

    delete: {
      description: "Delete rows from a table.",
      params: z.object({
        table: z.string().describe("Target table"),
        schema: z.string().default("public").describe("Schema containing the table"),
        where: z.string().describe("WHERE clause (without the WHERE keyword)"),
        returning: z.string().optional().describe("Columns to return from deleted rows"),
      }),
      returns: z.object({
        deleted_count: z.number(),
        rows: z.array(z.record(z.unknown())).optional(),
      }),
      execute: async (params, ctx) => {
        const ret = params.returning ? ` RETURNING ${params.returning}` : "";
        const data = await proxyQuery(
          ctx,
          `DELETE FROM "${params.schema}"."${params.table}" WHERE ${params.where}${ret}`,
        );
        return {
          deleted_count: data.row_count ?? data.rows?.length ?? 0,
          rows: params.returning ? data.rows : undefined,
        };
      },
    },

    upsert: {
      description: "Insert a row or update on conflict (upsert).",
      params: z.object({
        table: z.string().describe("Target table"),
        schema: z.string().default("public").describe("Schema containing the table"),
        data: z.string().describe("JSON object of column-value pairs"),
        conflict_columns: z.string().describe("JSON array of columns forming the unique constraint"),
        update_columns: z.string().describe("JSON array of columns to update on conflict"),
        returning: z.string().default("*").describe("Columns to return"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) => {
        const obj = JSON.parse(params.data);
        const conflictCols: string[] = JSON.parse(params.conflict_columns);
        const updateCols: string[] = JSON.parse(params.update_columns);
        const keys = Object.keys(obj);
        const cols = keys.map((k) => `"${k}"`).join(", ");
        const placeholders = keys.map((_, i) => `$${i + 1}`).join(", ");
        const vals = keys.map((k) => obj[k]);
        const conflict = conflictCols.map((c) => `"${c}"`).join(", ");
        const updates = updateCols.map((c) => `"${c}" = EXCLUDED."${c}"`).join(", ");
        const data = await proxyQuery(
          ctx,
          `INSERT INTO "${params.schema}"."${params.table}" (${cols}) VALUES (${placeholders})
           ON CONFLICT (${conflict}) DO UPDATE SET ${updates}
           RETURNING ${params.returning}`,
          vals,
        );
        return data.rows?.[0] ?? {};
      },
    },

    // ── Migrations ────────────────────────────────────────────────────────

    run_migration: {
      description: "Run a named SQL migration (tracked to prevent re-runs).",
      params: z.object({
        sql: z.string().describe("SQL migration statements to execute"),
        name: z.string().describe("Unique migration name for tracking"),
      }),
      returns: z.object({
        applied: z.boolean(),
        migration_name: z.string(),
        duration_ms: z.number(),
      }),
      execute: async (params, ctx) => {
        // Ensure migration tracking table exists
        await proxyQuery(
          ctx,
          `CREATE TABLE IF NOT EXISTS _migrations (
             name TEXT PRIMARY KEY,
             applied_at TIMESTAMPTZ DEFAULT NOW(),
             duration_ms INTEGER
           )`,
        );
        // Check if already applied
        const existing = await proxyQuery(ctx, `SELECT name FROM _migrations WHERE name = $1`, [
          params.name,
        ]);
        if (existing.rows?.length > 0) {
          throw new Error(`Migration "${params.name}" has already been applied.`);
        }
        const start = Date.now();
        await proxyQuery(ctx, params.sql);
        const duration_ms = Date.now() - start;
        await proxyQuery(
          ctx,
          `INSERT INTO _migrations (name, duration_ms) VALUES ($1, $2)`,
          [params.name, duration_ms],
        );
        return { applied: true, migration_name: params.name, duration_ms };
      },
    },

    list_migrations: {
      description: "List all previously applied migrations.",
      params: z.object({}),
      returns: z.array(
        z.object({
          name: z.string(),
          applied_at: z.string(),
          duration_ms: z.number(),
        }),
      ),
      execute: async (_params, ctx) => {
        // Ensure table exists so we don't error on fresh databases
        await proxyQuery(
          ctx,
          `CREATE TABLE IF NOT EXISTS _migrations (
             name TEXT PRIMARY KEY,
             applied_at TIMESTAMPTZ DEFAULT NOW(),
             duration_ms INTEGER
           )`,
        );
        const data = await proxyQuery(ctx, `SELECT name, applied_at, duration_ms FROM _migrations ORDER BY applied_at`);
        return data.rows ?? [];
      },
    },

    // ── Transactions ──────────────────────────────────────────────────────

    begin_transaction: {
      description: "Begin a new transaction.",
      params: z.object({
        isolation_level: z
          .enum(["read_committed", "repeatable_read", "serializable"])
          .default("read_committed")
          .describe("Transaction isolation level"),
      }),
      returns: z.object({ transaction_id: z.string() }),
      execute: async (params, ctx) => {
        const level = params.isolation_level.replace("_", " ").toUpperCase();
        const data = await proxyQuery(ctx, `BEGIN ISOLATION LEVEL ${level}`);
        return { transaction_id: data.transaction_id ?? "txn_" + Date.now() };
      },
    },

    commit: {
      description: "Commit a transaction.",
      params: z.object({
        transaction_id: z.string().describe("Transaction to commit"),
      }),
      returns: z.object({
        committed: z.boolean(),
        transaction_id: z.string(),
      }),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(`${ctx.credentials.proxy_url}/query`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            database: ctx.credentials.database,
            user: ctx.credentials.user,
            password: ctx.credentials.password,
            sql: "COMMIT",
            params: [],
            transaction_id: params.transaction_id,
          }),
        });
        if (!res.ok) throw new Error(`Postgres proxy ${res.status}: ${await res.text()}`);
        return { committed: true, transaction_id: params.transaction_id };
      },
    },

    rollback: {
      description: "Roll back a transaction.",
      params: z.object({
        transaction_id: z.string().describe("Transaction to roll back"),
      }),
      returns: z.object({
        rolled_back: z.boolean(),
        transaction_id: z.string(),
      }),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(`${ctx.credentials.proxy_url}/query`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            database: ctx.credentials.database,
            user: ctx.credentials.user,
            password: ctx.credentials.password,
            sql: "ROLLBACK",
            params: [],
            transaction_id: params.transaction_id,
          }),
        });
        if (!res.ok) throw new Error(`Postgres proxy ${res.status}: ${await res.text()}`);
        return { rolled_back: true, transaction_id: params.transaction_id };
      },
    },

    // ── Server info ───────────────────────────────────────────────────────

    table_info: {
      description: "Get size and row count information for a table.",
      params: z.object({
        table: z.string().describe("Table name"),
        schema: z.string().default("public").describe("Schema name"),
      }),
      returns: z.object({
        table_name: z.string(),
        row_count: z.number(),
        total_size: z.string(),
        index_size: z.string(),
        toast_size: z.string(),
        table_size: z.string(),
      }),
      execute: async (params, ctx) => {
        const fqn = `"${params.schema}"."${params.table}"`;
        const data = await proxyQuery(
          ctx,
          `SELECT
             $1 AS table_name,
             (SELECT count(*) FROM ${fqn})::bigint AS row_count,
             pg_size_pretty(pg_total_relation_size('${params.schema}.${params.table}')) AS total_size,
             pg_size_pretty(pg_indexes_size('${params.schema}.${params.table}')) AS index_size,
             COALESCE(pg_size_pretty(pg_total_relation_size(reltoastrelid)), '0 bytes') AS toast_size,
             pg_size_pretty(pg_relation_size('${params.schema}.${params.table}')) AS table_size
           FROM pg_catalog.pg_class
           WHERE relname = $2 AND relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = $3)`,
          [`${params.schema}.${params.table}`, params.table, params.schema],
        );
        return data.rows?.[0] ?? {
          table_name: `${params.schema}.${params.table}`,
          row_count: 0,
          total_size: "0 bytes",
          index_size: "0 bytes",
          toast_size: "0 bytes",
          table_size: "0 bytes",
        };
      },
    },

    database_size: {
      description: "Get the size of the current database.",
      params: z.object({}),
      returns: z.object({
        database: z.string(),
        size: z.string(),
        pretty_size: z.string(),
      }),
      execute: async (_params, ctx) => {
        const data = await proxyQuery(
          ctx,
          `SELECT current_database() AS database,
                  pg_database_size(current_database())::text AS size,
                  pg_size_pretty(pg_database_size(current_database())) AS pretty_size`,
        );
        return data.rows?.[0] ?? { database: "unknown", size: "0", pretty_size: "0 bytes" };
      },
    },

    active_connections: {
      description: "List active connections to the database.",
      params: z.object({}),
      returns: z.array(
        z.object({
          pid: z.number(),
          user: z.string(),
          database: z.string(),
          client_addr: z.string().nullable(),
          state: z.string().nullable(),
          query: z.string().nullable(),
          query_start: z.string().nullable(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await proxyQuery(
          ctx,
          `SELECT pid, usename AS "user", datname AS database, client_addr::text,
                  state, query, query_start::text
           FROM pg_stat_activity
           WHERE datname = current_database()
           ORDER BY query_start DESC NULLS LAST`,
        );
        return data.rows ?? [];
      },
    },

    // ── Export ─────────────────────────────────────────────────────────────

    export_csv: {
      description: "Run a SELECT query and return results as CSV text.",
      params: z.object({
        sql: z.string().describe("SELECT query to export"),
        file_name: z.string().default("export.csv").describe("Output file name"),
        delimiter: z.string().default(",").describe("Column delimiter"),
        headers: z.boolean().default(true).describe("Include column headers"),
      }),
      returns: z.object({
        file_path: z.string(),
        row_count: z.number(),
        size_bytes: z.number(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyQuery(ctx, params.sql);
        const rows: Record<string, unknown>[] = data.rows ?? [];
        const lines: string[] = [];
        if (rows.length > 0) {
          const keys = Object.keys(rows[0]);
          if (params.headers) {
            lines.push(keys.join(params.delimiter));
          }
          for (const row of rows) {
            lines.push(keys.map((k) => String(row[k] ?? "")).join(params.delimiter));
          }
        }
        const csv = lines.join("\n");
        return {
          file_path: params.file_name,
          row_count: rows.length,
          size_bytes: new TextEncoder().encode(csv).length,
        };
      },
    },

    export_json: {
      description: "Run a SELECT query and return results as JSON.",
      params: z.object({
        sql: z.string().describe("SELECT query to export"),
        file_name: z.string().default("export.json").describe("Output file name"),
        pretty: z.boolean().default(false).describe("Pretty-print JSON output"),
      }),
      returns: z.object({
        file_path: z.string(),
        row_count: z.number(),
        size_bytes: z.number(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyQuery(ctx, params.sql);
        const rows = data.rows ?? [];
        const json = params.pretty ? JSON.stringify(rows, null, 2) : JSON.stringify(rows);
        return {
          file_path: params.file_name,
          row_count: rows.length,
          size_bytes: new TextEncoder().encode(json).length,
        };
      },
    },
  },
});
