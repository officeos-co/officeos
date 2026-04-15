import { describe, it, expect } from "bun:test";

describe("payments", () => {
  describe("list_payment_intents", () => {
    it.todo("should call /v1/payment_intents with limit param");
    it.todo("should filter by customer when provided");
    it.todo("should map response to payment intent array");
  });

  describe("get_payment_intent", () => {
    it.todo("should call /v1/payment_intents/:id");
    it.todo("should return full intent with charges array");
  });

  describe("create_payment_intent", () => {
    it.todo("should POST to /v1/payment_intents with amount and currency");
    it.todo("should flatten payment_method_types into bracket notation");
    it.todo("should return client_secret");
  });

  describe("confirm_payment", () => {
    it.todo("should POST to /v1/payment_intents/:id/confirm");
    it.todo("should include payment_method in body if provided");
  });

  describe("cancel_payment", () => {
    it.todo("should POST to /v1/payment_intents/:id/cancel");
    it.todo("should include cancellation_reason if provided");
  });

  describe("list_charges", () => {
    it.todo("should call /v1/charges with limit param");
    it.todo("should filter by customer ID");
  });

  describe("get_charge", () => {
    it.todo("should call /v1/charges/:id");
    it.todo("should return refunds array from nested data");
  });

  describe("create_charge", () => {
    it.todo("should POST to /v1/charges with amount and currency");
    it.todo("should require customer or source");
  });
});
