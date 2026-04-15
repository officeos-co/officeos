import { describe, it, expect } from "bun:test";

describe("pipelines", () => {
  describe("list_pipelines", () => {
    it.todo("should call /crm/v3/pipelines/deals by default");
    it.todo("should call /crm/v3/pipelines/tickets for tickets object type");
    it.todo("should return mapped pipeline array with stages");
  });

  describe("get_pipeline", () => {
    it.todo("should call /crm/v3/pipelines/:type/:id");
    it.todo("should return stages with id, label, displayOrder");
  });

  describe("list_pipeline_stages", () => {
    it.todo("should call /crm/v3/pipelines/:type/:id/stages");
    it.todo("should return stage metadata as null when absent");
  });
});
