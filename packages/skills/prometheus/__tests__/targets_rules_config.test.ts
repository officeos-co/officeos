import { describe, it } from "bun:test";

describe("targets", () => {
  describe("targets", () => {
    it.todo("should call /api/v1/targets");
    it.todo("should pass state param when not 'any'");
    it.todo("should return activeTargets and droppedTargets");
  });

  describe("rules", () => {
    it.todo("should call /api/v1/rules");
    it.todo("should pass type param when provided");
    it.todo("should return groups with nested rules");
  });

  describe("alerts", () => {
    it.todo("should call /api/v1/alerts");
    it.todo("should return alerts array with labels, state, activeAt");
  });
});

describe("config_metadata", () => {
  describe("config", () => {
    it.todo("should call /api/v1/status/config");
    it.todo("should return yaml string");
  });

  describe("flags", () => {
    it.todo("should call /api/v1/status/flags");
    it.todo("should return key-value flag map");
  });

  describe("metadata", () => {
    it.todo("should call /api/v1/metadata");
    it.todo("should pass metric filter param");
    it.todo("should return map of metric name to metadata array");
  });

  describe("tsdb_stats", () => {
    it.todo("should call /api/v1/status/tsdb");
    it.todo("should return headStats with numSeries and chunkCount");
  });
});
