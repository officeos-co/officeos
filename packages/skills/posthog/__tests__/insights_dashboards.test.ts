import { describe, it } from "bun:test";

describe("insights", () => {
  describe("list_insights", () => {
    it.todo("should call /api/projects/{id}/insights/ with limit");
    it.todo("should pass saved=true filter");
  });

  describe("get_insight", () => {
    it.todo("should call /api/projects/{id}/insights/{id}/");
    it.todo("should return result data");
  });

  describe("create_insight", () => {
    it.todo("should POST with filters object");
    it.todo("should include name and description when provided");
  });
});

describe("dashboards", () => {
  describe("list_dashboards", () => {
    it.todo("should call /api/projects/{id}/dashboards/");
    it.todo("should return pinned field");
  });

  describe("get_dashboard", () => {
    it.todo("should call /api/projects/{id}/dashboards/{id}/");
    it.todo("should return tiles array");
  });
});

describe("cohorts_annotations", () => {
  describe("list_cohorts", () => {
    it.todo("should call /api/projects/{id}/cohorts/");
    it.todo("should return is_calculating field");
  });

  describe("create_cohort", () => {
    it.todo("should POST with name and optional filters");
  });

  describe("create_annotation", () => {
    it.todo("should POST with content and scope");
    it.todo("should default scope to project");
    it.todo("should include dashboard_id when provided");
  });
});
