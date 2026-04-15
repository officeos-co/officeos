import { describe, it } from "bun:test";

describe("sns", () => {
  describe("list_topics", () => {
    it.todo("should call ListTopics");
    it.todo("should extract topic_name from ARN");
  });

  describe("publish", () => {
    it.todo("should call Publish with TopicArn and Message");
    it.todo("should add Subject when provided");
    it.todo("should return message_id");
  });

  describe("subscribe", () => {
    it.todo("should call Subscribe with protocol and endpoint");
    it.todo("should return subscription_arn");
  });

  describe("create_topic", () => {
    it.todo("should append .fifo suffix for FIFO topics");
    it.todo("should set FifoTopic attribute when fifo=true");
    it.todo("should attach tags when provided");
    it.todo("should return topic_arn");
  });
});
