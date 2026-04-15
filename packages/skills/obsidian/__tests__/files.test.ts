import { describe, it, expect } from "bun:test";
import { files } from "../cli/files.ts";

describe("files actions", () => {
  it("should export expected action keys", () => {
    expect(Object.keys(files).sort()).toEqual([
      "list_files",
      "list_folders",
      "move_note",
      "rename_note",
    ]);
  });

  for (const [name, action] of Object.entries(files)) {
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
