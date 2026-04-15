import { describe, it, expect } from "bun:test";

describe("accounts", () => {
  describe("list_accounts", () => {
    it.todo("should build SOQL for Account with type and owner filters");
    it.todo("should return records from query response");
  });

  describe("create_account", () => {
    it.todo("should POST to /sobjects/Account with Name required");
    it.todo("should include type, industry, website, phone when provided");
  });

  describe("update_account", () => {
    it.todo("should PATCH /sobjects/Account/:id with fields body");
  });
});
