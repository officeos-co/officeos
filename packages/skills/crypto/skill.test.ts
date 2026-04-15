import { describe, it } from "bun:test";

describe("crypto skill", () => {
  describe("list_coins", () => {
    it.todo("should call GET /coins/list");
    it.todo("should return array of {id, symbol, name}");
    it.todo("should pass include_platform param");
  });

  describe("get_coin", () => {
    it.todo("should call GET /coins/:id");
    it.todo("should return description from description.en");
    it.todo("should extract market data fields when market_data is true");
    it.todo("should return null for missing market data");
  });

  describe("price", () => {
    it.todo("should call GET /simple/price with comma-joined ids and vs_currencies");
    it.todo("should include include_market_cap when true");
    it.todo("should return record keyed by coin ID");
  });

  describe("market_chart", () => {
    it.todo("should call GET /coins/:id/market_chart");
    it.todo("should pass days as string");
    it.todo("should accept 'max' as days value");
    it.todo("should return prices, market_caps, total_volumes arrays");
  });

  describe("markets", () => {
    it.todo("should call GET /coins/markets with vs_currency");
    it.todo("should filter by ids when provided");
    it.todo("should filter by category when provided");
    it.todo("should return market_cap_rank, current_price, 24h change");
    it.todo("should pass sparkline: false");
  });

  describe("search", () => {
    it.todo("should call GET /search with query param");
    it.todo("should return coins, exchanges, categories");
    it.todo("should include market_cap_rank in coin results");
  });

  describe("trending", () => {
    it.todo("should call GET /search/trending");
    it.todo("should return coins with rank, id, symbol, price_btc");
  });

  describe("exchanges", () => {
    it.todo("should call GET /exchanges with per_page and page");
    it.todo("should return trust_score, trade_volume_24h_btc");
  });

  describe("exchange_rates", () => {
    it.todo("should call GET /exchange_rates");
    it.todo("should return rates record with name, unit, value, type");
  });

  describe("categories", () => {
    it.todo("should call GET /coins/categories with order param");
    it.todo("should return market_cap and market_cap_change_24h");
  });

  describe("global", () => {
    it.todo("should call GET /global");
    it.todo("should return active_cryptocurrencies and total_market_cap_usd");
    it.todo("should return market_cap_percentage dominance record");
  });

  describe("ohlc", () => {
    it.todo("should call GET /coins/:id/ohlc");
    it.todo("should accept only valid days values: 1, 7, 14, 30, 90, 180, 365");
    it.todo("should return array of [timestamp, open, high, low, close] tuples");
  });
});
