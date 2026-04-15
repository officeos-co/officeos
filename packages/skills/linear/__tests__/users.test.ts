import { describe, it } from "bun:test";

describe("users", () => {
  describe("list_users", () => {
    it.todo("should query users with first param");
    it.todo("should return id, name, email, display_name, active");
  });

  describe("get_user", () => {
    it.todo("should query user by id");
    it.todo("should return admin boolean and created_at");
  });

  describe("me", () => {
    it.todo("should query viewer");
    it.todo("should return teams as name string array");
  });
});
