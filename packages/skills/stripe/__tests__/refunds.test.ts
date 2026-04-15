import { describe, it, expect } from "bun:test";

describe("refunds", () => {
  describe("list_refunds", () => {
    it.todo("should call /v1/refunds with limit param");
    it.todo("should filter by charge and payment_intent");
    it.todo("should return mapped refund array with nullable fields");
  });

  describe("create_refund", () => {
    it.todo("should POST to /v1/refunds with charge or payment_intent");
    it.todo("should include partial amount when provided");
    it.todo("should include reason when provided");
    it.todo("should flatten metadata into bracket notation");
    it.todo("should throw when neither charge nor payment_intent provided");
  });
});
