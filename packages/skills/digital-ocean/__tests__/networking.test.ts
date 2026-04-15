import { describe, it } from "bun:test";

describe("networking", () => {
  describe("list_firewalls", () => {
    it.todo("should GET /firewalls with per_page");
    it.todo("should return empty arrays for missing rules/droplets/tags");
  });

  describe("create_firewall", () => {
    it.todo("should parse inbound_rules and outbound_rules from JSON strings");
    it.todo("should include droplet_ids when provided");
    it.todo("should return id, name, status, created_at");
  });

  describe("list_load_balancers", () => {
    it.todo("should GET /load_balancers with per_page");
    it.todo("should extract region from region.slug");
  });

  describe("create_load_balancer", () => {
    it.todo("should parse forwarding_rules from JSON string");
    it.todo("should parse health_check when provided");
    it.todo("should include tag when provided");
  });

  describe("list_floating_ips", () => {
    it.todo("should GET /floating_ips with per_page");
    it.todo("should return null droplet when absent");
    it.todo("should return false for missing locked field");
  });
});
