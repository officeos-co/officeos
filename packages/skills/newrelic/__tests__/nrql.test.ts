import { describe, it } from "bun:test";

describe("nrql", () => {
  describe("nrql (instant)", () => {
    it.todo("should POST to NerdGraph with NRQL query and account ID");
    it.todo("should pass timeout variable to GraphQL");
    it.todo("should return results and metadata from response");
    it.todo("should throw when NerdGraph returns errors array");
    it.todo("should parse account_id string to integer");
  });

  describe("nrql_async", () => {
    it.todo("should POST with async: true in NRQL query");
    it.todo("should return results array");
  });
});
