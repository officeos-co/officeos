import { describe, it } from "bun:test";

describe("sets", () => {
  describe("sadd", () => {
    it.todo("should send SADD with parsed members array");
    it.todo("should return added count");
  });

  describe("srem", () => {
    it.todo("should send SREM with parsed members array");
  });

  describe("smembers", () => {
    it.todo("should send SMEMBERS and return all members");
  });

  describe("sismember", () => {
    it.todo("should return true when result is 1");
    it.todo("should return false when result is 0");
  });

  describe("scard", () => {
    it.todo("should send SCARD and return count");
  });

  describe("sunion", () => {
    it.todo("should send SUNION with parsed keys array");
  });

  describe("sinter", () => {
    it.todo("should send SINTER with parsed keys array");
  });

  describe("sdiff", () => {
    it.todo("should send SDIFF with parsed keys array");
  });
});
