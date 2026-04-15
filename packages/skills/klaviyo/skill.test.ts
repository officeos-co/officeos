import { describe, it } from "bun:test";

describe("klaviyo skill", () => {
  describe("list_profiles", () => {
    it.todo("should call GET /profiles with page[size] and page[cursor]");
    it.todo("should return profiles array and next_cursor");
    it.todo("should extract cursor from links.next URL");
  });

  describe("get_profile", () => {
    it.todo("should call GET /profiles/:id");
    it.todo("should map attributes correctly");
  });

  describe("create_profile", () => {
    it.todo("should POST to /profiles with JSONAPI data structure");
    it.todo("should include custom properties when provided");
    it.todo("should return mapped profile");
  });

  describe("update_profile", () => {
    it.todo("should PATCH /profiles/:id");
    it.todo("should only send provided attributes");
  });

  describe("search_profiles", () => {
    it.todo("should call GET /profiles with equals(email,...) filter");
    it.todo("should return array of matching profiles");
  });

  describe("list_lists", () => {
    it.todo("should call GET /lists");
    it.todo("should return lists and next_cursor");
  });

  describe("get_list", () => {
    it.todo("should call GET /lists/:id");
  });

  describe("create_list", () => {
    it.todo("should POST to /lists with name in attributes");
  });

  describe("add_profiles_to_list", () => {
    it.todo("should POST to /lists/:id/relationships/profiles");
    it.todo("should convert emails to relationship objects");
    it.todo("should return count of subscribed emails");
  });

  describe("list_segments", () => {
    it.todo("should call GET /segments");
  });

  describe("get_segment", () => {
    it.todo("should call GET /segments/:id");
  });

  describe("list_campaigns", () => {
    it.todo("should filter by channel using equals() filter");
    it.todo("should default to email channel");
  });

  describe("get_campaign", () => {
    it.todo("should call GET /campaigns/:id");
    it.todo("should return send_time as nullable");
  });

  describe("create_campaign", () => {
    it.todo("should POST campaign with list audience relationships");
    it.todo("should include template_id when provided");
  });

  describe("list_flows", () => {
    it.todo("should call GET /flows");
  });

  describe("get_flow", () => {
    it.todo("should call GET /flows/:id");
  });

  describe("list_templates", () => {
    it.todo("should call GET /templates");
  });

  describe("get_template", () => {
    it.todo("should call GET /templates/:id");
    it.todo("should return html and text content");
  });

  describe("track_event", () => {
    it.todo("should POST to /events with metric and profile data");
    it.todo("should default time to now when not provided");
    it.todo("should include value when provided");
  });

  describe("list_events", () => {
    it.todo("should call GET /events with sort param");
    it.todo("should return metric_id and profile_id from relationships");
  });

  describe("list_metrics", () => {
    it.todo("should call GET /metrics");
    it.todo("should include integration name");
  });

  describe("auth", () => {
    it.todo("should set Klaviyo-API-Key header on every request");
    it.todo("should include revision header 2024-10-15");
  });
});
