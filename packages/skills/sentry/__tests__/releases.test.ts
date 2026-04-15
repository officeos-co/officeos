import { describe, it } from "bun:test";

describe("releases", () => {
  describe("list_releases", () => {
    it.todo("should call /organizations/{org}/releases/ with per_page");
    it.todo("should filter by project when provided");
    it.todo("should map commit_count, deploy_count, new_groups");
  });

  describe("create_release", () => {
    it.todo("should POST with version and projects array");
    it.todo("should include ref and url only when provided");
  });

  describe("finalize_release", () => {
    it.todo("should PUT with dateReleased set to current ISO timestamp");
    it.todo("should return version and date_released");
  });

  describe("list_deploys", () => {
    it.todo("should call /organizations/{org}/releases/{version}/deploys/");
    it.todo("should map date_started and date_finished to null when missing");
  });

  describe("create_deploy", () => {
    it.todo("should POST to deploys endpoint with environment");
    it.todo("should include name and url only when provided");
  });
});
