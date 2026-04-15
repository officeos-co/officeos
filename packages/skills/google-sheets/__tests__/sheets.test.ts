import { describe, it } from "bun:test";

describe("sheets", () => {
  describe("add_sheet", () => {
    it.todo("should POST batchUpdate with addSheet request");
    it.todo("should return sheetId from reply");
  });

  describe("delete_sheet", () => {
    it.todo("should POST batchUpdate with deleteSheet request");
  });

  describe("rename_sheet", () => {
    it.todo("should POST batchUpdate with updateSheetProperties request");
    it.todo("should set fields to title");
  });

  describe("duplicate_sheet", () => {
    it.todo("should POST batchUpdate with duplicateSheet request");
    it.todo("should include newSheetName when new_title is provided");
  });
});
