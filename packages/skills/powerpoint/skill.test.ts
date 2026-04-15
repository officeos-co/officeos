import { describe, it } from "bun:test";

describe("powerpoint skill", () => {
  describe("create", () => {
    it.todo("should POST /presentations with filename, title, author");
    it.todo("should default width_px to 1280 and height_px to 720");
    it.todo("should return file_id and created_at");
    it.todo("should throw on proxy error");
  });

  describe("open", () => {
    it.todo("should GET /presentations/:file_id");
    it.todo("should return slide_count and dimensions");
    it.todo("should throw on 404 for unknown file_id");
  });

  describe("save", () => {
    it.todo("should POST /presentations/:file_id/save");
    it.todo("should return size_bytes and saved_at");
  });

  describe("get_info", () => {
    it.todo("should GET /presentations/:file_id/info");
    it.todo("should return width_px, height_px, slide_count");
  });

  describe("add_slide", () => {
    it.todo("should POST /presentations/:file_id/slides");
    it.todo("should default layout to BLANK");
    it.todo("should accept optional position");
    it.todo("should return slide_index and slide_id");
  });

  describe("delete_slide", () => {
    it.todo("should DELETE /presentations/:file_id/slides/:index");
    it.todo("should return updated slide_count");
  });

  describe("reorder_slide", () => {
    it.todo("should POST /presentations/:file_id/slides/reorder with from_index and to_index");
    it.todo("should return slide_count");
  });

  describe("duplicate_slide", () => {
    it.todo("should POST /presentations/:file_id/slides/:index/duplicate");
    it.todo("should return new_slide_index");
  });

  describe("add_text", () => {
    it.todo("should POST text to the correct slide endpoint");
    it.todo("should accept font_size, bold, italic, color, align");
    it.todo("should default font_size to 24");
    it.todo("should return element_id");
  });

  describe("add_image", () => {
    it.todo("should POST image_url and position to slide endpoint");
    it.todo("should accept alt_text");
    it.todo("should return element_id");
  });

  describe("add_chart", () => {
    it.todo("should POST chart_type, categories, and series to slide endpoint");
    it.todo("should accept all supported chart types");
    it.todo("should return element_id");
  });

  describe("add_shape", () => {
    it.todo("should POST shape_type and position to slide endpoint");
    it.todo("should accept fill_color, line_color, text");
    it.todo("should return element_id");
  });

  describe("set_background", () => {
    it.todo("should POST color or image_url to slide background endpoint");
    it.todo("should accept both color and image_url");
  });

  describe("set_notes", () => {
    it.todo("should POST notes to slide notes endpoint");
    it.todo("should return slide_index");
  });

  describe("export_pdf", () => {
    it.todo("should POST /presentations/:file_id/export/pdf");
    it.todo("should return pdf_file_id and download_url");
  });

  describe("download", () => {
    it.todo("should GET /presentations/:file_id/download");
    it.todo("should return download_url and expires_at");
  });
});
