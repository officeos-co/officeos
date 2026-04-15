import { describe, it } from "bun:test";

describe("gitlab", () => {
  describe("projects", () => {
    it.todo("list_projects should call /projects with membership param");
    it.todo("list_projects should filter by search query");
    it.todo("get_project should call /projects/:id and return mapped fields");
    it.todo("get_project should accept URL-encoded path as id");
  });

  describe("merge_requests", () => {
    it.todo("list_merge_requests should filter by state");
    it.todo("get_mr should return description and merged_at fields");
    it.todo("create_mr should POST source_branch, target_branch, title");
    it.todo("merge_mr should PUT to /merge_requests/:iid/merge with squash option");
  });

  describe("issues", () => {
    it.todo("list_issues should filter by state and labels");
    it.todo("get_issue should return description and labels array");
    it.todo("create_issue should POST title and optional fields");
    it.todo("create_issue should split assignee_ids into array of numbers");
  });

  describe("pipelines", () => {
    it.todo("list_pipelines should filter by status and ref");
    it.todo("get_pipeline should return duration and finished_at");
    it.todo("retry_pipeline should POST to /pipelines/:id/retry");
    it.todo("cancel_pipeline should POST to /pipelines/:id/cancel");
  });

  describe("branches", () => {
    it.todo("list_branches should filter by search");
    it.todo("create_branch should POST branch name and ref");
    it.todo("delete_branch should DELETE the branch and return success: true");
  });

  describe("tags", () => {
    it.todo("list_tags should return name, message, and commit_sha");
    it.todo("list_tags should filter by search");
  });

  describe("search", () => {
    it.todo("search should accept scope=projects and return raw results");
    it.todo("search should accept scope=merge_requests");
  });

  describe("members", () => {
    it.todo("list_members should return id, username, access_level");
  });

  describe("auth", () => {
    it.todo("should use PRIVATE-TOKEN header with provided token");
    it.todo("should use custom url from credentials when provided");
    it.todo("should default to https://gitlab.com when url not provided");
    it.todo("should throw on non-ok response with status and body");
  });
});
