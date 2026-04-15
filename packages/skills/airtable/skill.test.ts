import { describe, it } from "bun:test";

describe("airtable skill", () => {
  describe("list_bases", () => {
    it.todo("should GET /meta/bases");
    it.todo("should return array of {id, name, permission_level}");
    it.todo("should throw on 401 for invalid API key");
  });

  describe("get_schema", () => {
    it.todo("should GET /meta/bases/:base_id/tables");
    it.todo("should return id, name, and tables array");
  });

  describe("list_tables", () => {
    it.todo("should GET /meta/bases/:base_id/tables");
    it.todo("should return the tables array from the response");
  });

  describe("create_table", () => {
    it.todo("should POST /meta/bases/:base_id/tables with name and fields");
    it.todo("should accept optional description");
    it.todo("should return table object with id");
  });

  describe("update_table", () => {
    it.todo("should PATCH /meta/bases/:base_id/tables/:table_id");
    it.todo("should accept partial updates (name only, description only)");
  });

  describe("create_field", () => {
    it.todo("should POST /meta/bases/:base_id/tables/:table_id/fields");
    it.todo("should accept all supported field types");
    it.todo("should accept optional type-specific options");
    it.todo("should return field object with id and type");
  });

  describe("update_field", () => {
    it.todo("should PATCH /meta/bases/:base_id/tables/:table_id/fields/:field_id");
    it.todo("should accept partial updates");
  });

  describe("list_records", () => {
    it.todo("should GET /:base_id/:table_id");
    it.todo("should pass filterByFormula query param when filter provided");
    it.todo("should pass maxRecords query param");
    it.todo("should pass sort params as sort[0][field] and sort[0][direction]");
    it.todo("should pass fields[] array params when fields provided");
    it.todo("should pass view param when specified");
    it.todo("should pass offset for pagination");
    it.todo("should return records array and optional offset");
  });

  describe("get_record", () => {
    it.todo("should GET /:base_id/:table_id/:record_id");
    it.todo("should return {id, fields, created_time}");
  });

  describe("create_record", () => {
    it.todo("should POST /:base_id/:table_id with {fields}");
    it.todo("should return created record with id");
  });

  describe("update_record", () => {
    it.todo("should PATCH /:base_id/:table_id/:record_id when merge=true");
    it.todo("should PUT /:base_id/:table_id/:record_id when merge=false");
    it.todo("should default merge to true");
  });

  describe("delete_record", () => {
    it.todo("should DELETE /:base_id/:table_id/:record_id");
    it.todo("should return {id, deleted: true}");
  });

  describe("batch_create", () => {
    it.todo("should split records into chunks of 10");
    it.todo("should POST each chunk to /:base_id/:table_id with {records: [{fields}]}");
    it.todo("should concatenate results from all batches");
    it.todo("should handle single-record input");
  });

  describe("batch_update", () => {
    it.todo("should split records into chunks of 10");
    it.todo("should PATCH or PUT based on merge flag");
    it.todo("should concatenate results from all batches");
  });

  describe("batch_delete", () => {
    it.todo("should split record_ids into chunks of 10");
    it.todo("should DELETE with records[] query params per chunk");
    it.todo("should return array of {id, deleted} for all records");
  });
});
