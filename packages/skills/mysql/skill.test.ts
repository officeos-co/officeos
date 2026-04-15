import { describe, it, expect } from "bun:test";
// Note: skill.ts doesn't exist yet -- these tests define the contract
// Uncomment the import when skill.ts is implemented:
// import skill from "./skill.ts";

describe("mysql", () => {
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
    it.todo("should expose begin_transaction action");
    it.todo("should expose commit action");
    it.todo("should expose rollback action");
    it.todo("should expose table_info action");
    it.todo("should expose database_size action");
    it.todo("should expose active_connections action");
    it.todo("should expose show_processlist action");
    it.todo("should expose export_csv action");
    it.todo("should expose export_json action");
    it.todo("should expose list_users action");
    it.todo("should expose create_user action");
    it.todo("should expose grant_privileges action");
  });

  // ── Param validation ─────────────────────────────────────────────
  describe("params", () => {
    describe("connect", () => {
      it.todo("should require database param");
      it.todo("should require user param");
      it.todo("should require password param");
      it.todo("should accept optional host param with default localhost");
      it.todo("should accept optional port param with default 3306");
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
      it.todo("should accept optional database param");
    });

    describe("list_columns", () => {
      it.todo("should require table param");
      it.todo("should accept optional database param");
    });

    describe("list_indexes", () => {
      it.todo("should require table param");
      it.todo("should accept optional database param");
    });

    describe("create_table", () => {
      it.todo("should require table param");
      it.todo("should require columns param as JSON array string");
      it.todo("should accept optional database param");
      it.todo("should accept optional engine param with default InnoDB");
      it.todo("should accept optional if_not_exists boolean param");
    });

    describe("drop_table", () => {
      it.todo("should require table param");
      it.todo("should accept optional database param");
      it.todo("should accept optional if_exists boolean param");
    });

    describe("alter_table", () => {
      it.todo("should require table param");
      it.todo("should require action param (add_column, drop_column, rename_column)");
      it.todo("should require column_name param");
      it.todo("should require column_type when action is add_column");
      it.todo("should require new_name when action is rename_column");
      it.todo("should accept optional database param");
    });

    describe("insert", () => {
      it.todo("should require table param");
      it.todo("should require data param as JSON object string");
      it.todo("should accept optional database param");
    });

    describe("update", () => {
      it.todo("should require table param");
      it.todo("should require set param as JSON object string");
      it.todo("should require where param");
      it.todo("should accept optional database param");
    });

    describe("delete", () => {
      it.todo("should require table param");
      it.todo("should require where param");
      it.todo("should accept optional database param");
    });

    describe("upsert", () => {
      it.todo("should require table param");
      it.todo("should require data param as JSON object string");
      it.todo("should require update_columns param as JSON array string");
      it.todo("should accept optional database param");
    });

    describe("begin_transaction", () => {
      it.todo("should accept optional isolation_level param with default repeatable_read");
    });

    describe("commit", () => {
      it.todo("should require transaction_id param");
    });

    describe("rollback", () => {
      it.todo("should require transaction_id param");
    });

    describe("table_info", () => {
      it.todo("should require table param");
      it.todo("should accept optional database param");
    });

    describe("database_size", () => {
      it.todo("should accept no required params");
    });

    describe("active_connections", () => {
      it.todo("should accept no required params");
    });

    describe("show_processlist", () => {
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

    describe("list_users", () => {
      it.todo("should accept no required params");
    });

    describe("create_user", () => {
      it.todo("should require username param");
      it.todo("should require password param");
      it.todo("should accept optional host param with default %");
    });

    describe("grant_privileges", () => {
      it.todo("should require username param");
      it.todo("should require database param");
      it.todo("should require privileges param as JSON array string");
      it.todo("should accept optional host param with default %");
      it.todo("should accept optional table param with default *");
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
      it.todo("should return name and size for each database");
    });

    describe("list_tables", () => {
      it.todo("should return table_name, table_type, row_estimate, engine");
      it.todo("should filter by database");
    });

    describe("list_columns", () => {
      it.todo("should return column_name, data_type, is_nullable, column_default, column_key, extra, character_maximum_length");
      it.todo("should filter by table and database");
    });

    describe("list_indexes", () => {
      it.todo("should return index_name, is_unique, columns, index_type");
      it.todo("should filter by table and database");
    });

    describe("create_table", () => {
      it.todo("should create table and return created status and table_name");
      it.todo("should support if_not_exists flag");
      it.todo("should support engine parameter");
      it.todo("should throw on duplicate table without if_not_exists");
    });

    describe("drop_table", () => {
      it.todo("should drop table and return dropped status and table_name");
      it.todo("should support if_exists flag");
      it.todo("should throw on non-existent table without if_exists");
    });

    describe("alter_table", () => {
      it.todo("should add column when action is add_column");
      it.todo("should drop column when action is drop_column");
      it.todo("should rename column when action is rename_column");
      it.todo("should return altered status, table_name, and action");
    });

    describe("insert", () => {
      it.todo("should insert row and return inserted_id and affected_rows");
      it.todo("should throw on constraint violation");
    });

    describe("update", () => {
      it.todo("should update matching rows and return affected_rows and changed_rows");
      it.todo("should require where clause");
    });

    describe("delete", () => {
      it.todo("should delete matching rows and return affected_rows");
      it.todo("should require where clause");
    });

    describe("upsert", () => {
      it.todo("should insert when no duplicate key");
      it.todo("should update specified columns on duplicate key");
      it.todo("should return inserted_id and affected_rows");
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
      it.todo("should return table_name, engine, row_count, data_size, index_size, auto_increment");
    });

    describe("database_size", () => {
      it.todo("should return database, size_bytes, pretty_size");
    });

    describe("active_connections", () => {
      it.todo("should return id, user, host, database, command, time, state, info per connection");
    });

    describe("show_processlist", () => {
      it.todo("should return id, user, host, database, command, time, state, info per process");
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

    describe("list_users", () => {
      it.todo("should return user, host, authentication_string for each user");
    });

    describe("create_user", () => {
      it.todo("should create user and return created status, user, host");
      it.todo("should throw on duplicate user");
    });

    describe("grant_privileges", () => {
      it.todo("should grant privileges and return granted status, user, database, privileges");
      it.todo("should support granting on specific tables");
    });
  });
});
