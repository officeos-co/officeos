import { describe, it } from "bun:test";

describe("containers", () => {
  describe("list_containers", () => {
    it.todo("should call /containers/json with all=false by default");
    it.todo("should parse filter expression as key=value into filters JSON param");
    it.todo("should strip leading slash from container names");
  });

  describe("run", () => {
    it.todo("should create container then POST to /start");
    it.todo("should build ExposedPorts and PortBindings from port mappings");
    it.todo("should split command string into Cmd array");
  });

  describe("stop / start / restart", () => {
    it.todo("should POST to /stop with timeout query param");
    it.todo("should POST to /start");
    it.todo("should POST to /restart with timeout query param");
  });

  describe("logs", () => {
    it.todo("should request stdout=true and stderr=true");
    it.todo("should pass tail and timestamps params");
    it.todo("should return log text as string");
  });

  describe("exec", () => {
    it.todo("should create exec instance then start it");
    it.todo("should inspect exec for exit code");
  });

  describe("stats", () => {
    it.todo("should compute cpu_percent from cpu_stats deltas");
    it.todo("should aggregate network rx/tx from all interfaces");
    it.todo("should aggregate block_read and block_write from blkio_stats");
  });
});
