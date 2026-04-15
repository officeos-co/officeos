import { describe, it } from "bun:test";

describe("issues", () => {
  describe("list_issues", () => {
    it.todo("should filter out pull requests from results");
    it.todo("should pass state and per_page as query params");
    it.todo("should map labels to name strings");
  });

  describe("get_issue", () => {
    it.todo("should return body, assignees, and comment count");
  });

  describe("create_issue", () => {
    it.todo("should POST with title, body, labels, assignees");
    it.todo("should return number, title, html_url");
  });

  describe("close_issue", () => {
    it.todo("should PATCH with state: closed");
    it.todo("should return number and new state");
  });

  describe("add_issue_comment", () => {
    it.todo("should POST body to /issues/{number}/comments");
  });
});
