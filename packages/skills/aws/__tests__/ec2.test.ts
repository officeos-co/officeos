import { describe, it } from "bun:test";

describe("ec2", () => {
  describe("list_instances", () => {
    it.todo("should call DescribeInstances with state filter");
    it.todo("should apply extra filters from JSON param");
    it.todo("should return mapped instance array");
    it.todo("should parse tags from item blocks");
  });

  describe("get_instance", () => {
    it.todo("should call DescribeInstances with InstanceId");
    it.todo("should return all instance fields");
  });

  describe("start_instance", () => {
    it.todo("should call StartInstances");
    it.todo("should return previous and current state");
  });

  describe("stop_instance", () => {
    it.todo("should call StopInstances");
    it.todo("should return previous and current state");
  });

  describe("terminate_instance", () => {
    it.todo("should call TerminateInstances");
  });

  describe("create_instance", () => {
    it.todo("should call RunInstances with required params");
    it.todo("should apply security groups, tags, user_data");
    it.todo("should return launched instance info");
  });
});
