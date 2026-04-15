import { describe, it, expect } from "bun:test";

describe("tickets", () => {
  describe("list_tickets", () => {
    it.todo("should call /crm/v3/objects/tickets with limit param");
  });

  describe("get_ticket", () => {
    it.todo("should call /crm/v3/objects/tickets/:id");
    it.todo("should include associations when present");
  });

  describe("create_ticket", () => {
    it.todo("should POST with properties object");
  });

  describe("update_ticket", () => {
    it.todo("should PATCH /crm/v3/objects/tickets/:id");
  });

  describe("delete_ticket", () => {
    it.todo("should DELETE /crm/v3/objects/tickets/:id");
  });
});
