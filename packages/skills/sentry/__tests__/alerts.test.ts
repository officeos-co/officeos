import { describe, it } from "bun:test";

describe("alerts", () => {
  describe("list_alert_rules", () => {
    it.todo("should call /projects/{org}/{project}/rules/");
    it.todo("should coerce id to string");
    it.todo("should default frequency to 30 when missing");
  });

  describe("create_alert_rule", () => {
    it.todo("should parse conditions and actions JSON strings");
    it.todo("should POST with actionMatch: all");
    it.todo("should include environment only when provided");
  });

  describe("delete_alert_rule", () => {
    it.todo("should DELETE /projects/{org}/{project}/rules/{ruleId}/");
    it.todo("should return deleted id");
  });
});
