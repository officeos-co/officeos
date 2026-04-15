import { describe, it } from "bun:test";

describe("ads", () => {
  describe("list_ads", () => {
    it.todo("should list from /{ad_set_id}/ads when ad_set_id provided");
    it.todo("should list from /{account_id}/ads otherwise");
    it.todo("should extract creative.id from nested creative field");
  });

  describe("get_ad", () => {
    it.todo("should fetch ad with all detail fields including preview link");
  });

  describe("create_ad", () => {
    it.todo("should derive account_id from ad set before posting");
    it.todo("should POST to /act_{account_id}/ads with creative_id wrapped in object");
  });

  describe("update_ad", () => {
    it.todo("should POST only provided fields to /{ad_id}");
  });

  describe("pause_ad", () => {
    it.todo("should POST status=PAUSED to /{ad_id}");
  });
});
