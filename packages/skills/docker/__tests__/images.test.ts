import { describe, it } from "bun:test";

describe("images", () => {
  describe("list_images", () => {
    it.todo("should call /images/json with all param");
    it.todo("should return repo_tags, size, and created per image");
  });

  describe("pull", () => {
    it.todo("should POST /images/create with fromImage and tag params");
    it.todo("should parse NDJSON response for last status line");
    it.todo("should default tag to 'latest'");
  });

  describe("push", () => {
    it.todo("should POST /images/{repo}/push with tag param");
  });

  describe("tag", () => {
    it.todo("should POST /images/{source}/tag with repo and tag params");
  });

  describe("rm_image", () => {
    it.todo("should DELETE /images/{image} with force param");
    it.todo("should return deleted layers array");
  });

  describe("inspect_image", () => {
    it.todo("should return config, layers, os, architecture, and size");
  });

  describe("history", () => {
    it.todo("should map layer history to created_by, size, created");
  });
});
