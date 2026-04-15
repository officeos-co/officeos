import { describe, it, expect } from "bun:test";

describe("leads", () => {
  describe("list_leads", () => {
    it.todo("should build SOQL for Lead with WHERE clause when filters provided");
    it.todo("should return records from query response");
  });

  describe("create_lead", () => {
    it.todo("should POST to /sobjects/Lead with LastName and Company required");
    it.todo("should merge additional fields into body");
  });

  describe("convert_lead", () => {
    it.todo("should POST to /actions/standard/convertLead with inputs array");
    it.todo("should include opportunityName when provided");
    it.todo("should set doNotCreateOpportunity=true when flag is set");
    it.todo("should extract accountId and contactId from response outputValues");
  });
});
