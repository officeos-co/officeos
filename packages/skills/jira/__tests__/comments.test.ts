import { describe, it } from "bun:test";

describe("jira/comments", () => {
  describe("list_comments", () => {
    it.todo("should require issue_key");
    it.todo("should GET /rest/api/3/issue/{key}/comment");
    it.todo("should accept optional max_results");
    it.todo("should return array of id, author, body, created, updated");
    it.todo("should extract plain text from Atlassian Document Format body");
  });

  describe("add_comment", () => {
    it.todo("should require issue_key and body");
    it.todo("should POST to /rest/api/3/issue/{key}/comment");
    it.todo("should encode body as Atlassian Document Format");
    it.todo("should return id, author, body, created");
  });

  describe("update_comment", () => {
    it.todo("should require issue_key, comment_id, body");
    it.todo("should PUT to /rest/api/3/issue/{key}/comment/{comment_id}");
    it.todo("should encode new body as ADF");
    it.todo("should return id, body, updated");
  });

  describe("delete_comment", () => {
    it.todo("should require issue_key and comment_id");
    it.todo("should DELETE /rest/api/3/issue/{key}/comment/{comment_id}");
    it.todo("should return success: true");
  });
});
