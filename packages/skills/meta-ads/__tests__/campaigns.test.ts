import { describe, it } from "bun:test";

describe("campaigns", () => {
  describe("list_campaigns", () => {
    it.todo("should call /{account_id}/campaigns with fields and limit");
    it.todo("should pass effective_status filter when status provided");
    it.todo("should pass time_range filter when date_range provided");
    it.todo("should fallback to effective_status for status field");
  });

  describe("get_campaign", () => {
    it.todo("should fetch single campaign by ID with all detail fields");
  });

  describe("create_campaign", () => {
    it.todo("should POST to /{account_id}/campaigns");
    it.todo("should include daily_budget only when provided");
    it.todo("should default status to PAUSED");
  });

  describe("update_campaign", () => {
    it.todo("should POST partial fields to /{campaign_id}");
  });

  describe("pause_campaign", () => {
    it.todo("should POST status=PAUSED to /{campaign_id}");
  });

  describe("delete_campaign", () => {
    it.todo("should DELETE /{campaign_id} and return success");
  });
});
