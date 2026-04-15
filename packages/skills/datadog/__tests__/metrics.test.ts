import { describe, it } from "bun:test";

describe("metrics", () => {
  describe("query_metrics", () => {
    it.todo("should call /api/v1/query with query, from, and to params");
    it.todo("should map series pointlist from response");
    it.todo("should throw on non-2xx response with Datadog error body");
  });

  describe("submit_metrics", () => {
    it.todo("should POST to /api/v2/series with correct payload shape");
    it.todo("should convert [timestamp, value] tuples to {timestamp, value} objects");
    it.todo("should return status ok when errors array is empty");
  });

  describe("list_metrics", () => {
    it.todo("should call /api/v1/metrics with from param");
    it.todo("should include host query param when provided");
    it.todo("should return flat array of metric name strings");
  });
});
