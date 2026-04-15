import { describe, it } from "bun:test";

describe("domains", () => {
  describe("list_domains", () => {
    it.todo("should GET /domains with per_page");
    it.todo("should return zone_file as null when absent");
  });

  describe("get_domain", () => {
    it.todo("should URL-encode domain name");
  });

  describe("create_domain", () => {
    it.todo("should include ip_address when provided");
    it.todo("should return name and ttl");
  });

  describe("delete_domain", () => {
    it.todo("should DELETE /domains/:name");
    it.todo("should return deleted: true");
  });

  describe("list_records", () => {
    it.todo("should filter by type when provided");
    it.todo("should return null for priority/port/weight when absent");
  });

  describe("create_record", () => {
    it.todo("should POST with required type, name, data, ttl");
    it.todo("should include priority for MX records");
  });

  describe("update_record", () => {
    it.todo("should PUT only provided fields");
  });

  describe("delete_record", () => {
    it.todo("should DELETE /domains/:name/records/:id");
  });
});
