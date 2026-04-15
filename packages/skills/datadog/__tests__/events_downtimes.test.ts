import { describe, it } from "bun:test";

describe("events", () => {
  describe("list_events", () => {
    it.todo("should call /api/v1/events with start and end params");
    it.todo("should pass optional priority, sources, tags params");
    it.todo("should map events array from response");
  });

  describe("create_event", () => {
    it.todo("should POST to /api/v1/events with title and text");
    it.todo("should use alert_type and priority defaults");
    it.todo("should return status, event_id, and event_url");
  });
});

describe("downtimes", () => {
  describe("list_downtimes", () => {
    it.todo("should call /api/v1/downtime");
    it.todo("should pass current_only param when set");
    it.todo("should join scope arrays into comma string");
  });

  describe("create_downtime", () => {
    it.todo("should POST to /api/v1/downtime with scope");
    it.todo("should include optional start, end, message");
    it.todo("should accept monitor_tags array");
  });

  describe("cancel_downtime", () => {
    it.todo("should DELETE /api/v1/downtime/{id}");
    it.todo("should return empty object on 204");
  });
});
