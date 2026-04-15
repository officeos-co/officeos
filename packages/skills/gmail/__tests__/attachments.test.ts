import { describe, it } from "bun:test";

describe("attachments", () => {
  describe("get_attachment", () => {
    it.todo("should call /messages/{id}/attachments/{attachmentId}");
    it.todo("should return attachment_id, size, base64 data");
  });

  describe("download_attachment", () => {
    it.todo("should fetch message to get attachment metadata");
    it.todo("should decode base64url data to buffer");
    it.todo("should write buffer to output_path");
    it.todo("should return file_path, size, mime_type");
  });
});
