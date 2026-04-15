import { describe, it } from "bun:test";

describe("dashboards", () => {
  describe("list_dashboards", () => {
    it.todo("should use entitySearch with type = DASHBOARD filter");
    it.todo("should include name LIKE filter when name provided");
    it.todo("should return guid, name, permalink, nextCursor");
  });

  describe("get_dashboard", () => {
    it.todo("should query DashboardEntity fragment");
    it.todo("should return pages with widgets");
  });

  describe("create_dashboard", () => {
    it.todo("should POST dashboardCreate mutation");
    it.todo("should default permissions to PRIVATE");
    it.todo("should return guid and permalink");
  });
});

describe("synthetics", () => {
  describe("list_synthetic_monitors", () => {
    it.todo("should use entitySearch with type = MONITOR filter");
    it.todo("should return monitorType, period, uri");
  });

  describe("get_synthetic_monitor", () => {
    it.todo("should query SyntheticMonitorEntity fragment");
    it.todo("should return tags array");
  });
});

describe("deployments", () => {
  describe("record_deployment", () => {
    it.todo("should POST changeTrackingCreateDeployment mutation");
    it.todo("should include optional changelog and commit fields");
    it.todo("should return deploymentId, version, timestamp");
  });

  describe("list_deployments", () => {
    it.todo("should query ApmApplicationEntity deployments field");
    it.todo("should respect limit param");
    it.todo("should return version, user, timestamp per entry");
  });
});
