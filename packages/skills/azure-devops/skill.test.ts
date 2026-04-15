import { describe, it } from "bun:test";

describe("azure-devops", () => {
  describe("projects", () => {
    it.todo("list_projects should call /_apis/projects with $top and $skip params");
    it.todo("list_projects should return id, name, state, visibility");
    it.todo("get_project should return default_team_name from defaultTeam.name");
    it.todo("get_project should handle project with no description (null)");
  });

  describe("repositories", () => {
    it.todo("list_repos should call /:project/_apis/git/repositories");
    it.todo("list_repos should return default_branch as null when unset");
    it.todo("get_repo should return size and remote_url");
  });

  describe("pull_requests", () => {
    it.todo("list_prs should pass searchCriteria.status filter");
    it.todo("list_prs should return source_branch as full ref name");
    it.todo("create_pr should normalize short branch names to refs/heads/");
    it.todo("create_pr should not include description when not provided");
    it.todo("get_pr should return merge_status field");
  });

  describe("work_items", () => {
    it.todo("list_work_items should POST WIQL query and batch-fetch results");
    it.todo("list_work_items should return empty array when query has no results");
    it.todo("list_work_items should slice IDs to top limit before batch fetch");
    it.todo("create_work_item should build JSON Patch ops array");
    it.todo("create_work_item should include description op when provided");
    it.todo("create_work_item should use $Type path prefix in work item type URL");
    it.todo("get_work_item should return description from System.Description field");
    it.todo("get_work_item should return assigned_to displayName or null");
  });

  describe("builds", () => {
    it.todo("list_builds should pass definitions filter when definition_id given");
    it.todo("list_builds should normalize branch to refs/heads/ when filtering");
    it.todo("list_builds should pass resultFilter when result given");
    it.todo("queue_build should POST with definition.id in body");
    it.todo("queue_build should include sourceBranch when branch given");
    it.todo("queue_build should pass parameters JSON string as-is");
  });

  describe("pipelines", () => {
    it.todo("list_pipelines should call /:project/_apis/pipelines with $top");
    it.todo("list_pipelines should return folder field (defaults to backslash)");
    it.todo("run_pipeline should POST to /:project/_apis/pipelines/:id/runs");
    it.todo("run_pipeline should set resources.repositories.self.refName when branch given");
    it.todo("run_pipeline should parse variables JSON and include in body");
  });

  describe("auth", () => {
    it.todo("should use Basic auth with base64(:token) — empty username");
    it.todo("should include api-version=7.1 in all requests");
    it.todo("should throw descriptive error on non-ok response");
  });
});
