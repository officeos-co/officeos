import { describe, it } from "bun:test";

describe("applications", () => {
  describe("list_applications", () => {
    it.todo("should call /v2/applications.json");
    it.todo("should pass filter[name] param when filter_name provided");
    it.todo("should respect limit param by slicing results");
    it.todo("should return health_status and application_summary");
  });

  describe("get_application", () => {
    it.todo("should call /v2/applications/{id}.json");
    it.todo("should return application with settings and links");
  });
});
