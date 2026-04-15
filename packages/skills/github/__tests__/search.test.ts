import { describe, it } from "bun:test";

describe("search", () => {
  describe("search_repos", () => {
    it.todo("should call /search/repositories with q and per_page");
    it.todo("should omit sort param when sort is best-match");
    it.todo("should return total_count and items array");
  });

  describe("search_issues", () => {
    it.todo("should call /search/issues with q param");
    it.todo("should set is_pr based on presence of pull_request field");
  });

  describe("search_code", () => {
    it.todo("should call /search/code with q param");
    it.todo("should return repository full_name from nested repository object");
  });
});
