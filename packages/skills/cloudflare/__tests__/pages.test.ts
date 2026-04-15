import { describe, it } from "bun:test";

describe("pages", () => {
  describe("list_pages_projects", () => {
    it.todo("should call /accounts/:id/pages/projects");
    it.todo("should map production_branch with main fallback");
  });

  describe("create_pages_project", () => {
    it.todo("should POST to /accounts/:id/pages/projects");
    it.todo("should include build_config when build_command or output_dir is provided");
  });

  describe("deploy_pages", () => {
    it.todo("should POST to /accounts/:id/pages/projects/:name/deployments");
    it.todo("should return deployment status from latest_stage.status");
  });
});
