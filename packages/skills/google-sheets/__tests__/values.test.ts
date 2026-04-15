import { describe, it } from "bun:test";

describe("values", () => {
  describe("get_values", () => {
    it.todo("should GET /spreadsheets/:id/values/:range");
    it.todo("should return empty values array when no data");
  });

  describe("batch_get_values", () => {
    it.todo("should GET with multiple ranges param");
    it.todo("should map valueRanges to output format");
  });

  describe("update_values", () => {
    it.todo("should PUT to /spreadsheets/:id/values/:range with valueInputOption");
    it.todo("should parse JSON values string");
  });

  describe("append_values", () => {
    it.todo("should POST to /spreadsheets/:id/values/:range:append");
    it.todo("should return updates.updatedRange");
  });

  describe("batch_update_values", () => {
    it.todo("should POST to /spreadsheets/:id/values:batchUpdate");
    it.todo("should parse JSON data string");
  });

  describe("clear_values", () => {
    it.todo("should POST to /spreadsheets/:id/values/:range:clear");
  });

  describe("batch_clear_values", () => {
    it.todo("should POST to /spreadsheets/:id/values:batchClear with ranges array");
  });
});
