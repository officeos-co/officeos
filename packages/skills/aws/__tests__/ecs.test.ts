import { describe, it } from "bun:test";

describe("ecs", () => {
  describe("list_clusters", () => {
    it.todo("should call ListClusters then DescribeClusters");
    it.todo("should return empty array when no clusters exist");
    it.todo("should return running_tasks_count and active_services_count");
  });

  describe("list_services", () => {
    it.todo("should call ListServices then DescribeServices");
    it.todo("should return empty array when no services");
  });

  describe("list_tasks", () => {
    it.todo("should filter by service when provided");
    it.todo("should default to RUNNING status");
  });

  describe("describe_service", () => {
    it.todo("should throw when service not found");
    it.todo("should return last 10 events");
    it.todo("should map deployments correctly");
  });

  describe("update_service", () => {
    it.todo("should set forceNewDeployment when force_deployment true");
    it.todo("should update desired_count when provided");
    it.todo("should update task_definition when provided");
  });
});
