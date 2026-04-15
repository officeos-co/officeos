import { describe, it } from "bun:test";

describe("pdf skill", () => {
  describe("create", () => {
    it.todo("should POST /documents with filename");
    it.todo("should return file_id and created_at");
    it.todo("should throw on proxy error");
  });

  describe("open", () => {
    it.todo("should GET /documents/:file_id");
    it.todo("should return page_count and encrypted flag");
    it.todo("should throw on 404 for unknown file_id");
  });

  describe("get_info", () => {
    it.todo("should GET /documents/:file_id/info");
    it.todo("should return page_sizes array with width and height per page");
    it.todo("should return metadata fields including creator and producer");
  });

  describe("save", () => {
    it.todo("should POST /documents/:file_id/save");
    it.todo("should return size_bytes and saved_at");
  });

  describe("extract_text", () => {
    it.todo("should GET /documents/:file_id/text");
    it.todo("should pass pages query param when specified");
    it.todo("should return text string and per-page array");
    it.todo("should omit pages param when not specified");
  });

  describe("add_page", () => {
    it.todo("should POST /documents/:file_id/pages with width and height");
    it.todo("should default to A4 dimensions (595x842 points)");
    it.todo("should accept named size to override dimensions");
    it.todo("should accept optional position for insertion");
    it.todo("should return page_index and updated page_count");
  });

  describe("add_text", () => {
    it.todo("should POST to /documents/:file_id/pages/:page_index/text");
    it.todo("should accept x, y coordinates, font, font_size, color");
    it.todo("should default font to Helvetica and font_size to 12");
    it.todo("should accept opacity between 0 and 1");
    it.todo("should return text_element_id");
  });

  describe("add_image", () => {
    it.todo("should POST to /documents/:file_id/pages/:page_index/images");
    it.todo("should accept image_url and optional size/position");
    it.todo("should return image_element_id");
  });

  describe("delete_page", () => {
    it.todo("should DELETE /documents/:file_id/pages/:page_index");
    it.todo("should return updated page_count");
  });

  describe("rotate", () => {
    it.todo("should POST /documents/:file_id/rotate with pages and degrees");
    it.todo("should accept 90, 180, or 270 degrees");
    it.todo("should reject invalid rotation angles");
    it.todo("should accept 'all' as pages value");
    it.todo("should return rotated_pages array");
  });

  describe("merge", () => {
    it.todo("should POST /documents/merge with ordered file_ids array");
    it.todo("should return combined page_count");
    it.todo("should accept optional output_filename");
  });

  describe("split", () => {
    it.todo("should POST /documents/:file_id/split with ranges array");
    it.todo("should return array of file objects with file_id and range");
  });

  describe("copy_pages", () => {
    it.todo("should POST /documents/copy-pages with source, target, and pages");
    it.todo("should accept optional insert_at index");
    it.todo("should return updated page_count for target");
  });

  describe("set_metadata", () => {
    it.todo("should PATCH /documents/:file_id/metadata");
    it.todo("should accept partial metadata fields");
    it.todo("should return updated metadata object");
  });

  describe("download", () => {
    it.todo("should GET /documents/:file_id/download");
    it.todo("should return download_url and expires_at");
  });
});
