import { describe, it } from "bun:test";

describe("domains", () => {
  describe("list_domains", () => {
    it.todo("should call /v9/projects/:id/domains");
    it.todo("should return domains with nullable gitBranch");
  });

  describe("add_domain", () => {
    it.todo("should POST to /v10/projects/:id/domains");
  });

  describe("remove_domain", () => {
    it.todo("should DELETE /v9/projects/:id/domains/:domain");
  });

  describe("get_domain_config", () => {
    it.todo("should call /v6/domains/:domain/config");
  });

  describe("verify_domain", () => {
    it.todo("should POST to /v9/projects/:id/domains/:domain/verify");
  });
});
