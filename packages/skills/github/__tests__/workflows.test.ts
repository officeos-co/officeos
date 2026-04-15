import { describe, it } from "bun:test";

describe("workflows", () => {
  describe("list_workflows", () => {
    it.todo("should call /actions/workflows and return workflows array");
  });

  describe("list_workflow_runs", () => {
    it.todo("should call /actions/workflows/{id}/runs");
    it.todo("should add &status= param when status filter provided");
  });

  describe("trigger_workflow", () => {
    it.todo("should POST to /actions/workflows/{id}/dispatches with ref and inputs");
    it.todo("should return { triggered: true } on 204 response");
    it.todo("should throw on non-ok response");
  });
});
