import { describe, it } from "bun:test";

describe("kubernetes", () => {
  describe("list_clusters", () => {
    it.todo("should GET /kubernetes/clusters with per_page");
    it.todo("should extract status from status.state");
  });

  describe("get_cluster", () => {
    it.todo("should return ipv4 as null when absent");
    it.todo("should return maintenance_policy");
  });

  describe("create_cluster", () => {
    it.todo("should build node_pools array from params");
    it.todo("should include vpc_uuid when provided");
    it.todo("should return endpoint as null when absent");
  });

  describe("delete_cluster", () => {
    it.todo("should DELETE /kubernetes/clusters/:id");
    it.todo("should return deleted: true");
  });

  describe("get_kubeconfig", () => {
    it.todo("should fetch kubeconfig as text");
    it.todo("should use x-kubeconfig-expires header for expires_at");
    it.todo("should default expires_at to 7 days when header absent");
  });

  describe("list_node_pools", () => {
    it.todo("should GET /kubernetes/clusters/:id/node_pools");
    it.todo("should return auto_scale as false when absent");
  });
});
