import { describe, it } from "bun:test";

describe("jira/projects", () => {
  describe("list_projects", () => {
    it.todo("should GET /rest/api/3/project/search");
    it.todo("should accept optional max_results and query");
    it.todo("should return array of id, key, name, type");
  });

  describe("get_project", () => {
    it.todo("should require project_key");
    it.todo("should GET /rest/api/3/project/{key}");
    it.todo("should return id, key, name, description, type");
  });

  describe("list_boards", () => {
    it.todo("should GET /rest/agile/1.0/board");
    it.todo("should accept optional project_key, type, max_results");
    it.todo("should filter boards by scrum or kanban type");
    it.todo("should return array of id, name, type");
  });

  describe("list_sprints", () => {
    it.todo("should require board_id");
    it.todo("should GET /rest/agile/1.0/board/{id}/sprint");
    it.todo("should accept optional state: active, future, closed");
    it.todo("should return array of id, name, state, start_date, end_date, goal");
  });

  describe("get_sprint", () => {
    it.todo("should require sprint_id");
    it.todo("should GET /rest/agile/1.0/sprint/{id}");
    it.todo("should return id, name, state, start_date, end_date, goal");
  });
});
