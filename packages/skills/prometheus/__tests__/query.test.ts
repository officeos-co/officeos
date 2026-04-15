import { describe, it } from "bun:test";

describe("query", () => {
  describe("query (instant)", () => {
    it.todo("should call /api/v1/query with query param");
    it.todo("should pass time param when provided");
    it.todo("should pass timeout param when provided");
    it.todo("should throw when response status is not success");
    it.todo("should return resultType and result from data field");
  });

  describe("query_range", () => {
    it.todo("should call /api/v1/query_range with query, start, end, step");
    it.todo("should return matrix resultType");
    it.todo("should handle missing data gracefully");
  });
});
