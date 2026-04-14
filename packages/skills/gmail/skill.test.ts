import { describe, it } from "bun:test";

describe("gmail", () => {
  describe("actions", () => {
    // Messages
    it.todo("should expose list_messages action");
    it.todo("should expose get_message action");
    it.todo("should expose send_message action");
    it.todo("should expose reply_message action");
    it.todo("should expose forward_message action");
    it.todo("should expose delete_message action");
    it.todo("should expose trash_message action");
    it.todo("should expose untrash_message action");
    // Search
    it.todo("should expose search action");
    // Drafts
    it.todo("should expose list_drafts action");
    it.todo("should expose get_draft action");
    it.todo("should expose create_draft action");
    it.todo("should expose update_draft action");
    it.todo("should expose send_draft action");
    it.todo("should expose delete_draft action");
    // Labels
    it.todo("should expose list_labels action");
    it.todo("should expose get_label action");
    it.todo("should expose create_label action");
    it.todo("should expose update_label action");
    it.todo("should expose delete_label action");
    it.todo("should expose add_label action");
    it.todo("should expose remove_label action");
    // Threads
    it.todo("should expose list_threads action");
    it.todo("should expose get_thread action");
    it.todo("should expose trash_thread action");
    it.todo("should expose untrash_thread action");
    // Attachments
    it.todo("should expose get_attachment action");
    it.todo("should expose download_attachment action");
  });

  describe("params", () => {
    describe("list_messages", () => {
      it.todo("should accept optional query");
      it.todo("should accept optional label");
      it.todo("should accept optional max_results");
    });
    describe("get_message", () => {
      it.todo("should require message_id");
    });
    describe("send_message", () => {
      it.todo("should require to");
      it.todo("should require subject");
      it.todo("should require body");
      it.todo("should accept optional cc");
      it.todo("should accept optional bcc");
      it.todo("should accept optional attachments");
    });
    describe("reply_message", () => {
      it.todo("should require message_id");
      it.todo("should require body");
    });
    describe("forward_message", () => {
      it.todo("should require message_id");
      it.todo("should require to");
    });
    describe("delete_message", () => {
      it.todo("should require message_id");
    });
    describe("trash_message", () => {
      it.todo("should require message_id");
    });
    describe("untrash_message", () => {
      it.todo("should require message_id");
    });
    describe("search", () => {
      it.todo("should require query");
      it.todo("should accept optional max_results");
    });
    describe("list_drafts", () => {
      it.todo("should accept optional max_results");
    });
    describe("get_draft", () => {
      it.todo("should require draft_id");
    });
    describe("create_draft", () => {
      it.todo("should require to");
      it.todo("should require subject");
      it.todo("should require body");
    });
    describe("update_draft", () => {
      it.todo("should require draft_id");
    });
    describe("send_draft", () => {
      it.todo("should require draft_id");
    });
    describe("delete_draft", () => {
      it.todo("should require draft_id");
    });
    describe("get_label", () => {
      it.todo("should require label_id");
    });
    describe("create_label", () => {
      it.todo("should require name");
    });
    describe("update_label", () => {
      it.todo("should require label_id");
      it.todo("should require name");
    });
    describe("delete_label", () => {
      it.todo("should require label_id");
    });
    describe("add_label", () => {
      it.todo("should require message_id");
      it.todo("should require label_id");
    });
    describe("remove_label", () => {
      it.todo("should require message_id");
      it.todo("should require label_id");
    });
    describe("list_threads", () => {
      it.todo("should accept optional query");
      it.todo("should accept optional max_results");
    });
    describe("get_thread", () => {
      it.todo("should require thread_id");
    });
    describe("trash_thread", () => {
      it.todo("should require thread_id");
    });
    describe("untrash_thread", () => {
      it.todo("should require thread_id");
    });
    describe("get_attachment", () => {
      it.todo("should require message_id");
      it.todo("should require attachment_id");
    });
    describe("download_attachment", () => {
      it.todo("should require message_id");
      it.todo("should require attachment_id");
      it.todo("should require output_path");
    });
  });

  describe("execute", () => {
    describe("list_messages", () => {
      it.todo("should call Gmail API messages.list correctly");
      it.todo("should return list of message summaries");
      it.todo("should handle errors");
    });
    describe("get_message", () => {
      it.todo("should call Gmail API messages.get correctly");
      it.todo("should return full message with body and attachments");
      it.todo("should handle errors");
    });
    describe("send_message", () => {
      it.todo("should call Gmail API messages.send correctly");
      it.todo("should return id and thread_id");
      it.todo("should handle errors");
    });
    describe("reply_message", () => {
      it.todo("should call Gmail API with In-Reply-To header");
      it.todo("should return id and thread_id");
      it.todo("should handle errors");
    });
    describe("forward_message", () => {
      it.todo("should call Gmail API correctly");
      it.todo("should return id and thread_id");
      it.todo("should handle errors");
    });
    describe("search", () => {
      it.todo("should call Gmail API with search query");
      it.todo("should return list of matching messages");
      it.todo("should handle errors");
    });
    describe("create_draft", () => {
      it.todo("should call Gmail API drafts.create correctly");
      it.todo("should return draft_id and message_id");
      it.todo("should handle errors");
    });
    describe("send_draft", () => {
      it.todo("should call Gmail API drafts.send correctly");
      it.todo("should return id and thread_id");
      it.todo("should handle errors");
    });
    describe("list_labels", () => {
      it.todo("should call Gmail API labels.list correctly");
      it.todo("should return list of labels");
      it.todo("should handle errors");
    });
    describe("create_label", () => {
      it.todo("should call Gmail API labels.create correctly");
      it.todo("should return id and name");
      it.todo("should handle errors");
    });
    describe("add_label", () => {
      it.todo("should call Gmail API messages.modify correctly");
      it.todo("should return id and label_ids");
      it.todo("should handle errors");
    });
    describe("get_thread", () => {
      it.todo("should call Gmail API threads.get correctly");
      it.todo("should return thread with messages");
      it.todo("should handle errors");
    });
    describe("download_attachment", () => {
      it.todo("should call Gmail API attachments.get correctly");
      it.todo("should write file to output_path");
      it.todo("should return file_path, size, mime_type");
      it.todo("should handle errors");
    });
  });
});
