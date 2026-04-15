import { describe, it } from "bun:test";

describe("workers", () => {
  describe("list_workers", () => {
    it.todo("should resolve account ID via /accounts endpoint");
    it.todo("should call /accounts/:id/workers/scripts");
  });

  describe("get_worker", () => {
    it.todo("should fetch script as text");
    it.todo("should fetch settings for bindings metadata");
  });

  describe("deploy_worker", () => {
    it.todo("should PUT script with application/javascript content type");
    it.todo("should return script size in bytes");
  });

  describe("delete_worker", () => {
    it.todo("should DELETE /accounts/:id/workers/scripts/:name");
  });

  describe("tail_worker", () => {
    it.todo("should POST to /accounts/:id/workers/scripts/:name/tails");
    it.todo("should include filter when status is provided");
  });
});
