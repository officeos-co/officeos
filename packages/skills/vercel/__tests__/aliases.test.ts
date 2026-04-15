import { describe, it } from "bun:test";

describe("aliases", () => {
  describe("list_aliases", () => {
    it.todo("should call /v4/aliases with limit and optional projectId");
  });

  describe("set_alias", () => {
    it.todo("should POST to /v2/deployments/:id/aliases");
  });

  describe("remove_alias", () => {
    it.todo("should DELETE /v2/aliases/:aliasId");
  });

  describe("list_secrets", () => {
    it.todo("should call /v3/secrets with limit");
  });

  describe("create_secret", () => {
    it.todo("should POST to /v2/secrets");
  });

  describe("delete_secret", () => {
    it.todo("should DELETE /v2/secrets/:name");
  });
});
