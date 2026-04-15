import { describe, it } from "bun:test";

describe("gists", () => {
  describe("list_gists", () => {
    it.todo("should call /gists with per_page");
    it.todo("should return files as array of filename strings");
  });

  describe("get_gist", () => {
    it.todo("should call /gists/{id}");
    it.todo("should return files array with filename, language, content");
  });

  describe("create_gist", () => {
    it.todo("should POST with description, public, files map");
    it.todo("should transform files map to {content} objects");
    it.todo("should return id and html_url");
  });
});
