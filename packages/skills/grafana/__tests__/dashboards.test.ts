import { describe, it } from "bun:test";

describe("dashboards", () => {
  describe("list_dashboards", () => {
    it.todo("should call /api/search with type=dash-db");
    it.todo("should pass query, tag, and folderIds params");
  });

  describe("get_dashboard", () => {
    it.todo("should call /api/dashboards/uid/:uid");
    it.todo("should return meta.url as url");
  });

  describe("create_dashboard", () => {
    it.todo("should POST to /api/dashboards/db");
    it.todo("should set id and uid to null");
  });

  describe("update_dashboard", () => {
    it.todo("should fetch current dashboard before updating");
    it.todo("should merge changed fields onto current dashboard");
  });

  describe("delete_dashboard", () => {
    it.todo("should DELETE /api/dashboards/uid/:uid");
  });

  describe("get_panel_data", () => {
    it.todo("should find panel by id in dashboard panels array");
    it.todo("should POST to /api/ds/query with panel targets");
  });

  describe("search", () => {
    it.todo("should call /api/search with query, type, tag");
  });
});
