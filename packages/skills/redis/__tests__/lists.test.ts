import { describe, it } from "bun:test";

describe("lists", () => {
  describe("lpush", () => {
    it.todo("should send LPUSH with parsed values array");
    it.todo("should return new list length");
  });

  describe("rpush", () => {
    it.todo("should send RPUSH with parsed values array");
  });

  describe("lpop", () => {
    it.todo("should send LPOP with count");
    it.todo("should wrap single result in array");
  });

  describe("rpop", () => {
    it.todo("should send RPOP with count");
  });

  describe("lrange", () => {
    it.todo("should send LRANGE with start and stop");
    it.todo("should default stop to -1");
  });

  describe("llen", () => {
    it.todo("should send LLEN and return length");
  });
});
