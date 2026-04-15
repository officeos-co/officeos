import { describe, it, expect } from "bun:test";

describe("balance", () => {
  describe("get_balance", () => {
    it.todo("should call /v1/balance");
    it.todo("should return available and pending arrays by currency");
    it.todo("should include connect_reserved as null when absent");
  });

  describe("list_balance_transactions", () => {
    it.todo("should call /v1/balance_transactions with limit param");
    it.todo("should filter by type when provided");
    it.todo("should apply created date filters using flattenParams");
    it.todo("should return fee, net, and available_on fields");
  });

  describe("list_payouts", () => {
    it.todo("should call /v1/payouts with limit param");
    it.todo("should filter by status when provided");
  });

  describe("get_payout", () => {
    it.todo("should call /v1/payouts/:id");
    it.todo("should return payout with arrival_date and method");
  });

  describe("create_payout", () => {
    it.todo("should POST to /v1/payouts with amount and currency");
    it.todo("should include method and description when provided");
  });
});
