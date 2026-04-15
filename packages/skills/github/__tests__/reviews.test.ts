import { describe, it } from "bun:test";

describe("reviews", () => {
  describe("list_pr_comments", () => {
    it.todo("should call /pulls/{number}/comments with per_page");
    it.todo("should return path as null when not present");
  });

  describe("add_pr_comment", () => {
    it.todo("should POST to /issues/{number}/comments (not /pulls/)");
    it.todo("should return id and html_url");
  });

  describe("list_pr_reviews", () => {
    it.todo("should call /pulls/{number}/reviews");
    it.todo("should return id, author, state, body, submitted_at");
  });

  describe("request_pr_review", () => {
    it.todo("should POST reviewers and team_reviewers to /requested_reviewers");
    it.todo("should return requested_reviewers and requested_teams as login/slug arrays");
  });
});
