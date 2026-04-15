import { describe, it, expect } from "bun:test";
import { crud } from "../cli/crud.ts";

describe("crud actions", () => {
  it("should export expected action keys", () => {
    expect(Object.keys(crud).sort()).toEqual([
      "append_note",
      "create_note",
      "delete_note",
      "prepend_note",
      "read_note",
      "write_note",
    ]);
  });

  for (const [name, action] of Object.entries(crud)) {
    describe(name, () => {
      it("should have a description", () => {
        expect(action.description).toBeTruthy();
      });

      it("should have a params schema", () => {
        expect(action.params).toBeDefined();
      });

      it("should have an execute function", () => {
        expect(typeof action.execute).toBe("function");
      });
    });
  }
});
