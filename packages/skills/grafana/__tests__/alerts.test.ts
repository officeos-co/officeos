import { describe, it } from "bun:test";

describe("alerts", () => {
  describe("list_alert_rules", () => {
    it.todo("should call /api/v1/provisioning/alert-rules");
    it.todo("should map folderUID to folder_uid");
  });

  describe("get_alert_rule", () => {
    it.todo("should call /api/v1/provisioning/alert-rules/:uid");
  });

  describe("create_alert_rule", () => {
    it.todo("should POST to /api/v1/provisioning/alert-rules");
    it.todo("should include labels and annotations when provided");
  });

  describe("delete_alert_rule", () => {
    it.todo("should DELETE /api/v1/provisioning/alert-rules/:uid");
  });

  describe("list_alert_instances", () => {
    it.todo("should call /api/alertmanager/grafana/api/v2/alerts");
  });

  describe("pause_alert_rule", () => {
    it.todo("should PATCH /api/v1/provisioning/alert-rules/:uid with is_paused");
  });
});
