import { describe, it } from "bun:test";

describe("teams", () => {
  describe("list_teams", () => {
    it.todo("should call /v2/teams with limit param");
    it.todo("should return mapped team array");
  });

  describe("get_team", () => {
    it.todo("should call /v2/teams/:id");
    it.todo("should return team with updatedAt fallback");
  });
});
