import { describe, it } from "bun:test";

describe("users", () => {
  describe("list_users", () => {
    it.todo("should call /api/users with perpage param");
    it.todo("should map isGrafanaAdmin to is_admin");
  });

  describe("get_user", () => {
    it.todo("should call /api/users/:id");
    it.todo("should return is_disabled and last_seen_at fields");
  });

  describe("get_org", () => {
    it.todo("should call /api/org");
  });

  describe("list_org_users", () => {
    it.todo("should call /api/org/users");
    it.todo("should return login, role, email");
  });
});
