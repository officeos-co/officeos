import { describe, it, expect } from "bun:test";

describe("lists", () => {
  describe("list_contact_lists", () => {
    it.todo("should call /contacts/v1/lists with count and offset");
    it.todo("should map timestamps to strings");
  });

  describe("get_list", () => {
    it.todo("should call /contacts/v1/lists/:id");
    it.todo("should return filters as null when absent");
  });

  describe("create_list", () => {
    it.todo("should POST with dynamic=true for DYNAMIC type");
    it.todo("should POST with dynamic=false for STATIC type");
  });

  describe("add_to_list", () => {
    it.todo("should POST vids as numbers to /contacts/v1/lists/:id/add");
  });

  describe("remove_from_list", () => {
    it.todo("should POST vids as numbers to /contacts/v1/lists/:id/remove");
  });

  describe("list_marketing_emails", () => {
    it.todo("should call /marketing-emails/v1/emails with limit and offset");
  });

  describe("get_email_stats", () => {
    it.todo("should compute open_rate as opens/sent");
    it.todo("should return null rates when sent is 0");
  });
});
