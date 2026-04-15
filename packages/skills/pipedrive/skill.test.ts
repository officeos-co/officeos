import { describe, it } from "bun:test";

describe("pipedrive skill", () => {
  describe("list_deals", () => {
    it.todo("should call GET /deals with status and pagination params");
    it.todo("should return mapped deal array");
    it.todo("should throw on non-success Pipedrive response");
  });

  describe("get_deal", () => {
    it.todo("should call GET /deals/:id");
    it.todo("should return full deal object");
    it.todo("should throw 404 error when deal not found");
  });

  describe("create_deal", () => {
    it.todo("should POST to /deals with title and optional fields");
    it.todo("should return created deal object");
  });

  describe("update_deal", () => {
    it.todo("should PUT to /deals/:id with partial fields");
    it.todo("should return updated deal object");
  });

  describe("delete_deal", () => {
    it.todo("should DELETE /deals/:id");
    it.todo("should return success confirmation");
  });

  describe("search_deals", () => {
    it.todo("should call GET /deals/search with term");
    it.todo("should respect exact_match flag");
    it.todo("should return scored results array");
  });

  describe("list_persons", () => {
    it.todo("should call GET /persons with pagination");
    it.todo("should map email and phone arrays correctly");
  });

  describe("get_person", () => {
    it.todo("should call GET /persons/:id");
  });

  describe("create_person", () => {
    it.todo("should wrap email string in Pipedrive email array format");
    it.todo("should wrap phone string in Pipedrive phone array format");
  });

  describe("update_person", () => {
    it.todo("should PUT to /persons/:id");
  });

  describe("list_organizations", () => {
    it.todo("should call GET /organizations with pagination");
  });

  describe("get_organization", () => {
    it.todo("should call GET /organizations/:id");
  });

  describe("create_organization", () => {
    it.todo("should POST to /organizations");
  });

  describe("list_activities", () => {
    it.todo("should call GET /activities with pagination");
    it.todo("should convert boolean done to numeric 0/1 for API");
    it.todo("should filter by type when provided");
  });

  describe("create_activity", () => {
    it.todo("should POST to /activities with all required fields");
  });

  describe("update_activity", () => {
    it.todo("should PUT to /activities/:id");
    it.todo("should be able to mark activity as done");
  });

  describe("list_pipelines", () => {
    it.todo("should call GET /pipelines");
    it.todo("should return array of pipelines with id, name, active");
  });

  describe("list_stages", () => {
    it.todo("should call GET /stages");
    it.todo("should filter by pipeline_id when provided");
  });

  describe("search", () => {
    it.todo("should call GET /itemSearch with term");
    it.todo("should filter by item_type when provided");
    it.todo("should return results with item_type and item fields");
    it.todo("should reject term shorter than 2 chars via Zod validation");
  });
});
