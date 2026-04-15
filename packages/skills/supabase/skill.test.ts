import { describe, it, expect } from "bun:test";
// Note: skill.ts doesn't exist yet -- these tests define the contract
// Uncomment the import when skill.ts is implemented:
// import skill from "./skill.ts";

describe("supabase", () => {
  // ── Action registry ──────────────────────────────────────────────
  describe("actions", () => {
    // Database
    it.todo("should expose query action");
    it.todo("should expose list_tables action");
    it.todo("should expose get_table action");
    // CRUD (PostgREST)
    it.todo("should expose select action");
    it.todo("should expose insert action");
    it.todo("should expose update action");
    it.todo("should expose delete action");
    it.todo("should expose upsert action");
    // Auth
    it.todo("should expose list_users action");
    it.todo("should expose get_user action");
    it.todo("should expose create_user action");
    it.todo("should expose delete_user action");
    it.todo("should expose invite_user action");
    it.todo("should expose update_user action");
    // Storage
    it.todo("should expose list_buckets action");
    it.todo("should expose create_bucket action");
    it.todo("should expose delete_bucket action");
    it.todo("should expose list_files action");
    it.todo("should expose upload_file action");
    it.todo("should expose download_file action");
    it.todo("should expose delete_file action");
    it.todo("should expose get_public_url action");
    it.todo("should expose create_signed_url action");
    // Edge Functions
    it.todo("should expose list_functions action");
    it.todo("should expose get_function action");
    it.todo("should expose invoke_function action");
    // Realtime
    it.todo("should expose list_channels action");
    // Vector / Embeddings
    it.todo("should expose vector_search action");
    // RPC
    it.todo("should expose rpc action");
  });

  // ── Param validation ─────────────────────────────────────────────
  describe("params", () => {
    describe("query", () => {
      it.todo("should require sql param");
    });

    describe("list_tables", () => {
      it.todo("should accept no required params");
    });

    describe("get_table", () => {
      it.todo("should require table param");
    });

    describe("select", () => {
      it.todo("should require table param");
      it.todo("should accept optional columns param with default *");
      it.todo("should accept optional filter param");
      it.todo("should accept optional order param");
      it.todo("should accept optional limit param with default 100");
      it.todo("should accept optional offset param with default 0");
    });

    describe("insert", () => {
      it.todo("should require table param");
      it.todo("should require records param as JSON array");
    });

    describe("update", () => {
      it.todo("should require table param");
      it.todo("should require filter param");
      it.todo("should require data param as JSON object");
    });

    describe("delete", () => {
      it.todo("should require table param");
      it.todo("should require filter param");
    });

    describe("upsert", () => {
      it.todo("should require table param");
      it.todo("should require records param as JSON array");
      it.todo("should accept optional on_conflict param");
    });

    describe("list_users", () => {
      it.todo("should accept optional page param with default 1");
      it.todo("should accept optional per_page param with default 50");
    });

    describe("get_user", () => {
      it.todo("should require user_id param");
    });

    describe("create_user", () => {
      it.todo("should require email param");
      it.todo("should require password param");
      it.todo("should accept optional email_confirm boolean param");
      it.todo("should accept optional user_metadata param as JSON object");
    });

    describe("delete_user", () => {
      it.todo("should require user_id param");
    });

    describe("invite_user", () => {
      it.todo("should require email param");
    });

    describe("update_user", () => {
      it.todo("should require user_id param");
      it.todo("should accept optional email param");
      it.todo("should accept optional password param");
      it.todo("should accept optional user_metadata param as JSON object");
    });

    describe("list_buckets", () => {
      it.todo("should accept no required params");
    });

    describe("create_bucket", () => {
      it.todo("should require name param");
      it.todo("should accept optional public boolean param with default false");
    });

    describe("delete_bucket", () => {
      it.todo("should require id param");
    });

    describe("list_files", () => {
      it.todo("should require bucket param");
      it.todo("should accept optional path param with default empty string");
      it.todo("should accept optional limit param with default 100");
      it.todo("should accept optional offset param with default 0");
    });

    describe("upload_file", () => {
      it.todo("should require bucket param");
      it.todo("should require path param");
      it.todo("should require content param (base64)");
      it.todo("should accept optional content_type param");
      it.todo("should accept optional upsert boolean param");
    });

    describe("download_file", () => {
      it.todo("should require bucket param");
      it.todo("should require path param");
    });

    describe("delete_file", () => {
      it.todo("should require bucket param");
      it.todo("should require paths param as JSON array");
    });

    describe("get_public_url", () => {
      it.todo("should require bucket param");
      it.todo("should require path param");
    });

    describe("create_signed_url", () => {
      it.todo("should require bucket param");
      it.todo("should require path param");
      it.todo("should accept optional expires_in param with default 3600");
    });

    describe("list_functions", () => {
      it.todo("should accept no required params");
    });

    describe("get_function", () => {
      it.todo("should require slug param");
    });

    describe("invoke_function", () => {
      it.todo("should require slug param");
      it.todo("should accept optional body param as JSON");
    });

    describe("list_channels", () => {
      it.todo("should accept no required params");
    });

    describe("vector_search", () => {
      it.todo("should require table param");
      it.todo("should require query_embedding param as JSON array");
      it.todo("should accept optional match_count param with default 10");
      it.todo("should accept optional match_threshold param with default 0.5");
    });

    describe("rpc", () => {
      it.todo("should require function_name param");
      it.todo("should accept optional params param as JSON object");
    });
  });

  // ── Execute behavior ─────────────────────────────────────────────
  describe("execute", () => {
    describe("query", () => {
      it.todo("should execute SQL and return result rows");
      it.todo("should throw on invalid SQL");
    });

    describe("list_tables", () => {
      it.todo("should return array of tables with table_name, row_count, size");
    });

    describe("get_table", () => {
      it.todo("should return columns with column_name, data_type, is_nullable, column_default");
      it.todo("should throw for non-existent table");
    });

    describe("select", () => {
      it.todo("should return matching rows as JSON array");
      it.todo("should apply PostgREST filter");
      it.todo("should apply ordering");
      it.todo("should respect limit and offset");
      it.todo("should select specific columns");
    });

    describe("insert", () => {
      it.todo("should insert records and return inserted rows");
      it.todo("should support inserting multiple records");
    });

    describe("update", () => {
      it.todo("should update matching rows and return updated rows");
      it.todo("should require filter to prevent full-table update");
    });

    describe("delete", () => {
      it.todo("should delete matching rows and return deleted rows");
      it.todo("should require filter to prevent full-table delete");
    });

    describe("upsert", () => {
      it.todo("should upsert records and return result rows");
      it.todo("should use on_conflict columns for conflict resolution");
    });

    describe("list_users", () => {
      it.todo("should return paginated user list");
      it.todo("should include id, email, created_at for each user");
    });

    describe("get_user", () => {
      it.todo("should return user details by UUID");
      it.todo("should throw for non-existent user");
    });

    describe("create_user", () => {
      it.todo("should create user and return user object");
      it.todo("should auto-confirm email when email_confirm is true");
      it.todo("should attach user_metadata when provided");
    });

    describe("delete_user", () => {
      it.todo("should delete user and return success");
      it.todo("should throw for non-existent user");
    });

    describe("invite_user", () => {
      it.todo("should send invite and return user object");
    });

    describe("update_user", () => {
      it.todo("should update user fields and return updated user");
      it.todo("should merge user_metadata when provided");
    });

    describe("list_buckets", () => {
      it.todo("should return array of buckets with id, name, public, created_at");
    });

    describe("create_bucket", () => {
      it.todo("should create bucket and return name");
      it.todo("should create public bucket when public is true");
    });

    describe("delete_bucket", () => {
      it.todo("should delete bucket and return success");
      it.todo("should throw for non-existent bucket");
    });

    describe("list_files", () => {
      it.todo("should return files in bucket at given path");
      it.todo("should respect limit and offset");
    });

    describe("upload_file", () => {
      it.todo("should upload base64 content and return key");
      it.todo("should set content type when provided");
      it.todo("should overwrite when upsert is true");
    });

    describe("download_file", () => {
      it.todo("should return base64-encoded file content");
      it.todo("should return content_type");
      it.todo("should throw for non-existent file");
    });

    describe("delete_file", () => {
      it.todo("should delete files and return deleted paths");
    });

    describe("get_public_url", () => {
      it.todo("should return constructed public URL");
      it.todo("should only work for public buckets");
    });

    describe("create_signed_url", () => {
      it.todo("should return signed URL with expiry");
      it.todo("should respect custom expires_in");
    });

    describe("list_functions", () => {
      it.todo("should return array of deployed functions");
    });

    describe("get_function", () => {
      it.todo("should return function details by slug");
      it.todo("should throw for non-existent function");
    });

    describe("invoke_function", () => {
      it.todo("should invoke function and return response");
      it.todo("should pass body to function when provided");
    });

    describe("list_channels", () => {
      it.todo("should return active realtime channels");
    });

    describe("vector_search", () => {
      it.todo("should call match RPC and return matching rows");
      it.todo("should respect match_count");
      it.todo("should filter by match_threshold");
    });

    describe("rpc", () => {
      it.todo("should invoke Postgres function and return result");
      it.todo("should pass params to function");
      it.todo("should throw for non-existent function");
    });
  });
});
