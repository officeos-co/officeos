import { describe, it } from "bun:test";

describe("exa", () => {
  describe("actions", () => {
    it.todo("should expose search action");
    it.todo("should expose find_similar action");
    it.todo("should expose get_contents action");
    it.todo("should expose search_and_contents action");
  });

  describe("params", () => {
    describe("search", () => {
      it.todo("should require query");
      it.todo("should accept optional num_results between 1 and 100, defaulting to 10");
      it.todo("should accept optional type: neural or keyword, defaulting to neural");
      it.todo("should accept optional include_domains array");
      it.todo("should accept optional exclude_domains array");
      it.todo("should accept optional start_crawl_date and end_crawl_date ISO strings");
      it.todo("should accept optional start_published_date and end_published_date ISO strings");
      it.todo("should accept optional include_text boolean");
      it.todo("should accept optional highlights boolean");
      it.todo("should accept optional category string");
    });

    describe("find_similar", () => {
      it.todo("should require url");
      it.todo("should accept optional num_results defaulting to 10");
      it.todo("should accept optional exclude_source_domain defaulting to true");
      it.todo("should accept optional include_domains and exclude_domains");
      it.todo("should accept optional include_text");
    });

    describe("get_contents", () => {
      it.todo("should require ids array");
      it.todo("should accept optional text defaulting to true");
      it.todo("should accept optional highlights");
      it.todo("should accept optional max_chars");
    });

    describe("search_and_contents", () => {
      it.todo("should require query");
      it.todo("should accept optional num_results between 1 and 20, defaulting to 5");
      it.todo("should accept optional max_chars defaulting to 3000");
      it.todo("should accept optional type, include_domains, exclude_domains");
      it.todo("should accept optional start_published_date and end_published_date");
      it.todo("should accept optional category");
    });
  });

  describe("execute", () => {
    describe("search", () => {
      it.todo("should POST to https://api.exa.ai/search");
      it.todo("should send x-api-key header");
      it.todo("should map num_results to numResults in body");
      it.todo("should include contents with text when include_text is true");
      it.todo("should include contents with highlights when highlights is true");
      it.todo("should return results array with id, url, title, score");
      it.todo("should throw on non-200 response");
    });

    describe("find_similar", () => {
      it.todo("should POST to https://api.exa.ai/findSimilar");
      it.todo("should send url and numResults in body");
      it.todo("should set excludeSourceDomain to true by default");
      it.todo("should include contents when include_text is true");
      it.todo("should return results array");
    });

    describe("get_contents", () => {
      it.todo("should POST to https://api.exa.ai/contents");
      it.todo("should send ids array in body");
      it.todo("should include text contents by default");
      it.todo("should include highlights when requested");
      it.todo("should pass max_chars as maxCharacters");
      it.todo("should return results with text field");
    });

    describe("search_and_contents", () => {
      it.todo("should POST to https://api.exa.ai/search with contents field");
      it.todo("should include text contents with maxCharacters");
      it.todo("should return results with text field populated");
    });
  });
});
