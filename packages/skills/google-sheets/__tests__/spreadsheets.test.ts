import { describe, it } from "bun:test";

describe("spreadsheets", () => {
  describe("create_spreadsheet", () => {
    it.todo("should POST to Sheets API with title property");
    it.todo("should return spreadsheetId, url, and sheet names");
  });

  describe("get_spreadsheet", () => {
    it.todo("should GET spreadsheet metadata");
    it.todo("should return all sheet properties");
  });

  describe("list_sheets", () => {
    it.todo("should GET with fields=sheets.properties filter");
    it.todo("should return sheet tab list with row/column counts");
  });
});
