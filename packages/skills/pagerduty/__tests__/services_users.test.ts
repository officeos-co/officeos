import { describe, it } from "bun:test";

describe("services", () => {
  describe("list_services", () => {
    it.todo("should call /services");
    it.todo("should pass team_ids[] array param");
    it.todo("should pass query param for name search");
  });

  describe("get_service", () => {
    it.todo("should call /services/{id}");
    it.todo("should return escalation_policy and teams references");
  });
});

describe("users", () => {
  describe("list_users", () => {
    it.todo("should call /users with limit param");
    it.todo("should filter by query and team_ids");
  });

  describe("get_user", () => {
    it.todo("should call /users/{id}");
    it.todo("should return all user fields");
  });
});
