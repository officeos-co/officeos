import { describe, it } from "bun:test";

describe("word skill", () => {
  describe("create", () => {
    it.todo("should POST /documents with filename, title, author");
    it.todo("should return file_id and created_at");
    it.todo("should throw on proxy error");
  });

  describe("open", () => {
    it.todo("should GET /documents/:file_id");
    it.todo("should return paragraph_count and section_count");
    it.todo("should throw on 404 for unknown file_id");
  });

  describe("save", () => {
    it.todo("should POST /documents/:file_id/save");
    it.todo("should return size_bytes and saved_at");
  });

  describe("get_text", () => {
    it.todo("should GET /documents/:file_id/text");
    it.todo("should pass include_formatting query param");
    it.todo("should return text string and paragraphs array");
  });

  describe("replace_text", () => {
    it.todo("should POST /documents/:file_id/replace with search and replace");
    it.todo("should return replacements_made count");
    it.todo("should respect match_case flag");
  });

  describe("add_paragraph", () => {
    it.todo("should POST /documents/:file_id/paragraphs with text and style");
    it.todo("should default style to Normal");
    it.todo("should accept optional position, bold, italic, font_size");
    it.todo("should return paragraph_index");
  });

  describe("add_table", () => {
    it.todo("should POST /documents/:file_id/tables with rows and cols");
    it.todo("should accept headers and data arrays");
    it.todo("should return table_index");
  });

  describe("add_image", () => {
    it.todo("should POST /documents/:file_id/images with image_url");
    it.todo("should accept width_cm, height_cm, alt_text, position");
    it.todo("should return image_index");
  });

  describe("add_heading", () => {
    it.todo("should POST /documents/:file_id/headings with text and level");
    it.todo("should default level to 1");
    it.todo("should reject level outside 1–6");
  });

  describe("add_page_break", () => {
    it.todo("should POST /documents/:file_id/page-breaks");
    it.todo("should return paragraph_index");
  });

  describe("delete_paragraph", () => {
    it.todo("should DELETE /documents/:file_id/paragraphs/:index");
    it.todo("should return updated paragraph_count");
  });

  describe("list_styles", () => {
    it.todo("should GET /documents/:file_id/styles with type query param");
    it.todo("should default type to paragraph");
    it.todo("should return array of name, type, base_style");
  });

  describe("set_properties", () => {
    it.todo("should PATCH /documents/:file_id/properties");
    it.todo("should accept partial properties (title only, etc.)");
  });

  describe("export_pdf", () => {
    it.todo("should POST /documents/:file_id/export/pdf");
    it.todo("should return pdf_file_id and download_url");
    it.todo("should accept optional output_filename");
  });

  describe("download", () => {
    it.todo("should GET /documents/:file_id/download");
    it.todo("should return download_url and expires_at");
  });
});
