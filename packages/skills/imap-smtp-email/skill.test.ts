import { describe, it } from "bun:test";

describe("imap-smtp-email skill", () => {
  describe("list_folders", () => {
    it.todo("should GET /folders with auth headers");
    it.todo("should return array of {name, delimiter, flags, message_count, unseen_count}");
    it.todo("should throw on proxy auth failure");
  });

  describe("get_folder", () => {
    it.todo("should GET /folders/:folder (URL-encoded)");
    it.todo("should return uid_validity and uid_next");
    it.todo("should URL-encode folder name with slashes, e.g. Work/Projects");
  });

  describe("list_messages", () => {
    it.todo("should GET /folders/INBOX/messages with limit and offset");
    it.todo("should default folder to INBOX");
    it.todo("should pass unseen_only query param");
    it.todo("should return array of message headers");
  });

  describe("get_message", () => {
    it.todo("should GET /folders/:folder/messages/:uid");
    it.todo("should pass include_body and format query params");
    it.todo("should default format to text");
    it.todo("should return text_body, html_body, and attachments array");
  });

  describe("search", () => {
    it.todo("should POST /folders/:folder/search with query params");
    it.todo("should accept since and before date filters");
    it.todo("should accept unseen and flagged boolean filters");
    it.todo("should default limit to 50");
    it.todo("should return array of message headers");
  });

  describe("send", () => {
    it.todo("should POST /messages/send with to, subject, body");
    it.todo("should include cc, bcc, reply_to when provided");
    it.todo("should attach files via attachments array");
    it.todo("should return message_id, accepted, rejected, sent_at");
    it.todo("should throw when proxy SMTP auth fails");
  });

  describe("reply", () => {
    it.todo("should POST /messages/reply with folder, uid, and body");
    it.todo("should default reply_all to false");
    it.todo("should include all original recipients when reply_all is true");
    it.todo("should return message_id and accepted array");
  });

  describe("forward", () => {
    it.todo("should POST /messages/forward with folder, uid, and to");
    it.todo("should prepend note when provided");
    it.todo("should return message_id and accepted");
  });

  describe("move", () => {
    it.todo("should POST /folders/:folder/messages/:uid/move with destination");
    it.todo("should URL-encode folder name");
    it.todo("should return new_folder and success");
  });

  describe("copy", () => {
    it.todo("should POST /folders/:folder/messages/:uid/copy with destination");
    it.todo("should return destination and success");
  });

  describe("delete_message", () => {
    it.todo("should DELETE /folders/:folder/messages/:uid with expunge flag");
    it.todo("should default expunge to true");
    it.todo("should return expunged: true when expunge was requested");
    it.todo("should return expunged: false when expunge was false");
  });

  describe("set_flags", () => {
    it.todo("should POST /folders/:folder/messages/:uid/flags with flags and action");
    it.todo("should accept add and remove actions");
    it.todo("should accept standard IMAP flags: \\Seen, \\Flagged, \\Answered, \\Draft");
    it.todo("should return updated flags array");
  });
});
