import { describe, it } from "bun:test";

describe("events", () => {
  describe("list_events", () => {
    it.todo("should call /api/projects/{id}/events/ with limit param");
    it.todo("should pass event filter as query param");
    it.todo("should pass distinct_id, after, before when provided");
    it.todo("should map results array to event objects");
  });

  describe("get_event", () => {
    it.todo("should call /api/projects/{id}/events/{event_id}/");
    it.todo("should return elements field from response");
  });
});

describe("persons", () => {
  describe("list_persons", () => {
    it.todo("should call /api/projects/{id}/persons/ with limit");
    it.todo("should pass search param");
    it.todo("should return distinct_ids array per person");
  });

  describe("get_person", () => {
    it.todo("should call /api/projects/{id}/persons/{person_id}/");
  });

  describe("delete_person", () => {
    it.todo("should DELETE /api/projects/{id}/persons/{person_id}/");
    it.todo("should pass delete_events body when true");
    it.todo("should return empty object on 204");
  });
});
