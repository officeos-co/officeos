import { describe, it } from "bun:test";

describe("sqs", () => {
  describe("list_queues", () => {
    it.todo("should call ListQueues without prefix when omitted");
    it.todo("should pass QueueNamePrefix when provided");
    it.todo("should extract queue_name from URL");
  });

  describe("send_message", () => {
    it.todo("should set DelaySeconds param");
    it.todo("should set MessageGroupId for FIFO queues");
    it.todo("should return message_id and md5_of_body");
  });

  describe("receive_messages", () => {
    it.todo("should call ReceiveMessage with MaxNumberOfMessages");
    it.todo("should set VisibilityTimeout when provided");
    it.todo("should return receipt_handle");
  });

  describe("delete_message", () => {
    it.todo("should call DeleteMessage with ReceiptHandle");
    it.todo("should return deleted: true");
  });

  describe("create_queue", () => {
    it.todo("should append .fifo suffix when fifo=true and name lacks it");
    it.todo("should set FifoQueue attribute when fifo=true");
    it.todo("should return queue_url");
  });
});
