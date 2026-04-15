import { describe, it, expect } from "bun:test";

describe("deals", () => {
  describe("list_deals", () => {
    it.todo("should call /crm/v3/objects/deals with limit param");
    it.todo("should support pagination cursor");
  });

  describe("get_deal", () => {
    it.todo("should call /crm/v3/objects/deals/:id");
  });

  describe("create_deal", () => {
    it.todo("should POST with dealname and dealstage required");
    it.todo("should include pipeline, amount, closedate when provided");
    it.todo("should merge additional properties");
  });

  describe("update_deal", () => {
    it.todo("should PATCH /crm/v3/objects/deals/:id");
  });

  describe("delete_deal", () => {
    it.todo("should DELETE /crm/v3/objects/deals/:id");
  });

  describe("search_deals", () => {
    it.todo("should POST to /crm/v3/objects/deals/search");
  });
});
