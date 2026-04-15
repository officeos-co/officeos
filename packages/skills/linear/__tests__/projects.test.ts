import { describe, it } from "bun:test";

describe("projects", () => {
  describe("list_projects", () => {
    it.todo("should return id, name, description, state, progress, lead, start_date, target_date");
  });

  describe("get_project", () => {
    it.todo("should return members array as name strings");
    it.todo("should return teams array as name strings");
    it.todo("should compute issues_count from nodes length");
  });

  describe("create_project", () => {
    it.todo("should include teamIds array in input");
    it.todo("should include optional fields only when provided");
    it.todo("should return id, name, url");
  });

  describe("archive_project", () => {
    it.todo("should return archived_at from entity.archivedAt");
  });
});
