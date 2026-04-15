import { describe, it } from "bun:test";

// Detailed tests live in __tests__/issues.test.ts, __tests__/comments.test.ts, __tests__/projects.test.ts

describe("jira", () => {
  describe("actions", () => {
    // Issues
    it.todo("should expose list_issues action");
    it.todo("should expose get_issue action");
    it.todo("should expose create_issue action");
    it.todo("should expose update_issue action");
    it.todo("should expose delete_issue action");
    it.todo("should expose transition_issue action");
    it.todo("should expose get_transitions action");
    it.todo("should expose assign_issue action");
    it.todo("should expose search_issues action");
    // Comments
    it.todo("should expose list_comments action");
    it.todo("should expose add_comment action");
    it.todo("should expose update_comment action");
    it.todo("should expose delete_comment action");
    // Projects & Boards
    it.todo("should expose list_projects action");
    it.todo("should expose get_project action");
    it.todo("should expose list_boards action");
    it.todo("should expose list_sprints action");
    it.todo("should expose get_sprint action");
  });

  describe("credentials", () => {
    it.todo("should require domain credential");
    it.todo("should require email credential");
    it.todo("should require api_token credential");
    it.todo("should build Basic auth header from base64(email:api_token)");
    it.todo("should build correct base URL from domain");
  });
});
