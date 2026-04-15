import { describe, it } from "bun:test";

describe("apply", () => {
  describe("apply", () => {
    it.todo("should POST JSON manifest and return action=created");
    it.todo("should PUT on 409 conflict and return action=configured");
    it.todo("should determine API path from apiVersion (core vs group)");
    it.todo("should throw on invalid JSON input");
  });

  describe("delete_resource", () => {
    it.todo("should route Deployment to /apis/apps/v1");
    it.todo("should route Job to /apis/batch/v1");
    it.todo("should route ConfigMap to /api/v1");
    it.todo("should append gracePeriodSeconds when provided");
  });
});
