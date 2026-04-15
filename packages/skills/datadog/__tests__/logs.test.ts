import { describe, it } from "bun:test";

describe("logs", () => {
  describe("search_logs", () => {
    it.todo("should POST to /api/v2/logs/events/search");
    it.todo("should pass filter query, from, to in request body");
    it.todo("should respect limit and sort params");
    it.todo("should map response data array to log objects");
  });

  describe("aggregate_logs", () => {
    it.todo("should POST to /api/v2/logs/analytics/aggregate");
    it.todo("should map group_by strings to facet objects");
    it.todo("should include compute specs when provided");
    it.todo("should return buckets array");
  });
});
