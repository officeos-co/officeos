import { describe, it } from "bun:test";

describe("nodes", () => {
  describe("list_nodes", () => {
    it.todo("should parse node-role.kubernetes.io/* labels into roles string");
    it.todo("should map Ready condition to 'Ready' or 'NotReady' status");
  });

  describe("describe_node", () => {
    it.todo("should fetch node details, pods on node, and node events");
  });

  describe("cordon / uncordon", () => {
    it.todo("should PATCH node spec.unschedulable=true for cordon");
    it.todo("should PATCH node spec.unschedulable=false for uncordon");
  });

  describe("drain", () => {
    it.todo("should cordon node first then evict all non-DaemonSet pods");
    it.todo("should skip DaemonSet pods when ignore_daemonsets=true");
    it.todo("should return list of evicted pod names");
  });
});
