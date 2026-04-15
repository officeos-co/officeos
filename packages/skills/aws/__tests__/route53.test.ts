import { describe, it } from "bun:test";

describe("route53", () => {
  describe("list_hosted_zones", () => {
    it.todo("should strip /hostedzone/ prefix from zone_id");
    it.todo("should parse private_zone boolean");
    it.todo("should return record_count as integer");
  });

  describe("list_records", () => {
    it.todo("should append ?type filter when provided");
    it.todo("should parse alias_target block");
    it.todo("should return null alias_target when not present");
  });

  describe("create_record", () => {
    it.todo("should use AliasTarget block when alias_target provided");
    it.todo("should use TTL+ResourceRecords when value provided");
    it.todo("should return change_id without /change/ prefix");
  });

  describe("delete_record", () => {
    it.todo("should send DELETE action in XML body");
    it.todo("should default TTL to 300 when not provided");
  });
});
