import { describe, it } from "bun:test";

describe("insights", () => {
  describe("get_insights", () => {
    it.todo("should call /{object_id}/insights with level and fields params");
    it.todo("should use default metric fields when none specified");
    it.todo("should pass time_range when date_range provided");
    it.todo("should pass breakdowns as comma-separated string");
    it.todo("should pass time_increment when provided");
  });

  describe("get_campaign_insights", () => {
    it.todo("should fetch insights for campaign and return first result");
    it.todo("should return all-null object when no data returned");
  });

  describe("get_ad_set_insights", () => {
    it.todo("should fetch insights for ad set");
  });

  describe("get_ad_insights", () => {
    it.todo("should fetch insights for single ad");
  });
});
