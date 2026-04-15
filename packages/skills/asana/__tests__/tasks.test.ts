import { describe, it } from "bun:test";

describe("asana/tasks", () => {
  describe("list_tasks", () => {
    it.todo("should accept optional project_gid");
    it.todo("should accept optional section_gid");
    it.todo("should accept optional assignee");
    it.todo("should accept optional completed filter");
    it.todo("should accept optional modified_since");
    it.todo("should GET /tasks with appropriate query params");
    it.todo("should return array of gid, name, completed, assignee, due_on, created_at, modified_at");
  });

  describe("get_task", () => {
    it.todo("should require task_gid");
    it.todo("should GET /tasks/{gid}");
    it.todo("should return gid, name, notes, completed, assignee, due_on");
  });

  describe("create_task", () => {
    it.todo("should require project_gid and name");
    it.todo("should POST to /tasks");
    it.todo("should wrap body in data envelope");
    it.todo("should include projects array with project_gid");
    it.todo("should include section in memberships when section_gid provided");
    it.todo("should return gid, name, created_at");
  });

  describe("update_task", () => {
    it.todo("should require task_gid");
    it.todo("should PUT to /tasks/{gid}");
    it.todo("should support marking completed true or false");
    it.todo("should return gid, name, completed");
  });

  describe("delete_task", () => {
    it.todo("should require task_gid");
    it.todo("should DELETE /tasks/{gid}");
    it.todo("should return success: true");
  });

  describe("list_comments", () => {
    it.todo("should require task_gid");
    it.todo("should GET /tasks/{gid}/stories");
    it.todo("should return array of gid, type, text, created_at, created_by");
  });

  describe("add_comment", () => {
    it.todo("should require task_gid and text");
    it.todo("should POST to /tasks/{gid}/stories");
    it.todo("should wrap body in data envelope");
    it.todo("should return gid, text, created_at");
  });
});
