import { describe, it, expect } from "bun:test";

describe("opportunities", () => {
  describe("list_opportunities", () => {
    it.todo("should build SOQL for Opportunity with stage and account_id filters");
    it.todo("should add CloseDate range clauses when close_date_range provided");
    it.todo("should return records from query response");
  });

  describe("create_opportunity", () => {
    it.todo("should POST to /sobjects/Opportunity with Name, StageName, CloseDate required");
    it.todo("should include Amount and AccountId when provided");
  });

  describe("update_opportunity", () => {
    it.todo("should PATCH /sobjects/Opportunity/:id with fields body");
  });
});
