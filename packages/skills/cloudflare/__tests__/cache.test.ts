import { describe, it } from "bun:test";

describe("cache", () => {
  describe("purge_cache", () => {
    it.todo("should POST purge_everything=true when flag is set");
    it.todo("should POST specific files when provided");
    it.todo("should POST tags when provided");
  });

  describe("cache_settings", () => {
    it.todo("should PATCH browser_cache_ttl when provided");
    it.todo("should PATCH cache_level when provided");
    it.todo("should return current settings after updates");
  });
});
