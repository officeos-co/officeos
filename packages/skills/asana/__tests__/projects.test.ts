import { describe, it } from "bun:test";

describe("asana/projects", () => {
  describe("list_projects", () => {
    it.todo("should require workspace_gid");
    it.todo("should GET /projects with workspace query param");
    it.todo("should accept optional team_gid filter");
    it.todo("should accept optional archived filter");
    it.todo("should return array of gid, name, color, archived, created_at, modified_at");
  });

  describe("get_project", () => {
    it.todo("should require project_gid");
    it.todo("should GET /projects/{gid}");
    it.todo("should return gid, name, notes, color, archived, created_at, modified_at, due_date");
  });

  describe("create_project", () => {
    it.todo("should require workspace_gid and name");
    it.todo("should POST to /projects");
    it.todo("should wrap body in data envelope");
    it.todo("should include team when provided");
    it.todo("should return gid, name, color");
  });

  describe("update_project", () => {
    it.todo("should require project_gid");
    it.todo("should PUT to /projects/{gid}");
    it.todo("should only send provided fields");
    it.todo("should return gid, name");
  });

  describe("list_sections", () => {
    it.todo("should require project_gid");
    it.todo("should GET /projects/{gid}/sections");
    it.todo("should return array of gid, name, created_at");
  });

  describe("create_section", () => {
    it.todo("should require project_gid and name");
    it.todo("should POST to /projects/{gid}/sections");
    it.todo("should return gid, name");
  });
});
