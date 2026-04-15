import { describe, it } from "bun:test";

describe("jira/issues", () => {
  describe("list_issues", () => {
    it.todo("should require jql");
    it.todo("should accept optional max_results defaulting to 50");
    it.todo("should accept optional start_at defaulting to 0");
    it.todo("should accept optional fields as comma-separated string");
    it.todo("should POST to /rest/api/3/search with Basic auth");
    it.todo("should return total, start_at, max_results, issues array");
    it.todo("should map issue fields: id, key, summary, status, assignee, priority");
    it.todo("should throw on non-200 response");
  });

  describe("get_issue", () => {
    it.todo("should require issue_key");
    it.todo("should GET /rest/api/3/issue/{key}");
    it.todo("should return id, key, summary, description, status, assignee, reporter");
    it.todo("should return labels array");
    it.todo("should return due_date as null when not set");
  });

  describe("create_issue", () => {
    it.todo("should require project_key and summary");
    it.todo("should default issue_type to Task");
    it.todo("should POST to /rest/api/3/issue");
    it.todo("should encode description as Atlassian Document Format");
    it.todo("should include priority when provided");
    it.todo("should include assignee accountId when provided");
    it.todo("should include labels array when provided");
    it.todo("should include duedate when provided");
    it.todo("should include parent key for sub-tasks");
    it.todo("should return id, key, url");
  });

  describe("update_issue", () => {
    it.todo("should require issue_key");
    it.todo("should PUT to /rest/api/3/issue/{key}");
    it.todo("should only include provided fields in body");
    it.todo("should encode description as ADF when provided");
    it.todo("should return success: true");
  });

  describe("delete_issue", () => {
    it.todo("should require issue_key");
    it.todo("should DELETE /rest/api/3/issue/{key}");
    it.todo("should return success: true");
  });

  describe("transition_issue", () => {
    it.todo("should require issue_key and transition_id");
    it.todo("should POST to /rest/api/3/issue/{key}/transitions");
    it.todo("should include comment in update field when provided");
    it.todo("should return success: true");
  });

  describe("get_transitions", () => {
    it.todo("should require issue_key");
    it.todo("should GET /rest/api/3/issue/{key}/transitions");
    it.todo("should return array of id, name, to_status");
  });

  describe("assign_issue", () => {
    it.todo("should require issue_key");
    it.todo("should PUT to /rest/api/3/issue/{key}/assignee");
    it.todo("should send accountId null when account_id not provided");
    it.todo("should return success: true");
  });

  describe("search_issues", () => {
    it.todo("should require jql");
    it.todo("should behave identically to list_issues");
  });
});
