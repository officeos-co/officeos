import { describe, it, expect } from "bun:test";

describe("customers", () => {
  describe("list_customers", () => {
    it.todo("should call /v1/customers with limit param");
    it.todo("should filter by email when provided");
    it.todo("should apply created date filters using bracket notation");
    it.todo("should return mapped customer array with nullable fields");
    it.todo("should throw on non-ok response");
  });

  describe("get_customer", () => {
    it.todo("should call /v1/customers/:id");
    it.todo("should return full customer object with subscriptions");
    it.todo("should handle missing optional fields with null defaults");
  });

  describe("create_customer", () => {
    it.todo("should POST to /v1/customers with provided fields");
    it.todo("should flatten metadata into bracket notation");
    it.todo("should omit undefined optional fields from body");
  });

  describe("update_customer", () => {
    it.todo("should POST to /v1/customers/:id with update fields");
  });

  describe("delete_customer", () => {
    it.todo("should DELETE /v1/customers/:id");
    it.todo("should return id and deleted flag");
  });

  describe("search_customers", () => {
    it.todo("should GET /v1/customers/search with query param");
    it.todo("should pass optional limit");
  });
});
