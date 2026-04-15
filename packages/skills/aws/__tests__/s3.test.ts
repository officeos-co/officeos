import { describe, it } from "bun:test";

describe("s3", () => {
  describe("list_buckets", () => {
    it.todo("should call GET / on s3 endpoint");
    it.todo("should return bucket name and creation_date");
  });

  describe("list_objects", () => {
    it.todo("should pass list-type=2 and max-keys");
    it.todo("should filter by prefix when provided");
    it.todo("should return key, size, last_modified, etag");
  });

  describe("get_object", () => {
    it.todo("should call s3GetObject and return body");
    it.todo("should base64-encode binary content");
  });

  describe("put_object", () => {
    it.todo("should call PUT with content-type header");
  });

  describe("delete_object", () => {
    it.todo("should call DELETE on the object key");
    it.todo("should return deleted: true");
  });

  describe("create_bucket", () => {
    it.todo("should PUT / with location constraint for non-us-east-1");
    it.todo("should skip location constraint for us-east-1");
  });

  describe("delete_bucket", () => {
    it.todo("should call DELETE / on the bucket");
  });

  describe("presign_url", () => {
    it.todo("should return url with X-Amz-Signature");
    it.todo("should compute expires_at from expires_in");
  });
});
