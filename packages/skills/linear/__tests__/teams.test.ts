import { describe, it } from "bun:test";

describe("teams", () => {
  describe("list_teams", () => {
    it.todo("should return all teams with id, name, key, description, members_count");
  });

  describe("get_team", () => {
    it.todo("should return members as name strings");
    it.todo("should return states as name strings");
    it.todo("should set cycles_enabled based on cycles nodes length");
  });

  describe("list_workflow_states", () => {
    it.todo("should return id, name, type, color, position");
    it.todo("should use team id variable in query");
  });
});
