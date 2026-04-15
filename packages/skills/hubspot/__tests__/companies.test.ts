import { describe, it, expect } from "bun:test";

describe("companies", () => {
  describe("list_companies", () => {
    it.todo("should call /crm/v3/objects/companies with limit param");
    it.todo("should support pagination cursor");
  });

  describe("get_company", () => {
    it.todo("should call /crm/v3/objects/companies/:id");
    it.todo("should return null associations when absent");
  });

  describe("create_company", () => {
    it.todo("should POST with properties object");
  });

  describe("update_company", () => {
    it.todo("should PATCH /crm/v3/objects/companies/:id");
  });

  describe("delete_company", () => {
    it.todo("should DELETE /crm/v3/objects/companies/:id");
  });

  describe("search_companies", () => {
    it.todo("should POST to /crm/v3/objects/companies/search with filterGroups");
  });
});
