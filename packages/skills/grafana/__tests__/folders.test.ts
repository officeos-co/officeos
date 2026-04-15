import { describe, it } from "bun:test";

describe("folders", () => {
  describe("list_folders", () => {
    it.todo("should call /api/folders");
    it.todo("should return uid, id, title for each folder");
  });

  describe("create_folder", () => {
    it.todo("should POST to /api/folders");
    it.todo("should include uid when provided");
  });

  describe("delete_folder", () => {
    it.todo("should DELETE /api/folders/:uid");
  });
});
