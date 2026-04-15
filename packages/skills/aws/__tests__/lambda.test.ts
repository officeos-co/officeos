import { describe, it } from "bun:test";

describe("lambda", () => {
  describe("list_functions", () => {
    it.todo("should call GET /2015-03-31/functions with MaxItems");
    it.todo("should return mapped function array");
  });

  describe("get_function", () => {
    it.todo("should call GET /2015-03-31/functions/:name");
    it.todo("should return environment variables");
  });

  describe("invoke_function", () => {
    it.todo("should call lambdaInvoke with payload");
    it.todo("should return status_code and payload");
    it.todo("should capture x-amz-function-error header");
  });

  describe("create_function", () => {
    it.todo("should POST with ZipFile when provided");
    it.todo("should POST with S3Bucket/S3Key when provided");
    it.todo("should parse environment JSON");
  });

  describe("update_function_code", () => {
    it.todo("should PUT /code endpoint");
    it.todo("should return code_sha256");
  });

  describe("delete_function", () => {
    it.todo("should DELETE /functions/:name");
    it.todo("should return deleted: true");
  });

  describe("list_function_logs", () => {
    it.todo("should call FilterLogEvents with /aws/lambda/ prefix");
    it.todo("should apply start_time and end_time");
  });
});
