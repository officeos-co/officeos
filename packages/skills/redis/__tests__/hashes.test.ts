import { describe, it } from "bun:test";

describe("hashes", () => {
  describe("hget", () => {
    it.todo("should send HGET with key and field");
    it.todo("should return null when field does not exist");
  });

  describe("hset", () => {
    it.todo("should send HSET with interleaved field-value args");
    it.todo("should parse JSON fields object");
  });

  describe("hgetall", () => {
    it.todo("should send HGETALL and return record");
  });

  describe("hdel", () => {
    it.todo("should send HDEL with parsed fields array");
    it.todo("should return removed count");
  });

  describe("hkeys", () => {
    it.todo("should send HKEYS and return field names");
  });

  describe("hvals", () => {
    it.todo("should send HVALS and return field values");
  });
});
