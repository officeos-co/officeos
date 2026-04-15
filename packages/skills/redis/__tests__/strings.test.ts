import { describe, it } from "bun:test";

describe("strings", () => {
  describe("connect", () => {
    it.todo("should POST to proxy /command with INFO server and connection params");
    it.todo("should parse redis_version from INFO response");
  });

  describe("get", () => {
    it.todo("should send GET command to proxy");
    it.todo("should return null when key does not exist");
  });

  describe("set", () => {
    it.todo("should send SET command with key and value");
    it.todo("should include EX when ex is provided");
    it.todo("should include NX when nx is true");
  });

  describe("mget", () => {
    it.todo("should send MGET with parsed keys array");
    it.todo("should zip keys and values in response");
  });

  describe("mset", () => {
    it.todo("should send MSET with interleaved key-value args");
  });

  describe("incr", () => {
    it.todo("should send INCR for by=1");
    it.todo("should send INCRBY for by>1");
  });

  describe("decr", () => {
    it.todo("should send DECR for by=1");
    it.todo("should send DECRBY for by>1");
  });

  describe("append", () => {
    it.todo("should send APPEND command");
    it.todo("should return new string length");
  });
});
