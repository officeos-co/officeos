import { describe, it } from "bun:test";

describe("analytics", () => {
  describe("get_zone_analytics", () => {
    it.todo("should call /zones/:id/analytics/dashboard");
    it.todo("should default since to -1440 (last 24h)");
    it.todo("should map totals fields");
  });

  describe("get_dns_analytics", () => {
    it.todo("should call /zones/:id/dns_analytics/report");
    it.todo("should map queryCount, responseCode, queryType fields");
  });
});
