import { describe, it } from "bun:test";

// Detailed tests live in __tests__/workspaces.test.ts, __tests__/projects.test.ts, __tests__/tasks.test.ts

describe("asana", () => {
  describe("actions", () => {
    // Workspaces
    it.todo("should expose list_workspaces action");
    it.todo("should expose list_tags action");
    it.todo("should expose search_tasks action");
    // Projects
    it.todo("should expose list_projects action");
    it.todo("should expose get_project action");
    it.todo("should expose create_project action");
    it.todo("should expose update_project action");
    it.todo("should expose list_sections action");
    it.todo("should expose create_section action");
    // Tasks
    it.todo("should expose list_tasks action");
    it.todo("should expose get_task action");
    it.todo("should expose create_task action");
    it.todo("should expose update_task action");
    it.todo("should expose delete_task action");
    it.todo("should expose list_comments action");
    it.todo("should expose add_comment action");
  });

  describe("credentials", () => {
    it.todo("should require access_token credential");
    it.todo("should send Authorization Bearer header");
    it.todo("should wrap POST/PUT bodies in data envelope");
    it.todo("should unwrap data from API responses");
  });
});
