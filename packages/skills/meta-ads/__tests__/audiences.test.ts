import { describe, it } from "bun:test";

describe("audiences", () => {
  describe("list_custom_audiences", () => {
    it.todo("should call /{account_id}/customaudiences with fields and limit");
    it.todo("should return approximate_count as null when missing");
  });

  describe("create_custom_audience", () => {
    it.todo("should POST name, subtype to /{account_id}/customaudiences");
    it.todo("should include rule when provided");
    it.todo("should include description when provided");
  });

  describe("get_audience_size", () => {
    it.todo("should fetch audience with approximate_count and delivery_status fields");
  });
});
