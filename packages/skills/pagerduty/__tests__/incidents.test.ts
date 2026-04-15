import { describe, it } from "bun:test";

describe("incidents", () => {
  describe("list_incidents", () => {
    it.todo("should call /incidents with correct query params");
    it.todo("should pass statuses[] array params correctly");
    it.todo("should map service ref from response");
    it.todo("should default limit to 25");
  });

  describe("get_incident", () => {
    it.todo("should call /incidents/{id}");
    it.todo("should extract body.details as description");
    it.todo("should map assignments to assigned_to array");
  });

  describe("create_incident", () => {
    it.todo("should POST to /incidents with incident wrapper");
    it.todo("should include service reference with type field");
    it.todo("should wrap body in incident_body type object");
    it.todo("should include escalation_policy_reference when provided");
  });

  describe("acknowledge_incident", () => {
    it.todo("should PUT to /incidents/{id} with status acknowledged");
  });

  describe("resolve_incident", () => {
    it.todo("should PUT to /incidents/{id} with status resolved");
    it.todo("should return resolved_at from last_status_change_at");
  });

  describe("add_note", () => {
    it.todo("should POST to /incidents/{id}/notes with note wrapper");
    it.todo("should return id, content, created_at");
  });

  describe("list_alerts", () => {
    it.todo("should call /incidents/{id}/alerts");
    it.todo("should return mapped alert objects");
  });
});
