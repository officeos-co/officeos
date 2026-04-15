import { describe, it } from "bun:test";

describe("pubsub", () => {
  describe("publish", () => {
    it.todo("should send PUBLISH command with channel and message");
    it.todo("should return number of receivers");
  });

  describe("subscribe", () => {
    it.todo("should POST to proxy /subscribe endpoint");
    it.todo("should include parsed channels array and timeout");
    it.todo("should return messages array from response");
  });
});
