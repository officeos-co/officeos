import { describe, it } from "bun:test";

describe("keys", () => {
  describe("keys", () => {
    it.todo("should send KEYS with pattern");
    it.todo("should default pattern to *");
  });

  describe("del", () => {
    it.todo("should send DEL with parsed keys array");
    it.todo("should return deleted count");
  });

  describe("exists", () => {
    it.todo("should send EXISTS and return boolean");
  });

  describe("expire", () => {
    it.todo("should send EXPIRE with seconds");
    it.todo("should return false when key does not exist");
  });

  describe("ttl", () => {
    it.todo("should send TTL and return remaining seconds");
    it.todo("should return -1 for persistent keys");
    it.todo("should return -2 for non-existent keys");
  });

  describe("type", () => {
    it.todo("should send TYPE and return type string");
  });

  describe("rename", () => {
    it.todo("should send RENAME with old and new key names");
  });
});
