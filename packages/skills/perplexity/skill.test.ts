import { describe, it } from "bun:test";

describe("perplexity", () => {
  describe("actions", () => {
    it.todo("should expose search action");
    it.todo("should expose chat action");
    it.todo("should expose search_news action");
    it.todo("should expose search_academic action");
  });

  describe("params", () => {
    describe("search", () => {
      it.todo("should require query");
      it.todo("should accept optional model defaulting to sonar");
      it.todo("should accept optional search_domain_filter as comma-separated string");
      it.todo("should accept optional return_images defaulting to false");
      it.todo("should accept optional return_related defaulting to false");
      it.todo("should accept optional recency: month, week, day, hour");
    });

    describe("chat", () => {
      it.todo("should require messages array");
      it.todo("should accept optional model defaulting to sonar");
      it.todo("should accept optional temperature between 0 and 2");
      it.todo("should accept optional max_tokens");
    });

    describe("search_news", () => {
      it.todo("should require query");
      it.todo("should accept optional recency defaulting to week");
      it.todo("should accept optional model");
    });

    describe("search_academic", () => {
      it.todo("should require query");
      it.todo("should accept optional search_domain_filter");
      it.todo("should accept optional model");
    });
  });

  describe("execute", () => {
    describe("search", () => {
      it.todo("should POST to https://api.perplexity.ai/chat/completions");
      it.todo("should send Authorization Bearer header");
      it.todo("should pass search_domain_filter as array when provided");
      it.todo("should return answer from choices[0].message.content");
      it.todo("should return citations array");
      it.todo("should return usage with prompt_tokens and completion_tokens");
      it.todo("should throw on non-200 response");
    });

    describe("chat", () => {
      it.todo("should pass full messages array to API");
      it.todo("should include temperature when specified");
      it.todo("should include max_tokens when specified");
      it.todo("should return content, role, model, citations, usage");
    });

    describe("search_news", () => {
      it.todo("should set search_recency_filter to week by default");
      it.todo("should override recency when provided");
      it.todo("should return answer and citations");
    });

    describe("search_academic", () => {
      it.todo("should pass domain filter array when provided");
      it.todo("should return answer and citations");
    });
  });
});
