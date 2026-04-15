import { describe, it } from "bun:test";

describe("databases", () => {
  describe("list_databases", () => {
    it.todo("should GET /databases with per_page");
    it.todo("should return connection_uri from nested connection.uri");
  });

  describe("get_database", () => {
    it.todo("should GET /databases/:id");
    it.todo("should map users with default role 'normal'");
  });

  describe("create_database", () => {
    it.todo("should POST with engine, region, size");
    it.todo("should include version when provided");
    it.todo("should return status and connection_uri");
  });

  describe("delete_database", () => {
    it.todo("should DELETE /databases/:id");
    it.todo("should return deleted: true");
  });

  describe("list_connection_pools", () => {
    it.todo("should GET /databases/:id/pools");
    it.todo("should return connection_uri from pool.connection.uri");
  });

  describe("create_connection_pool", () => {
    it.todo("should POST pool with name, mode, size, db, user");
  });

  describe("list_db_users", () => {
    it.todo("should GET /databases/:id/users");
    it.todo("should default role to normal when absent");
  });
});
