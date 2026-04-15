import { describe, it } from "bun:test";

describe("series_labels", () => {
  describe("series", () => {
    it.todo("should call /api/v1/series with match[] array params");
    it.todo("should pass start and end when provided");
    it.todo("should return array of label set objects");
  });

  describe("labels", () => {
    it.todo("should call /api/v1/labels");
    it.todo("should pass match[] selectors when provided");
    it.todo("should return array of label name strings");
  });

  describe("label_values", () => {
    it.todo("should call /api/v1/label/{name}/values");
    it.todo("should URL-encode the label name");
    it.todo("should pass match[] and time window params");
  });
});
