import { defineSkill, z } from "@harro/skill-sdk";
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
    throw new Error(`MySQL proxy ${res.status}: ${body}`);
  }
  return res.json();
}

export default defineSkill({
  name: "mysql",
  title: "MySQL",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M16.405 5.501c-.115 0-.193.014-.274.033v.013h.014c.054.104.146.18.214.273.054.107.1.214.154.32l.014-.015c.094-.066.14-.172.14-.333-.04-.047-.046-.094-.08-.14-.04-.067-.126-.1-.18-.153zM5.77 18.695h-.927a50.854 50.854 0 00-.27-4.41h-.008l-1.41 4.41H2.45l-1.4-4.41h-.01a72.892 72.892 0 00-.195 4.41H0c.055-1.966.192-3.81.41-5.53h1.15l1.335 4.064h.008l1.347-4.064h1.095c.242 2.015.384 3.86.428 5.53zm4.017-4.08c-.378 2.045-.876 3.533-1.492 4.46-.482.716-1.01 1.073-1.583 1.073-.153 0-.34-.046-.566-.138v-.494c.11.017.24.026.386.026.268 0 .483-.075.647-.222.197-.18.295-.382.295-.605 0-.155-.077-.47-.23-.944L6.23 14.615h.91l.727 2.36c.164.536.233.91.205 1.123.4-1.064.678-2.227.835-3.483zm12.325 4.08h-2.63v-5.53h.885v4.85h1.745zm-3.32.135l-1.016-.5c.09-.076.177-.158.255-.25.433-.506.648-1.258.648-2.253 0-1.83-.718-2.746-2.155-2.746-.704 0-1.254.232-1.65.697-.43.508-.646 1.256-.646 2.245 0 .972.19 1.686.574 2.14.35.41.877.615 1.583.615.264 0 .506-.033.725-.098l1.325.772.36-.622zM15.5 17.588c-.225-.36-.337-.94-.337-1.736 0-1.393.424-2.09 1.27-2.09.443 0 .77.167.977.5.224.362.336.936.336 1.723 0 1.404-.424 2.108-1.27 2.108-.445 0-.77-.167-.978-.5zm-1.658-.425c0 .47-.172.856-.516 1.156-.344.3-.803.45-1.384.45-.543 0-1.064-.172-1.573-.515l.237-.476c.438.22.833.328 1.19.328.332 0 .593-.073.783-.22a.754.754 0 00.3-.615c0-.33-.23-.61-.648-.845-.388-.213-1.163-.657-1.163-.657-.422-.307-.632-.636-.632-1.177 0-.45.157-.81.47-1.085.315-.278.72-.415 1.22-.415.512 0 .98.136 1.4.41l-.213.476a2.726 2.726 0 00-1.064-.23c-.283 0-.502.068-.654.206a.685.685 0 00-.248.524c0 .328.234.61.666.85.393.215 1.187.67 1.187.67.433.305.648.63.648 1.168zm9.382-5.852c-.535-.014-.95.04-1.297.188-.1.04-.26.04-.274.167.055.053.063.14.11.214.08.134.218.313.346.407.14.11.28.216.427.31.26.16.555.255.81.416.145.094.293.213.44.313.073.05.12.14.214.172v-.02c-.046-.06-.06-.147-.105-.214-.067-.067-.134-.127-.2-.193a3.223 3.223 0 00-.695-.675c-.214-.146-.682-.35-.77-.595l-.013-.014c.146-.013.32-.066.46-.106.227-.06.435-.047.67-.106.106-.027.213-.06.32-.094v-.06c-.12-.12-.21-.283-.334-.395a8.867 8.867 0 00-1.104-.823c-.21-.134-.476-.22-.697-.334-.08-.04-.214-.06-.26-.127-.12-.146-.19-.34-.275-.514a17.69 17.69 0 01-.547-1.163c-.12-.262-.193-.523-.34-.763-.69-1.137-1.437-1.826-2.586-2.5-.247-.14-.543-.2-.856-.274-.167-.008-.334-.02-.5-.027-.11-.047-.216-.174-.31-.235-.38-.24-1.364-.76-1.644-.072-.18.434.267.862.422 1.082.115.153.26.328.34.5.047.116.06.235.107.356.106.294.207.622.347.897.073.14.153.287.247.413.054.073.146.107.167.227-.094.136-.1.334-.154.5-.24.757-.146 1.693.194 2.25.107.166.362.534.703.393.3-.12.234-.5.32-.835.02-.08.007-.133.048-.187v.015c.094.188.188.367.274.555.206.328.566.668.867.895.16.12.287.328.487.402v-.02h-.015c-.043-.058-.1-.086-.154-.133a3.445 3.445 0 01-.35-.4 8.76 8.76 0 01-.747-1.218c-.11-.21-.202-.436-.29-.643-.04-.08-.04-.2-.107-.24-.1.146-.247.273-.32.453-.127.288-.14.642-.188 1.01-.027.007-.014 0-.027.014-.214-.052-.287-.274-.367-.46-.2-.475-.233-1.238-.06-1.785.047-.14.247-.582.167-.716-.042-.127-.174-.2-.247-.303a2.478 2.478 0 01-.24-.427c-.16-.374-.24-.788-.414-1.162-.08-.173-.22-.354-.334-.513-.127-.18-.267-.307-.368-.52-.033-.073-.08-.194-.027-.274.014-.054.042-.075.094-.09.088-.072.335.022.422.062.247.1.455.194.662.334.094.066.195.193.315.226h.14c.214.047.455.014.655.073.355.114.675.28.962.46a5.953 5.953 0 012.085 2.286c.08.154.115.295.188.455.14.33.313.663.455.982.14.315.275.636.476.897.1.14.502.213.682.286.133.06.34.115.46.188.23.14.454.3.67.454.11.076.443.243.463.378z\"/></svg>",
  emoji: "\ud83d\udc2c",
  description:
    "Full MySQL database management: connect, query, inspect schemas, manage tables, manipulate data, handle transactions, manage users, and export results via a backend proxy.",
  doc,

  credentials: {
    proxy_url: {
      label: "Proxy URL",
      kind: "text",
      placeholder: "https://api.officeos.co/mysql",
      help: "Backend proxy endpoint for MySQL queries.",
    },
    database: {
      label: "Database",
      kind: "text",
      placeholder: "myapp",
      help: "Default database name.",
    },
    user: {
      label: "User",
      kind: "text",
      placeholder: "root",
      help: "Database username.",
    },
    password: {
      label: "Password",
      kind: "password",
      help: "Database password.",
    },
  },

  actions: {
    // ── Connection ────────────────────────────────────────────────────────

    connect: {
      description: "Test connection to a MySQL database.",
      params: z.object({
        host: z.string().default("localhost").describe("Database server hostname"),
        port: z.number().default(3306).describe("Database server port"),
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
            sql: "SELECT VERSION() AS server_version",
            params: [],
          }),
        });
        if (!res.ok) {
          const body = await res.text();
          throw new Error(`MySQL proxy ${res.status}: ${body}`);
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
          size: z.string(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await proxyQuery(
          ctx,
          `SELECT
             s.SCHEMA_NAME AS name,
             CONCAT(ROUND(SUM(t.DATA_LENGTH + t.INDEX_LENGTH) / 1024 / 1024, 2), ' MB') AS size
           FROM information_schema.SCHEMATA s
           LEFT JOIN information_schema.TABLES t ON t.TABLE_SCHEMA = s.SCHEMA_NAME
           WHERE s.SCHEMA_NAME NOT IN ('information_schema', 'performance_schema', 'mysql', 'sys')
           GROUP BY s.SCHEMA_NAME
           ORDER BY s.SCHEMA_NAME`,
        );
        return data.rows;
      },
    },

    list_tables: {
      description: "List tables and views in a database.",
      params: z.object({
        database: z.string().optional().describe("Database name to inspect (defaults to configured database)"),
      }),
      returns: z.array(
        z.object({
          table_name: z.string(),
          table_type: z.string(),
          row_estimate: z.number(),
          engine: z.string().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const db = params.database ?? ctx.credentials.database;
        const data = await proxyQuery(
          ctx,
          `SELECT TABLE_NAME AS table_name, TABLE_TYPE AS table_type,
                  TABLE_ROWS AS row_estimate, ENGINE AS engine
           FROM information_schema.TABLES
           WHERE TABLE_SCHEMA = ?
           ORDER BY TABLE_NAME`,
          [db],
        );
        return data.rows;
      },
    },

    list_columns: {
      description: "List columns of a table.",
      params: z.object({
        table: z.string().describe("Table name"),
        database: z.string().optional().describe("Database name"),
      }),
      returns: z.array(
        z.object({
          column_name: z.string(),
          data_type: z.string(),
          is_nullable: z.string(),
          column_default: z.string().nullable(),
          column_key: z.string(),
          extra: z.string(),
          character_maximum_length: z.number().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const db = params.database ?? ctx.credentials.database;
        const data = await proxyQuery(
          ctx,
          `SELECT COLUMN_NAME AS column_name, DATA_TYPE AS data_type,
                  IS_NULLABLE AS is_nullable, COLUMN_DEFAULT AS column_default,
                  COLUMN_KEY AS column_key, EXTRA AS extra,
                  CHARACTER_MAXIMUM_LENGTH AS character_maximum_length
           FROM information_schema.COLUMNS
           WHERE TABLE_SCHEMA = ? AND TABLE_NAME = ?
           ORDER BY ORDINAL_POSITION`,
          [db, params.table],
        );
        return data.rows;
      },
    },

    list_indexes: {
      description: "List indexes on a table.",
      params: z.object({
        table: z.string().describe("Table name"),
        database: z.string().optional().describe("Database name"),
      }),
      returns: z.array(
        z.object({
          index_name: z.string(),
          is_unique: z.boolean(),
          columns: z.string(),
          index_type: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const db = params.database ?? ctx.credentials.database;
        const data = await proxyQuery(
          ctx,
          `SELECT INDEX_NAME AS index_name,
                  NOT NON_UNIQUE AS is_unique,
                  GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX SEPARATOR ', ') AS \`columns\`,
                  INDEX_TYPE AS index_type
           FROM information_schema.STATISTICS
           WHERE TABLE_SCHEMA = ? AND TABLE_NAME = ?
           GROUP BY INDEX_NAME, NON_UNIQUE, INDEX_TYPE
           ORDER BY INDEX_NAME`,
          [db, params.table],
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
        database: z.string().optional().describe("Database to create in"),
        engine: z.string().default("InnoDB").describe("Storage engine"),
        if_not_exists: z.boolean().default(false).describe("Skip if table already exists"),
      }),
      returns: z.object({
        created: z.boolean(),
        table_name: z.string(),
      }),
      execute: async (params, ctx) => {
        const db = params.database ?? ctx.credentials.database;
        const cols: { name: string; type: string }[] = JSON.parse(params.columns);
        const colDefs = cols.map((c) => `\`${c.name}\` ${c.type}`).join(", ");
        const ifne = params.if_not_exists ? "IF NOT EXISTS " : "";
        await proxyQuery(ctx, `CREATE TABLE ${ifne}\`${db}\`.\`${params.table}\` (${colDefs}) ENGINE=${params.engine}`);
        return { created: true, table_name: `${db}.${params.table}` };
      },
    },

    drop_table: {
      description: "Drop a table.",
      params: z.object({
        table: z.string().describe("Table to drop"),
        database: z.string().optional().describe("Database containing the table"),
        if_exists: z.boolean().default(false).describe("Skip if table does not exist"),
      }),
      returns: z.object({
        dropped: z.boolean(),
        table_name: z.string(),
      }),
      execute: async (params, ctx) => {
        const db = params.database ?? ctx.credentials.database;
        const ife = params.if_exists ? "IF EXISTS " : "";
        await proxyQuery(ctx, `DROP TABLE ${ife}\`${db}\`.\`${params.table}\``);
        return { dropped: true, table_name: `${db}.${params.table}` };
      },
    },

    alter_table: {
      description: "Alter a table: add, drop, or rename a column.",
      params: z.object({
        table: z.string().describe("Table to alter"),
        database: z.string().optional().describe("Database containing the table"),
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
        const db = params.database ?? ctx.credentials.database;
        const tbl = `\`${db}\`.\`${params.table}\``;
        let sql: string;
        switch (params.action) {
          case "add_column":
            if (!params.column_type) throw new Error("column_type is required for add_column");
            sql = `ALTER TABLE ${tbl} ADD COLUMN \`${params.column_name}\` ${params.column_type}`;
            break;
          case "drop_column":
            sql = `ALTER TABLE ${tbl} DROP COLUMN \`${params.column_name}\``;
            break;
          case "rename_column":
            if (!params.new_name) throw new Error("new_name is required for rename_column");
            sql = `ALTER TABLE ${tbl} RENAME COLUMN \`${params.column_name}\` TO \`${params.new_name}\``;
            break;
        }
        await proxyQuery(ctx, sql);
        return { altered: true, table_name: `${db}.${params.table}`, action: params.action };
      },
    },

    // ── Data operations ───────────────────────────────────────────────────

    insert: {
      description: "Insert a row into a table.",
      params: z.object({
        table: z.string().describe("Target table"),
        database: z.string().optional().describe("Database containing the table"),
        data: z.string().describe("JSON object of column-value pairs"),
      }),
      returns: z.object({
        inserted_id: z.number(),
        affected_rows: z.number(),
      }),
      execute: async (params, ctx) => {
        const db = params.database ?? ctx.credentials.database;
        const obj = JSON.parse(params.data);
        const keys = Object.keys(obj);
        const cols = keys.map((k) => `\`${k}\``).join(", ");
        const placeholders = keys.map(() => "?").join(", ");
        const vals = keys.map((k) => obj[k]);
        const data = await proxyQuery(
          ctx,
          `INSERT INTO \`${db}\`.\`${params.table}\` (${cols}) VALUES (${placeholders})`,
          vals,
        );
        return {
          inserted_id: data.inserted_id ?? 0,
          affected_rows: data.affected_rows ?? 1,
        };
      },
    },

    update: {
      description: "Update rows in a table.",
      params: z.object({
        table: z.string().describe("Target table"),
        database: z.string().optional().describe("Database containing the table"),
        set: z.string().describe("JSON object of columns to update"),
        where: z.string().describe("WHERE clause (without the WHERE keyword)"),
      }),
      returns: z.object({
        affected_rows: z.number(),
        changed_rows: z.number(),
      }),
      execute: async (params, ctx) => {
        const db = params.database ?? ctx.credentials.database;
        const obj = JSON.parse(params.set);
        const keys = Object.keys(obj);
        const setClauses = keys.map((k) => `\`${k}\` = ?`).join(", ");
        const vals = keys.map((k) => obj[k]);
        const data = await proxyQuery(
          ctx,
          `UPDATE \`${db}\`.\`${params.table}\` SET ${setClauses} WHERE ${params.where}`,
          vals,
        );
        return {
          affected_rows: data.affected_rows ?? 0,
          changed_rows: data.changed_rows ?? 0,
        };
      },
    },

    delete: {
      description: "Delete rows from a table.",
      params: z.object({
        table: z.string().describe("Target table"),
        database: z.string().optional().describe("Database containing the table"),
        where: z.string().describe("WHERE clause (without the WHERE keyword)"),
      }),
      returns: z.object({
        affected_rows: z.number(),
      }),
      execute: async (params, ctx) => {
        const db = params.database ?? ctx.credentials.database;
        const data = await proxyQuery(
          ctx,
          `DELETE FROM \`${db}\`.\`${params.table}\` WHERE ${params.where}`,
        );
        return {
          affected_rows: data.affected_rows ?? 0,
        };
      },
    },

    upsert: {
      description: "Insert a row or update on duplicate key (upsert).",
      params: z.object({
        table: z.string().describe("Target table"),
        database: z.string().optional().describe("Database containing the table"),
        data: z.string().describe("JSON object of column-value pairs"),
        update_columns: z.string().describe("JSON array of columns to update on duplicate key"),
      }),
      returns: z.object({
        inserted_id: z.number(),
        affected_rows: z.number(),
      }),
      execute: async (params, ctx) => {
        const db = params.database ?? ctx.credentials.database;
        const obj = JSON.parse(params.data);
        const updateCols: string[] = JSON.parse(params.update_columns);
        const keys = Object.keys(obj);
        const cols = keys.map((k) => `\`${k}\``).join(", ");
        const placeholders = keys.map(() => "?").join(", ");
        const vals = keys.map((k) => obj[k]);
        const updates = updateCols.map((c) => `\`${c}\` = VALUES(\`${c}\`)`).join(", ");
        const data = await proxyQuery(
          ctx,
          `INSERT INTO \`${db}\`.\`${params.table}\` (${cols}) VALUES (${placeholders})
           ON DUPLICATE KEY UPDATE ${updates}`,
          vals,
        );
        return {
          inserted_id: data.inserted_id ?? 0,
          affected_rows: data.affected_rows ?? 1,
        };
      },
    },

    // ── Transactions ──────────────────────────────────────────────────────

    begin_transaction: {
      description: "Begin a new transaction.",
      params: z.object({
        isolation_level: z
          .enum(["read_uncommitted", "read_committed", "repeatable_read", "serializable"])
          .default("repeatable_read")
          .describe("Transaction isolation level"),
      }),
      returns: z.object({ transaction_id: z.string() }),
      execute: async (params, ctx) => {
        const level = params.isolation_level.replace(/_/g, " ").toUpperCase();
        await proxyQuery(ctx, `SET TRANSACTION ISOLATION LEVEL ${level}`);
        const data = await proxyQuery(ctx, "START TRANSACTION");
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
        if (!res.ok) throw new Error(`MySQL proxy ${res.status}: ${await res.text()}`);
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
        if (!res.ok) throw new Error(`MySQL proxy ${res.status}: ${await res.text()}`);
        return { rolled_back: true, transaction_id: params.transaction_id };
      },
    },

    // ── Server info ───────────────────────────────────────────────────────

    table_info: {
      description: "Get size and row count information for a table.",
      params: z.object({
        table: z.string().describe("Table name"),
        database: z.string().optional().describe("Database name"),
      }),
      returns: z.object({
        table_name: z.string(),
        engine: z.string(),
        row_count: z.number(),
        data_size: z.string(),
        index_size: z.string(),
        auto_increment: z.number().nullable(),
      }),
      execute: async (params, ctx) => {
        const db = params.database ?? ctx.credentials.database;
        const data = await proxyQuery(
          ctx,
          `SELECT TABLE_NAME AS table_name, ENGINE AS engine,
                  TABLE_ROWS AS row_count,
                  CONCAT(ROUND(DATA_LENGTH / 1024 / 1024, 2), ' MB') AS data_size,
                  CONCAT(ROUND(INDEX_LENGTH / 1024 / 1024, 2), ' MB') AS index_size,
                  AUTO_INCREMENT AS auto_increment
           FROM information_schema.TABLES
           WHERE TABLE_SCHEMA = ? AND TABLE_NAME = ?`,
          [db, params.table],
        );
        return data.rows?.[0] ?? {
          table_name: `${db}.${params.table}`,
          engine: "unknown",
          row_count: 0,
          data_size: "0 MB",
          index_size: "0 MB",
          auto_increment: null,
        };
      },
    },

    database_size: {
      description: "Get the size of the current database.",
      params: z.object({}),
      returns: z.object({
        database: z.string(),
        size_bytes: z.string(),
        pretty_size: z.string(),
      }),
      execute: async (_params, ctx) => {
        const data = await proxyQuery(
          ctx,
          `SELECT TABLE_SCHEMA AS \`database\`,
                  SUM(DATA_LENGTH + INDEX_LENGTH) AS size_bytes,
                  CONCAT(ROUND(SUM(DATA_LENGTH + INDEX_LENGTH) / 1024 / 1024, 2), ' MB') AS pretty_size
           FROM information_schema.TABLES
           WHERE TABLE_SCHEMA = ?
           GROUP BY TABLE_SCHEMA`,
          [ctx.credentials.database],
        );
        return data.rows?.[0] ?? { database: ctx.credentials.database, size_bytes: "0", pretty_size: "0 MB" };
      },
    },

    active_connections: {
      description: "List active connections to the database.",
      params: z.object({}),
      returns: z.array(
        z.object({
          id: z.number(),
          user: z.string(),
          host: z.string(),
          database: z.string().nullable(),
          command: z.string(),
          time: z.number(),
          state: z.string().nullable(),
          info: z.string().nullable(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await proxyQuery(
          ctx,
          `SELECT ID AS id, USER AS user, HOST AS host, DB AS \`database\`,
                  COMMAND AS command, TIME AS time, STATE AS state, INFO AS info
           FROM information_schema.PROCESSLIST
           WHERE DB = ?
           ORDER BY TIME DESC`,
          [ctx.credentials.database],
        );
        return data.rows ?? [];
      },
    },

    show_processlist: {
      description: "Show all running processes on the MySQL server.",
      params: z.object({}),
      returns: z.array(
        z.object({
          id: z.number(),
          user: z.string(),
          host: z.string(),
          database: z.string().nullable(),
          command: z.string(),
          time: z.number(),
          state: z.string().nullable(),
          info: z.string().nullable(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await proxyQuery(
          ctx,
          `SELECT ID AS id, USER AS user, HOST AS host, DB AS \`database\`,
                  COMMAND AS command, TIME AS time, STATE AS state, INFO AS info
           FROM information_schema.PROCESSLIST
           ORDER BY TIME DESC`,
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

    // ── User management ───────────────────────────────────────────────────

    list_users: {
      description: "List all MySQL users.",
      params: z.object({}),
      returns: z.array(
        z.object({
          user: z.string(),
          host: z.string(),
          authentication_string: z.string(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await proxyQuery(
          ctx,
          `SELECT User AS user, Host AS host,
                  IFNULL(authentication_string, '') AS authentication_string
           FROM mysql.user
           ORDER BY User, Host`,
        );
        return data.rows ?? [];
      },
    },

    create_user: {
      description: "Create a new MySQL user.",
      params: z.object({
        username: z.string().describe("New username"),
        password: z.string().describe("User password"),
        host: z.string().default("%").describe("Allowed connection host"),
      }),
      returns: z.object({
        created: z.boolean(),
        user: z.string(),
        host: z.string(),
      }),
      execute: async (params, ctx) => {
        await proxyQuery(
          ctx,
          `CREATE USER ?@? IDENTIFIED BY ?`,
          [params.username, params.host, params.password],
        );
        return { created: true, user: params.username, host: params.host };
      },
    },

    grant_privileges: {
      description: "Grant privileges to a MySQL user.",
      params: z.object({
        username: z.string().describe("Username to grant to"),
        database: z.string().describe("Database to grant on (use * for all)"),
        privileges: z.string().describe("JSON array of privileges (e.g. '[\"SELECT\",\"INSERT\"]')"),
        host: z.string().default("%").describe("User host"),
        table: z.string().default("*").describe("Table to grant on (use * for all)"),
      }),
      returns: z.object({
        granted: z.boolean(),
        user: z.string(),
        database: z.string(),
        privileges: z.array(z.string()),
      }),
      execute: async (params, ctx) => {
        const privs: string[] = JSON.parse(params.privileges);
        const privList = privs.join(", ");
        const target =
          params.database === "*"
            ? `*.*`
            : params.table === "*"
              ? `\`${params.database}\`.*`
              : `\`${params.database}\`.\`${params.table}\``;
        await proxyQuery(
          ctx,
          `GRANT ${privList} ON ${target} TO ?@?`,
          [params.username, params.host],
        );
        await proxyQuery(ctx, "FLUSH PRIVILEGES");
        return {
          granted: true,
          user: params.username,
          database: params.database,
          privileges: privs,
        };
      },
    },
  },
});
