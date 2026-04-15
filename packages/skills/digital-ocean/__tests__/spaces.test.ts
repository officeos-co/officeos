import { describe, it } from "bun:test";

describe("spaces", () => {
  describe("list_spaces", () => {
    it.todo("should filter by region when provided");
    it.todo("should return empty string for missing created_at");
  });

  describe("create_space", () => {
    it.todo("should POST name and region");
    it.todo("should fallback to params for name/region when absent in response");
  });

  describe("delete_space", () => {
    it.todo("should DELETE /spaces/:name with region query param");
  });

  describe("list_space_objects", () => {
    it.todo("should include prefix when provided");
    it.todo("should return key, size, last_modified, etag");
  });

  describe("put_space_object", () => {
    it.todo("should POST with content_type and acl");
    it.todo("should return etag and key");
  });

  describe("get_space_object", () => {
    it.todo("should include region in query param");
    it.todo("should return body content");
  });

  describe("delete_space_object", () => {
    it.todo("should DELETE /spaces/:space/objects/:key?region=");
  });
});
