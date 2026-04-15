import { describe, it, expect } from "bun:test";

describe("pods", () => {
  describe("list_pods", () => {
    it.todo("should call /api/v1/namespaces/{ns}/pods with label selector");
    it.todo("should map container statuses to ready/restarts fields");
    it.todo("should default namespace to 'default' when not provided");
  });

  describe("get_pod", () => {
    it.todo("should fetch single pod by name and namespace");
    it.todo("should return containers, volumes, status, and conditions");
  });

  describe("delete_pod", () => {
    it.todo("should call DELETE with gracePeriodSeconds param");
    it.todo("should return name and status=deleted");
  });

  describe("logs", () => {
    it.todo("should call /log endpoint with tailLines and previous params");
    it.todo("should parse duration (e.g. 1h) to sinceSeconds");
    it.todo("should return log text as string");
  });

  describe("exec", () => {
    it.todo("should split command string and append as multiple command params");
    it.todo("should return exit_code based on response ok status");
  });

  describe("describe_pod", () => {
    it.todo("should fetch pod and events by involvedObject.name");
    it.todo("should map resource requests and limits from container specs");
  });
});
