import { describe, it } from "bun:test";

describe("server", () => {
  describe("info", () => {
    it.todo("should send INFO with no args by default");
    it.todo("should send INFO with section when provided");
    it.todo("should parse INFO response into key-value record");
    it.todo("should skip comment lines starting with #");
  });

  describe("dbsize", () => {
    it.todo("should send DBSIZE and return key_count");
  });

  describe("flushdb", () => {
    it.todo("should send FLUSHDB with no args by default");
    it.todo("should send FLUSHDB ASYNC when async=true");
  });

  describe("ping", () => {
    it.todo("should send PING and return pong string");
    it.todo("should fallback to PONG when result is undefined");
  });
});
