import { describe, it } from "bun:test";

describe("iam", () => {
  describe("list_users", () => {
    it.todo("should call ListUsers with MaxItems");
    it.todo("should return user_name, user_id, arn");
  });

  describe("list_roles", () => {
    it.todo("should call ListRoles");
    it.todo("should parse max_session_duration as integer");
  });

  describe("list_policies", () => {
    it.todo("should call ListPolicies with Scope filter");
  });

  describe("create_user", () => {
    it.todo("should call CreateUser with UserName and Path");
    it.todo("should attach tags when provided");
  });

  describe("attach_policy", () => {
    it.todo("should call AttachUserPolicy for user_name");
    it.todo("should call AttachRolePolicy for role_name");
    it.todo("should call AttachGroupPolicy for group_name");
    it.todo("should throw when no entity provided");
  });
});
