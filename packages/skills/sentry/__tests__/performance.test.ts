import { describe, it } from "bun:test";

describe("performance", () => {
  describe("list_transactions", () => {
    it.todo("should call /organizations/{org}/events/ with field list");
    it.todo("should use dataset=metricsEnhanced");
    it.todo("should add -sort prefix to sort field");
    it.todo("should map p50, p75, p95, failure_rate, apdex from response");
  });

  describe("get_transaction_summary", () => {
    it.todo("should query with transaction: filter in query param");
    it.todo("should use statsPeriod param");
    it.todo("should return p50, p75, p95, p99, throughput");
    it.todo("should return zeros for missing metric fields");
  });
});
