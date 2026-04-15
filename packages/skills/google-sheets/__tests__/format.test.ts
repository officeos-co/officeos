import { describe, it, expect } from "bun:test";
import { parseRange } from "../core/client.ts";

describe("format", () => {
  describe("parseRange", () => {
    it("should parse A1:D10 into GridRange", () => {
      const result = parseRange(0, "Sheet1!A1:D10");
      expect(result).toEqual({ sheetId: 0, startRowIndex: 0, endRowIndex: 10, startColumnIndex: 0, endColumnIndex: 4 });
    });

    it("should return { sheetId } for invalid A1 notation", () => {
      const result = parseRange(42, "invalid");
      expect(result).toEqual({ sheetId: 42 });
    });
  });

  describe("format_cells", () => {
    it.todo("should POST batchUpdate with repeatCell request");
    it.todo("should convert hex background color to RGB fractions");
    it.todo("should only include fields for provided formatting options");
  });

  describe("auto_resize", () => {
    it.todo("should POST batchUpdate with autoResizeDimensions request");
  });

  describe("merge_cells", () => {
    it.todo("should resolve sheet ID from range and POST mergeCells");
  });

  describe("sort_range", () => {
    it.todo("should POST sortRange with ASCENDING by default");
    it.todo("should use DESCENDING when ascending=false");
  });

  describe("add_filter", () => {
    it.todo("should POST setBasicFilter with range");
  });

  describe("remove_filter", () => {
    it.todo("should POST clearBasicFilter with sheetId");
  });
});
