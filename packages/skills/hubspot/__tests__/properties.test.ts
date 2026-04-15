import { describe, it, expect } from "bun:test";

describe("properties", () => {
  describe("list_properties", () => {
    it.todo("should call /crm/v3/properties/:object_type");
    it.todo("should return name, label, type, fieldType, groupName, description");
    it.todo("should set description to null when absent");
  });

  describe("create_property", () => {
    it.todo("should POST to /crm/v3/properties/:object_type");
    it.todo("should map field_type to fieldType and group_name to groupName");
    it.todo("should include options when provided for enumeration type");
    it.todo("should include description when provided");
  });
});
