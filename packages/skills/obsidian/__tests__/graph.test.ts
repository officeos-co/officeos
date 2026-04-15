import { describe, it, expect } from "bun:test";
import { graph } from "../cli/graph.ts";

describe("graph actions", () => {
  it("should export expected action keys", () => {
    expect(Object.keys(graph).sort()).toEqual([
      "find_orphans",
      "find_unresolved",
      "get_backlinks",
      "get_links",
    ]);
  });

  for (const [name, action] of Object.entries(graph)) {
    describe(name, () => {
      it("should have a description", () => {
        expect(action.description).toBeTruthy();
      });

      it("should have an execute function", () => {
        expect(typeof action.execute).toBe("function");
      });
    });
  }
});
