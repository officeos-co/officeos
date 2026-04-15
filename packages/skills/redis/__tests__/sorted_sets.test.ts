import { describe, it } from "bun:test";

describe("sorted_sets", () => {
  describe("zadd", () => {
    it.todo("should send ZADD with score-value pairs");
    it.todo("should include NX flag when nx=true");
    it.todo("should include XX flag when xx=true");
  });

  describe("zrem", () => {
    it.todo("should send ZREM with parsed members array");
  });

  describe("zrange", () => {
    it.todo("should send ZRANGE by default");
    it.todo("should send ZREVRANGE when rev=true");
    it.todo("should include WITHSCORES when withscores=true");
  });

  describe("zrangebyscore", () => {
    it.todo("should send ZRANGEBYSCORE with min, max");
    it.todo("should include LIMIT clause when limit is provided");
  });

  describe("zscore", () => {
    it.todo("should send ZSCORE and return numeric score");
    it.todo("should return null when member does not exist");
  });

  describe("zcard", () => {
    it.todo("should send ZCARD and return count");
  });
});
