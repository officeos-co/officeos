import { describe, it } from "bun:test";

describe("events", () => {
  describe("list_events", () => {
    it.todo("should call /projects/{org}/{project}/events/ with limit");
    it.todo("should return event_id, title, level, platform, timestamp, tags");
  });

  describe("get_event", () => {
    it.todo("should call /projects/{org}/{project}/events/{id}/");
    it.todo("should extract exception from entries where type=exception");
    it.todo("should extract breadcrumbs from entries where type=breadcrumbs");
    it.todo("should extract request from entries where type=request");
  });

  describe("get_latest_event", () => {
    it.todo("should call /issues/{id}/events/latest/");
    it.todo("should return same shape as get_event");
  });
});
