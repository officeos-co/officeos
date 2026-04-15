import { describe, it } from "bun:test";

describe("cycles", () => {
  describe("list_cycles", () => {
    it.todo("should query team cycles by team id");
    it.todo("should return id, name, number, starts_at, ends_at, progress");
  });

  describe("get_cycle", () => {
    it.todo("should return scope, completed_scope, and issues identifiers");
  });

  describe("get_active_cycle", () => {
    it.todo("should return null fields when no active cycle exists");
    it.todo("should return cycle data when active cycle exists");
  });

  describe("add_issue_to_cycle", () => {
    it.todo("should mutation issueUpdate with cycleId input");
    it.todo("should return issue_identifier and cycle_name");
  });

  describe("remove_issue_from_cycle", () => {
    it.todo("should mutation issueUpdate with cycleId: null");
  });
});
