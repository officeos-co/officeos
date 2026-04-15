import { describe, it, expect } from "bun:test";

describe("records", () => {
  describe("list_objects", () => {
    it.todo("should call /sobjects");
    it.todo("should return mapped sobject array with all flags");
  });

  describe("describe_object", () => {
    it.todo("should call /sobjects/:type/describe");
    it.todo("should return fields with picklistValues");
  });

  describe("get_record", () => {
    it.todo("should call /sobjects/:type/:id");
    it.todo("should append ?fields= param when fields provided");
  });

  describe("create_record", () => {
    it.todo("should POST to /sobjects/:type with fields body");
    it.todo("should return id and success");
  });

  describe("update_record", () => {
    it.todo("should PATCH /sobjects/:type/:id with fields body");
    it.todo("should return id and success:true");
  });

  describe("delete_record", () => {
    it.todo("should DELETE /sobjects/:type/:id");
    it.todo("should return id and success:true");
  });

  describe("upsert_record", () => {
    it.todo("should PATCH /sobjects/:type/:externalIdField/:externalIdValue");
    it.todo("should return created flag from response");
  });
});
