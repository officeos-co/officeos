import { describe, it } from "bun:test";

describe("drafts", () => {
  describe("list_drafts", () => {
    it.todo("should call /drafts with maxResults");
    it.todo("should fetch full draft for each listed draft");
    it.todo("should return draft_id, message_id, snippet, subject");
  });

  describe("create_draft", () => {
    it.todo("should POST to /drafts with encoded message");
    it.todo("should return draft_id and message_id");
  });

  describe("update_draft", () => {
    it.todo("should fetch existing draft to get current field values");
    it.todo("should PUT updated message to /drafts/{id}");
    it.todo("should preserve existing fields when optional params are omitted");
  });

  describe("send_draft", () => {
    it.todo("should POST to /drafts/send with draft id");
    it.todo("should return id, thread_id, label_ids");
  });

  describe("delete_draft", () => {
    it.todo("should DELETE /drafts/{id}");
    it.todo("should return { status: deleted }");
  });
});
