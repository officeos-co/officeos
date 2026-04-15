import { describe, it } from "bun:test";

describe("comments", () => {
  describe("list_comments", () => {
    it.todo("should query issue comments by issue id");
    it.todo("should return id, body, user name, created_at");
  });

  describe("create_comment", () => {
    it.todo("should mutation commentCreate with issueId and body");
    it.todo("should return id, body, created_at");
  });

  describe("update_comment", () => {
    it.todo("should mutation commentUpdate with comment id and new body");
    it.todo("should return updated_at timestamp");
  });

  describe("delete_comment", () => {
    it.todo("should mutation commentDelete with id");
    it.todo("should return success boolean");
  });
});
