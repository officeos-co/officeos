import { describe, it } from "bun:test";

describe("projects", () => {
  describe("list_projects", () => {
    it.todo("should call /organizations/{org}/projects/");
    it.todo("should map id, slug, name, platform, status, date_created");
  });

  describe("get_project", () => {
    it.todo("should call /projects/{org}/{project}/");
    it.todo("should extract DSN from keys[0].dsn.public");
    it.todo("should return team slug from team.slug");
  });

  describe("create_project", () => {
    it.todo("should POST to /teams/{org}/{team}/projects/");
    it.todo("should include platform only when provided");
  });

  describe("get_project_stats", () => {
    it.todo("should call /projects/{org}/{project}/stats/ with stat, resolution, since");
    it.todo("should return array of { timestamp, count } tuples");
  });
});
