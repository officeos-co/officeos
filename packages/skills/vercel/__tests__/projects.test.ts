import { describe, it } from "bun:test";

describe("projects", () => {
  describe("list_projects", () => {
    it.todo("should call /v9/projects with limit param");
    it.todo("should return mapped project array");
    it.todo("should append teamId when team_id credential is set");
  });

  describe("get_project", () => {
    it.todo("should call /v9/projects/:id");
    it.todo("should return project with nullable fields");
  });

  describe("create_project", () => {
    it.todo("should POST to /v10/projects with name and framework");
    it.todo("should include gitRepository when provided");
  });

  describe("update_project", () => {
    it.todo("should PATCH to /v9/projects/:id");
  });

  describe("delete_project", () => {
    it.todo("should DELETE /v9/projects/:id and return { deleted: true }");
  });
});
