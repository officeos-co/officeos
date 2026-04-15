import { describe, it, expect } from "bun:test";

describe("products", () => {
  describe("list_products", () => {
    it.todo("should call /v1/products with limit param");
    it.todo("should filter by active status when provided");
  });

  describe("get_product", () => {
    it.todo("should call /v1/products/:id");
    it.todo("should return updated timestamp");
  });

  describe("create_product", () => {
    it.todo("should POST to /v1/products with name");
    it.todo("should flatten images into bracket notation");
  });

  describe("update_product", () => {
    it.todo("should POST to /v1/products/:id with update fields");
  });

  describe("archive_product", () => {
    it.todo("should POST active=false to /v1/products/:id");
    it.todo("should return active as false");
  });

  describe("list_prices", () => {
    it.todo("should call /v1/prices with limit param");
    it.todo("should filter by product and active status");
  });

  describe("get_price", () => {
    it.todo("should call /v1/prices/:id");
    it.todo("should return billing_scheme");
  });

  describe("create_price", () => {
    it.todo("should POST to /v1/prices with product, unit_amount, currency");
    it.todo("should add recurring bracket params when interval provided");
  });
});
