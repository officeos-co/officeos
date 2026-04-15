import { describe, it } from "bun:test";

describe("deployments", () => {
  describe("list_deployments", () => {
    it.todo("should list deployments in given namespace");
    it.todo("should pass labelSelector query param when provided");
    it.todo("should map ready replicas as 'readyReplicas/replicas' string");
  });

  describe("get_deployment", () => {
    it.todo("should fetch single deployment and return spec fields");
  });

  describe("create_deployment", () => {
    it.todo("should POST manifest to /apis/apps/v1/namespaces/{ns}/deployments");
    it.todo("should parse env JSON and convert to [{name, value}] array");
    it.todo("should default labels to {app: name}");
  });

  describe("scale", () => {
    it.todo("should PUT scale subresource with desired replicas");
    it.todo("should return previous_replicas from current spec");
  });

  describe("rollout_restart", () => {
    it.todo("should PATCH deployment with restartedAt annotation");
    it.todo("should return ISO timestamp");
  });

  describe("rollout_undo", () => {
    it.todo("should POST to /rollback endpoint");
    it.todo("should use revision 0 when no revision specified");
  });

  describe("delete_deployment", () => {
    it.todo("should DELETE /apis/apps/v1/namespaces/{ns}/deployments/{name}");
  });
});
