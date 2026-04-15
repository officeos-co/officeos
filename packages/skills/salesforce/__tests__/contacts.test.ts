import { describe, it, expect } from "bun:test";

describe("contacts (salesforce)", () => {
  describe("list_contacts", () => {
    it.todo("should build SOQL for Contact with WHERE AccountId clause when account_id provided");
    it.todo("should return records from query response");
  });

  describe("create_contact", () => {
    it.todo("should POST to /sobjects/Contact with LastName required");
    it.todo("should map first_name to FirstName field");
  });

  describe("update_contact", () => {
    it.todo("should PATCH /sobjects/Contact/:id with fields body");
  });
});
