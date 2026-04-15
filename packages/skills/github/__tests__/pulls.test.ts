import { describe, it } from "bun:test";

describe("pulls", () => {
  describe("list_prs", () => {
    it.todo("should call /pulls with state and per_page");
    it.todo("should map draft flag correctly");
  });

  describe("get_pr", () => {
    it.todo("should return additions, deletions, changed_files");
    it.todo("should return head_ref and base_ref");
  });

  describe("create_pr", () => {
    it.todo("should POST with title, head, base, draft");
    it.todo("should return number, title, html_url");
  });

  describe("merge_pr", () => {
    it.todo("should PUT to /pulls/{number}/merge");
    it.todo("should pass merge_method, commit_title, commit_message");
    it.todo("should return merged, message, sha");
  });

  describe("request_pr_review", () => {
    it.todo("should POST reviewers and team_reviewers");
    it.todo("should return requested_reviewers and requested_teams as login/slug arrays");
  });

  describe("list_pr_files", () => {
    it.todo("should return filename, status, additions, deletions, patch");
  });
});
