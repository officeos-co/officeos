import { describe, it } from "bun:test";

describe("issues", () => {
  describe("list_issues", () => {
    it.todo("should build filter string from optional params");
    it.todo("should omit filter clause when no filters provided");
    it.todo("should map labels nodes to name strings");
  });

  describe("get_issue", () => {
    it.todo("should use $id variable in GraphQL query");
    it.todo("should return project, cycle, estimate as nullable");
  });

  describe("create_issue", () => {
    it.todo("should include optional fields only when provided");
    it.todo("should return id, identifier, title, url");
  });

  describe("update_issue", () => {
    it.todo("should only include changed fields in input");
    it.todo("should return identifier and new state name");
  });

  describe("archive_issue", () => {
    it.todo("should return archived_at timestamp from entity");
  });

  describe("search_issues", () => {
    it.todo("should use searchIssues query with query variable");
    it.todo("should return state name from nested state.name");
  });
});
