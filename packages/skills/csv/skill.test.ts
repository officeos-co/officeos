import { describe, it } from "bun:test";

describe("csv skill", () => {
  describe("parse", () => {
    it.todo("should POST /datasets/parse with content and delimiter");
    it.todo("should default delimiter to comma");
    it.todo("should default has_header to true");
    it.todo("should return file_id, row_count, column_count, columns");
    it.todo("should throw on proxy error");
  });

  describe("parse_file", () => {
    it.todo("should POST /datasets/parse-file with file_url");
    it.todo("should return file_id and columns");
  });

  describe("stringify", () => {
    it.todo("should POST /datasets/:file_id/stringify");
    it.todo("should default include_header to true");
    it.todo("should default quote_all to false");
    it.todo("should return content string");
  });

  describe("get_columns", () => {
    it.todo("should GET /datasets/:file_id/columns");
    it.todo("should return columns array and counts");
  });

  describe("get_rows", () => {
    it.todo("should GET /datasets/:file_id/rows with offset and limit");
    it.todo("should default offset to 0 and limit to 100");
    it.todo("should return rows array and total_rows");
  });

  describe("stats", () => {
    it.todo("should POST /datasets/:file_id/stats");
    it.todo("should accept optional columns array");
    it.todo("should return per-column min, max, mean, median, stddev, null_count, unique_count");
    it.todo("should return null for non-numeric stat fields on string columns");
  });

  describe("filter_rows", () => {
    it.todo("should POST /datasets/:file_id/filter with column, operator, value");
    it.todo("should accept all supported operators");
    it.todo("should allow omitting value for is_empty/is_not_empty");
    it.todo("should return a new file_id");
  });

  describe("sort", () => {
    it.todo("should POST /datasets/:file_id/sort with column and direction");
    it.todo("should default direction to asc");
    it.todo("should accept numeric flag for numeric sorting");
    it.todo("should return a new file_id");
  });

  describe("add_column", () => {
    it.todo("should POST /datasets/:file_id/columns with name and expression");
    it.todo("should accept values array as alternative to expression");
    it.todo("should accept default value fallback");
    it.todo("should return updated columns array");
  });

  describe("rename_column", () => {
    it.todo("should POST /datasets/:file_id/columns/rename with old_name and new_name");
    it.todo("should return updated columns array");
  });

  describe("drop_columns", () => {
    it.todo("should POST /datasets/:file_id/columns/drop with columns array");
    it.todo("should return reduced columns list");
  });

  describe("transform", () => {
    it.todo("should POST /datasets/:file_id/transform with column and operation");
    it.todo("should accept all supported operations");
    it.todo("should require find and replace params for replace operation");
    it.todo("should return a new file_id");
  });

  describe("deduplicate", () => {
    it.todo("should POST /datasets/:file_id/deduplicate");
    it.todo("should accept optional columns subset");
    it.todo("should default keep to first");
    it.todo("should return row_count and removed_count");
  });

  describe("merge", () => {
    it.todo("should POST /datasets/merge with file_ids array");
    it.todo("should default how to union");
    it.todo("should require on column for join operations");
    it.todo("should return combined row_count and columns");
  });

  describe("to_json", () => {
    it.todo("should POST /datasets/:file_id/to-json with orientation");
    it.todo("should default orientation to records");
    it.todo("should return records as array of objects for records orientation");
    it.todo("should return object of arrays for columns orientation");
  });

  describe("download", () => {
    it.todo("should GET /datasets/:file_id/download");
    it.todo("should return download_url and expires_at");
  });
});
