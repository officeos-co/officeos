import { describe, it } from "bun:test";

describe("droplets", () => {
  describe("list_droplets", () => {
    it.todo("should filter by tag_name when tag provided");
    it.todo("should map network v4 public IP");
    it.todo("should return null ip when no public network");
  });

  describe("get_droplet", () => {
    it.todo("should fetch /droplets/:id");
    it.todo("should return volume_ids as volumes array");
  });

  describe("create_droplet", () => {
    it.todo("should POST with required fields");
    it.todo("should include ssh_keys when provided");
    it.todo("should include vpc_uuid when provided");
  });

  describe("delete_droplet", () => {
    it.todo("should DELETE /droplets/:id");
    it.todo("should return deleted: true");
  });

  describe("power_on", () => {
    it.todo("should POST action type power_on");
    it.todo("should return action_id and status");
  });

  describe("power_off", () => {
    it.todo("should POST action type power_off");
  });

  describe("reboot", () => {
    it.todo("should POST action type reboot");
  });

  describe("resize", () => {
    it.todo("should POST with size and disk params");
  });

  describe("snapshot", () => {
    it.todo("should POST with type snapshot and name");
  });
});
