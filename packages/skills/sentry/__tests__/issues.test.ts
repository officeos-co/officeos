import { describe, it } from "bun:test";

describe("issues", () => {
  describe("list_issues", () => {
    it.todo("should call /projects/{org}/{project}/issues/ with query, sort, limit");
    it.todo("should add cursor param when provided");
    it.todo("should map culprit to null when missing");
  });

  describe("resolve_issue", () => {
    it.todo("should PUT status: resolved");
    it.todo("should add statusDetails.inNextRelease when resolvedInNextRelease");
  });

  describe("ignore_issue", () => {
    it.todo("should PUT status: ignored with statusDetails");
    it.todo("should include ignoreCount, ignoreWindow, ignoreUntil when provided");
  });

  describe("assign_issue", () => {
    it.todo("should PUT assignedTo with provided value");
    it.todo("should send empty string to unassign");
  });

  describe("get_issue_frequency", () => {
    it.todo("should call /issues/{id}/stats/?stat=events");
    it.todo("should return array of { timestamp, count }");
  });
});
