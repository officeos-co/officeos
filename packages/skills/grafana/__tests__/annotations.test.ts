import { describe, it } from "bun:test";

describe("annotations", () => {
  describe("list_annotations", () => {
    it.todo("should call /api/annotations");
    it.todo("should pass from, to, dashboardId, panelId, tags params");
  });

  describe("create_annotation", () => {
    it.todo("should POST to /api/annotations");
    it.todo("should include timeEnd for region annotations");
  });

  describe("delete_annotation", () => {
    it.todo("should DELETE /api/annotations/:id");
  });
});
