import { describe, it, expect } from "bun:test";

describe("webhooks", () => {
  describe("list_webhook_endpoints", () => {
    it.todo("should call /v1/webhook_endpoints with limit param");
    it.todo("should return id, url, status, enabled_events, created");
  });

  describe("create_webhook_endpoint", () => {
    it.todo("should POST to /v1/webhook_endpoints with url and events");
    it.todo("should set enabled_events using bracket notation");
    it.todo("should include description when provided");
    it.todo("should return signing secret in response");
  });
});
