import { describe, it, expect } from "bun:test";

describe("subscriptions", () => {
  describe("list_subscriptions", () => {
    it.todo("should call /v1/subscriptions with limit param");
    it.todo("should filter by customer and status");
    it.todo("should return mapped subscription array");
  });

  describe("get_subscription", () => {
    it.todo("should call /v1/subscriptions/:id");
    it.todo("should return full subscription with trial dates");
  });

  describe("create_subscription", () => {
    it.todo("should POST to /v1/subscriptions with customer and price_ids");
    it.todo("should set items using bracket notation for multiple prices");
    it.todo("should include trial_period_days when provided");
  });

  describe("update_subscription", () => {
    it.todo("should POST to /v1/subscriptions/:id with updated fields");
    it.todo("should apply proration_behavior when provided");
  });

  describe("cancel_subscription", () => {
    it.todo("should set cancel_at_period_end=true when at_period_end is true");
    it.todo("should DELETE subscription for immediate cancellation");
  });

  describe("pause_subscription", () => {
    it.todo("should POST with pause_collection[behavior]");
  });
});
