import { describe, it } from "bun:test";

describe("apps", () => {
  describe("list_apps", () => {
    it.todo("should GET /apps with per_page");
    it.todo("should return null for missing live_url");
  });

  describe("get_app", () => {
    it.todo("should GET /apps/:id");
    it.todo("should return last_deployment_active_at as null when absent");
  });

  describe("create_app", () => {
    it.todo("should parse spec JSON and POST to /apps");
    it.todo("should return id, default_ingress, live_url");
  });

  describe("delete_app", () => {
    it.todo("should DELETE /apps/:id");
    it.todo("should return deleted: true");
  });

  describe("list_deployments", () => {
    it.todo("should GET /apps/:id/deployments with per_page");
    it.todo("should return empty string for missing cause/phase");
    it.todo("should return null for missing progress");
  });
});
