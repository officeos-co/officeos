import { describe, it, expect } from "bun:test";

describe("bulk", () => {
  describe("bulk_insert", () => {
    it.todo("should create a job with operation=insert");
    it.todo("should PUT records to /jobs/ingest/:id/batches");
    it.todo("should PATCH job state to UploadComplete");
    it.todo("should return job_id and state from closed job");
  });

  describe("bulk_update", () => {
    it.todo("should create a job with operation=update");
    it.todo("should PUT records including Id field");
    it.todo("should return job_id and state");
  });

  describe("bulk_delete", () => {
    it.todo("should create a job with operation=delete");
    it.todo("should map record_ids to [{Id: ...}] objects");
    it.todo("should return job_id and state");
  });
});
