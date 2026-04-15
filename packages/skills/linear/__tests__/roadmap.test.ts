import { describe, it } from "bun:test";

describe("roadmap", () => {
  describe("list_roadmaps", () => {
    it.todo("should query roadmaps with first param");
    it.todo("should return id, name, description, slug from slugId");
  });

  describe("get_roadmap", () => {
    it.todo("should query roadmap by id");
    it.todo("should return projects as name string array");
  });

  describe("create_attachment", () => {
    it.todo("should mutation attachmentCreate with issueId, url, title");
    it.todo("should return id, url, title, created_at");
  });
});
