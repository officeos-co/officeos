import { describe, it } from "bun:test";

describe("labels", () => {
  describe("list_labels", () => {
    it.todo("should GET /labels and return all labels");
    it.todo("should map messageListVisibility and labelListVisibility");
  });

  describe("get_label", () => {
    it.todo("should return messages_total, messages_unread, threads_total, threads_unread");
  });

  describe("create_label", () => {
    it.todo("should POST /labels with name");
    it.todo("should return id and name");
  });

  describe("add_label", () => {
    it.todo("should POST to /messages/{id}/modify with addLabelIds");
    it.todo("should return updated label_ids");
  });

  describe("remove_label", () => {
    it.todo("should POST to /messages/{id}/modify with removeLabelIds");
  });
});
