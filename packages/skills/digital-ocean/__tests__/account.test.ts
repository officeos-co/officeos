import { describe, it } from "bun:test";

describe("account", () => {
  describe("get_account", () => {
    it.todo("should GET /account");
    it.todo("should return droplet_limit and floating_ip_limit");
    it.todo("should return null for missing team");
  });

  describe("list_ssh_keys", () => {
    it.todo("should GET /account/keys with per_page");
    it.todo("should return id, name, fingerprint, public_key");
  });

  describe("add_ssh_key", () => {
    it.todo("should POST to /account/keys with name and public_key");
    it.todo("should return id, name, fingerprint");
  });

  describe("list_regions", () => {
    it.todo("should GET /regions");
    it.todo("should return available boolean and features array");
  });

  describe("list_sizes", () => {
    it.todo("should GET /sizes");
    it.todo("should return price_monthly and price_hourly");
  });

  describe("list_images", () => {
    it.todo("should filter by type when provided");
    it.todo("should return null for missing slug");
    it.todo("should return 0 for missing size_gigabytes");
  });
});
