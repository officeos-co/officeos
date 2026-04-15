import { describe, it } from "bun:test";

describe("monitors", () => {
  describe("list_monitors", () => {
    it.todo("should call /api/v1/monitor");
    it.todo("should pass tags query param when provided");
    it.todo("should map overall_state to status field");
  });

  describe("get_monitor", () => {
    it.todo("should call /api/v1/monitor/{id}");
    it.todo("should return all monitor fields including options");
  });

  describe("create_monitor", () => {
    it.todo("should POST to /api/v1/monitor with name, type, query");
    it.todo("should include optional message and tags when provided");
    it.todo("should return id, name, type, status");
  });

  describe("update_monitor", () => {
    it.todo("should PUT to /api/v1/monitor/{id}");
    it.todo("should only include fields that are provided");
  });

  describe("mute_monitor", () => {
    it.todo("should POST to /api/v1/monitor/{id}/mute");
    it.todo("should include scope and end when provided");
  });

  describe("delete_monitor", () => {
    it.todo("should DELETE /api/v1/monitor/{id}");
    it.todo("should return deleted_monitor_id");
  });
});
