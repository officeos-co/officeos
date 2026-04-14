import { describe, it, expect } from "bun:test";
// Note: skill.ts doesn't exist yet -- these tests define the contract
// Uncomment the import when skill.ts is implemented:
// import skill from "./skill.ts";

describe("postgres", () => {
  // ── Action registry ──────────────────────────────────────────────
  describe("actions", () => {
    it.todo("should expose connect action");
    it.todo("should expose query action");
    it.todo("should expose query_one action");
    it.todo("should expose list_databases action");
    it.todo("should expose list_tables action");
    it.todo("should expose list_columns action");
    it.todo("should expose list_indexes action");
    it.todo("should expose create_table action");
    it.todo("should expose drop_table action");
    it.todo("should expose alter_table action");
    it.todo("should expose insert action");
    it.todo("should expose update action");
    it.todo("should expose delete action");
    it.todo("should expose upsert action");
    it.todo("should expose run_migration action");
    it.todo("should expose list_migrations action");
    it.todo("should expose begin_transaction action");
    it.todo("should expose commit action");
    it.todo("should expose rollback action");
    it.todo("should expose table_info action");
    it.todo("should expose database_size action");
    it.todo("should expose active_connections action");
    it.todo("should expose export_csv action");
    it.todo("should expose export_json action");
  });

  // ── Param validation ─────────────────────────────────────────────
  describe("params", () => {
    describe("connect", () => {
      it.todo("should require database param");
      it.todo("should require user param");
      it.todo("should require password param");
      it.todo("should accept optional host param with default localhost");
      it.todo("should accept optional port param with default 5432");
      it.todo("should accept optional ssl boolean param");
    });

    describe("query", () => {
      it.todo("should require sql param");
      it.todo("should accept optional params as JSON array string");
    });

    describe("query_one", () => {
      it.todo("should require sql param");
      it.todo("should accept optional params as JSON array string");
    });

    describe("list_databases", () => {
      it.todo("should accept no required params");
    });

    describe("list_tables", () => {
      it.todo("should accept optional schema param with default public");
    });

    describe("list_columns", () => {
      it.todo("should require table param");
      it.todo("should accept optional schema param with default public");
    });

    describe("list_indexes", () => {
      it.todo("should require table param");
      it.todo("should accept optional schema param with default public");
    });

    describe("create_table", () => {
      it.todo("should require table param");
      it.todo("should require columns param as JSON array string");
      it.todo("should accept optional schema param with default public");
      it.todo("should accept optional if_not_exists boolean param");
    });

    describe("drop_table", () => {
      it.todo("should require table param");
      it.todo("should accept optional schema param with default public");
      it.todo("should accept optional cascade boolean param");
    });

    describe("alter_table", () => {
      it.todo("should require table param");
      it.todo("should require action param (add_column, drop_column, rename_column)");
      it.todo("should require column_name param");
      it.todo("should require column_type when action is add_column");
      it.todo("should require new_name when action is rename_column");
      it.todo("should accept optional schema param with default public");
    });

    describe("insert", () => {
      it.todo("should require table param");
      it.todo("should require data param as JSON object string");
      it.todo("should accept optional schema param with default public");
      it.todo("should accept optional returning param with default *");
    });

    describe("update", () => {
      it.todo("should require table param");
      it.todo("should require set param as JSON object string");
      it.todo("should require where param");
      it.todo("should accept optional schema param with default public");
      it.todo("should accept optional returning param");
    });

    describe("delete", () => {
      it.todo("should require table param");
      it.todo("should require where param");
      it.todo("should accept optional schema param with default public");
      it.todo("should accept optional returning param");
    });

    describe("upsert", () => {
      it.todo("should require table param");
      it.todo("should require data param as JSON object string");
      it.todo("should require conflict_columns param as JSON array string");
      it.todo("should require update_columns param as JSON array string");
      it.todo("should accept optional schema param with default public");
      it.todo("should accept optional returning param with default *");
    });

    describe("run_migration", () => {
      it.todo("should require sql param");
      it.todo("should require name param");
    });

    describe("list_migrations", () => {
      it.todo("should accept no required params");
    });

    describe("begin_transaction", () => {
      it.todo("should accept optional isolation_level param with default read_committed");
    });

    describe("commit", () => {
      it.todo("should require transaction_id param");
    });

    describe("rollback", () => {
      it.todo("should require transaction_id param");
    });

    describe("table_info", () => {
      it.todo("should require table param");
      it.todo("should accept optional schema param with default public");
    });

    describe("database_size", () => {
      it.todo("should accept no required params");
    });

    describe("active_connections", () => {
      it.todo("should accept no required params");
    });

    describe("export_csv", () => {
      it.todo("should require sql param");
      it.todo("should accept optional file_name param with default export.csv");
      it.todo("should accept optional delimiter param with default comma");
      it.todo("should accept optional headers boolean param with default true");
    });

    describe("export_json", () => {
      it.todo("should require sql param");
      it.todo("should accept optional file_name param with default export.json");
      it.todo("should accept optional pretty boolean param with default false");
    });
  });

  // ── Execute behavior ─────────────────────────────────────────────
  describe("execute", () => {
    describe("connect", () => {
      it.todo("should establish connection to database");
      it.todo("should return connected status, server_version, database, and user");
      it.todo("should support SSL connections");
      it.todo("should throw on invalid credentials");
    });

    describe("query", () => {
      it.todo("should execute SQL and return rows array");
      it.todo("should return row_count and fields metadata");
      it.todo("should support parameterized queries via params");
      it.todo("should throw on syntax error");
    });

    describe("query_one", () => {
      it.todo("should return single row object");
      it.todo("should return null when no rows match");
      it.todo("should support parameterized queries via params");
    });

    describe("list_databases", () => {
      it.todo("should return name, owner, encoding, size for each database");
    });

    describe("list_tables", () => {
      it.todo("should return table_name, table_type, row_estimate");
      it.todo("should filter by schema");
    });

    describe("list_columns", () => {
      it.todo("should return column_name, data_type, is_nullable, column_default, character_maximum_length");
      it.todo("should filter by table and schema");
    });

    describe("list_indexes", () => {
      it.todo("should return index_name, is_unique, is_primary, columns, index_type");
      it.todo("should filter by table and schema");
    });

    describe("create_table", () => {
      it.todo("should create table and return created status and table_name");
      it.todo("should support if_not_exists flag");
      it.todo("should throw on duplicate table without if_not_exists");
    });

    describe("drop_table", () => {
      it.todo("should drop table and return dropped status and table_name");
      it.todo("should support cascade flag");
      it.todo("should throw on non-existent table");
    });

    describe("alter_table", () => {
      it.todo("should add column when action is add_column");
      it.todo("should drop column when action is drop_column");
      it.todo("should rename column when action is rename_column");
      it.todo("should return altered status, table_name, and action");
    });

    describe("insert", () => {
      it.todo("should insert row and return the inserted row");
      it.todo("should respect returning clause");
      it.todo("should throw on constraint violation");
    });

    describe("update", () => {
      it.todo("should update matching rows and return updated_count");
      it.todo("should return rows when returning is set");
      it.todo("should require where clause");
    });

    describe("delete", () => {
      it.todo("should delete matching rows and return deleted_count");
      it.todo("should return rows when returning is set");
      it.todo("should require where clause");
    });

    describe("upsert", () => {
      it.todo("should insert when no conflict");
      it.todo("should update specified columns on conflict");
      it.todo("should return the upserted row");
    });

    describe("run_migration", () => {
      it.todo("should apply migration and return applied status, migration_name, duration_ms");
      it.todo("should reject already-applied migration name");
    });

    describe("list_migrations", () => {
      it.todo("should return name, applied_at, duration_ms for each migration");
    });

    describe("begin_transaction", () => {
      it.todo("should return transaction_id");
      it.todo("should accept isolation_level parameter");
    });

    describe("commit", () => {
      it.todo("should commit transaction and return committed status");
      it.todo("should throw on invalid transaction_id");
    });

    describe("rollback", () => {
      it.todo("should rollback transaction and return rolled_back status");
      it.todo("should throw on invalid transaction_id");
    });

    describe("table_info", () => {
      it.todo("should return table_name, row_count, total_size, index_size, toast_size, table_size");
    });

    describe("database_size", () => {
      it.todo("should return database, size, pretty_size");
    });

    describe("active_connections", () => {
      it.todo("should return pid, user, database, client_addr, state, query, query_start per connection");
    });

    describe("export_csv", () => {
      it.todo("should return file_path, row_count, size_bytes");
      it.todo("should support custom delimiter");
      it.todo("should support toggling headers");
    });

    describe("export_json", () => {
      it.todo("should return file_path, row_count, size_bytes");
      it.todo("should support pretty-print option");
    });
  });
});
