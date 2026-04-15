import { describe, it } from "bun:test";

describe("deployments", () => {
  describe("list_deployments", () => {
    it.todo("should call /v6/deployments with limit param");
    it.todo("should filter by projectId when provided");
    it.todo("should filter by state when provided");
  });

  describe("get_deployment", () => {
    it.todo("should call /v13/deployments/:id");
    it.todo("should return deployment with nullable fields");
  });

  describe("create_deployment", () => {
    it.todo("should POST to /v13/deployments");
    it.todo("should include gitSource when provided");
  });

  describe("cancel_deployment", () => {
    it.todo("should PATCH /v12/deployments/:id/cancel");
  });

  describe("redeploy", () => {
    it.todo("should POST to /v13/deployments with forceNew=1 and deploymentId");
  });

  describe("list_checks", () => {
    it.todo("should call /v1/deployments/:id/checks");
  });

  describe("get_check", () => {
    it.todo("should call /v1/deployments/:id/checks/:checkId");
  });
});
