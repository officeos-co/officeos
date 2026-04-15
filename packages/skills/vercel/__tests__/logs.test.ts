import { describe, it } from "bun:test";

describe("logs", () => {
  describe("get_deployment_logs", () => {
    it.todo("should call /v2/deployments/:id/events");
    it.todo("should map event payload text");
  });

  describe("get_build_logs", () => {
    it.todo("should call /v13/deployments/:id");
    it.todo("should extract logs from builds[].output[]");
  });
});
