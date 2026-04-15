import { describe, it } from "bun:test";

describe("firewall", () => {
  describe("list_firewall_rules", () => {
    it.todo("should call /zones/:id/firewall/rules");
    it.todo("should map paused to boolean");
  });

  describe("create_firewall_rule", () => {
    it.todo("should first create a filter, then create the rule referencing it");
    it.todo("should throw if filter creation fails");
  });

  describe("update_firewall_rule", () => {
    it.todo("should fetch current rule before updating");
    it.todo("should update filter expression when filter param is provided");
  });

  describe("delete_firewall_rule", () => {
    it.todo("should DELETE /zones/:id/firewall/rules/:ruleId");
  });
});
