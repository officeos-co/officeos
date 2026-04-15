import { describe, it } from "bun:test";

describe("web-search", () => {
  describe("search", () => {
    it.todo("should call /search with query and format=json");
    it.todo("should pass categories as comma-separated param");
    it.todo("should pass engines as comma-separated param");
    it.todo("should pass language param");
    it.todo("should handle pagination via pageno param");
    it.todo("should respect limit and truncate results");
    it.todo("should pass time_range param when provided");
    it.todo("should pass safesearch param");
    it.todo("should throw on non-ok response");
  });

  describe("search_images", () => {
    it.todo("should search with categories=images");
    it.todo("should return img_src and thumbnail_src fields");
  });

  describe("search_news", () => {
    it.todo("should search with categories=news");
    it.todo("should pass time_range filter");
  });

  describe("get_engines", () => {
    it.todo("should call /config and return engines array");
    it.todo("should map engine fields correctly");
  });

  describe("get_categories", () => {
    it.todo("should call /config and return categories array");
  });

  describe("autocomplete", () => {
    it.todo("should call /autocompleter with query param");
    it.todo("should return array of suggestion strings");
  });
});
