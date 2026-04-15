import { describe, it } from "bun:test";

describe("rds", () => {
  describe("list_db_instances", () => {
    it.todo("should call DescribeDBInstances without filter by default");
    it.todo("should apply engine filter when provided");
    it.todo("should return endpoint as null when absent");
    it.todo("should parse multi_az as boolean");
  });

  describe("describe_db_instance", () => {
    it.todo("should call DescribeDBInstances with identifier");
    it.todo("should return security_groups array");
    it.todo("should return latest_restorable_time as null when absent");
  });

  describe("create_db_snapshot", () => {
    it.todo("should call CreateDBSnapshot with required params");
    it.todo("should attach tags when provided");
    it.todo("should return snapshot_id and status");
  });
});
