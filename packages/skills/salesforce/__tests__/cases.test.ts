import { describe, it, expect } from "bun:test";

describe("cases", () => {
  describe("list_cases", () => {
    it.todo("should build SOQL for Case with status, priority, account_id filters");
  });

  describe("create_case", () => {
    it.todo("should POST to /sobjects/Case with Subject required");
    it.todo("should include priority, status, account_id, contact_id when provided");
  });

  describe("update_case", () => {
    it.todo("should PATCH /sobjects/Case/:id with fields body");
  });

  describe("close_case", () => {
    it.todo("should PATCH /sobjects/Case/:id with Status=Closed");
    it.todo("should include Description when resolution provided");
    it.todo("should return status=Closed");
  });
});
