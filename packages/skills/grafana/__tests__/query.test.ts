import { describe, it } from "bun:test";

describe("query", () => {
  describe("query_datasource", () => {
    it.todo("should POST to /api/ds/query with datasource uid");
    it.todo("should include interval when provided");
    it.todo("should default from to now-1h and to to now");
  });
});
