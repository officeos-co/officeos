import { describe, it } from "bun:test";

describe("messages", () => {
  describe("list_messages", () => {
    it.todo("should call /messages with labelIds and maxResults");
    it.todo("should fetch metadata for each message individually");
    it.todo("should extract From, To, Subject, Date headers");
  });

  describe("get_message", () => {
    it.todo("should call /messages/{id}?format=full");
    it.todo("should extract body plain and html parts");
    it.todo("should list attachments with filename, mime_type, size");
  });

  describe("send_message", () => {
    it.todo("should build RFC 2822 message with to, subject, body");
    it.todo("should base64url-encode the raw message");
    it.todo("should POST to /messages/send");
  });

  describe("reply_message", () => {
    it.todo("should fetch original message for In-Reply-To and References headers");
    it.todo("should prepend Re: to subject if not already present");
    it.todo("should include threadId in the send payload");
  });

  describe("trash_message", () => {
    it.todo("should POST to /messages/{id}/trash");
    it.todo("should return id and label_ids");
  });

  describe("search", () => {
    it.todo("should pass q param to /messages list endpoint");
    it.todo("should return snippet, from, subject, date");
  });
});
