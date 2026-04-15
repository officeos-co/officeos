import { describe, it } from "bun:test";

describe("asana/workspaces", () => {
  describe("list_workspaces", () => {
    it.todo("should GET /workspaces with Bearer auth");
    it.todo("should return array of gid, name");
    it.todo("should throw on non-200 response");
  });

  describe("list_tags", () => {
    it.todo("should require workspace_gid");
    it.todo("should GET /workspaces/{gid}/tags");
    it.todo("should return array of gid, name, color");
  });

  describe("search_tasks", () => {
    it.todo("should require workspace_gid and text");
    it.todo("should GET /workspaces/{gid}/tasks/search with text param");
    it.todo("should accept optional completed filter");
    it.todo("should accept optional assignee filter mapped to assignee.any");
    it.todo("should accept optional project_gid filter mapped to projects.any");
    it.todo("should return array of gid, name, completed, assignee, due_on");
  });
});
