import { describe, it, expect } from "bun:test";

describe("invoices", () => {
  describe("list_invoices", () => {
    it.todo("should call /v1/invoices with limit param");
    it.todo("should filter by customer and status");
  });

  describe("get_invoice", () => {
    it.todo("should call /v1/invoices/:id");
    it.todo("should return line items from nested data");
  });

  describe("create_invoice", () => {
    it.todo("should POST to /v1/invoices with customer and collection_method");
    it.todo("should include due_date as string when provided");
  });

  describe("send_invoice", () => {
    it.todo("should POST to /v1/invoices/:id/send");
    it.todo("should return hosted_invoice_url");
  });

  describe("void_invoice", () => {
    it.todo("should POST to /v1/invoices/:id/void");
    it.todo("should return void status");
  });

  describe("finalize_invoice", () => {
    it.todo("should POST to /v1/invoices/:id/finalize");
    it.todo("should return invoice_pdf URL");
  });
});
