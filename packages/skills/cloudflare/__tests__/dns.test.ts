import { describe, it } from "bun:test";

describe("dns", () => {
  describe("list_zones", () => {
    it.todo("should call /zones with per_page param");
    it.todo("should filter by name when provided");
    it.todo("should map plan.name to plan string");
  });

  describe("get_zone", () => {
    it.todo("should call /zones/:id");
    it.todo("should return ssl_status from zone.ssl.status");
  });

  describe("list_dns_records", () => {
    it.todo("should call /zones/:id/dns_records");
    it.todo("should filter by type and name when provided");
  });

  describe("create_dns_record", () => {
    it.todo("should POST to /zones/:id/dns_records");
    it.todo("should include priority for MX records");
  });

  describe("update_dns_record", () => {
    it.todo("should PATCH /zones/:id/dns_records/:recordId with changed fields only");
  });

  describe("delete_dns_record", () => {
    it.todo("should DELETE /zones/:id/dns_records/:recordId");
  });
});
