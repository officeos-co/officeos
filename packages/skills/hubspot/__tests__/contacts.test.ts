import { describe, it, expect } from "bun:test";

describe("contacts", () => {
  describe("list_contacts", () => {
    it.todo("should call /crm/v3/objects/contacts with limit param");
    it.todo("should pass properties as comma-separated string");
    it.todo("should return mapped contact array");
  });

  describe("get_contact", () => {
    it.todo("should call /crm/v3/objects/contacts/:id");
    it.todo("should include associations when present");
  });

  describe("create_contact", () => {
    it.todo("should POST to /crm/v3/objects/contacts with properties object");
    it.todo("should merge extra properties into props");
  });

  describe("update_contact", () => {
    it.todo("should PATCH /crm/v3/objects/contacts/:id");
  });

  describe("delete_contact", () => {
    it.todo("should DELETE /crm/v3/objects/contacts/:id");
    it.todo("should return success: true");
  });

  describe("search_contacts", () => {
    it.todo("should POST to /crm/v3/objects/contacts/search");
    it.todo("should include filterGroups when filter_groups provided");
    it.todo("should use query key for free-text search");
  });
});
