import { describe, it, expect } from "bun:test";

describe("engagement", () => {
  describe("create_note", () => {
    it.todo("should POST to /crm/v3/objects/notes with hs_note_body");
    it.todo("should build associations array for contact_ids with typeId 202");
    it.todo("should build associations array for company_ids with typeId 190");
    it.todo("should build associations array for deal_ids with typeId 214");
    it.todo("should send empty associations array when none provided");
  });

  describe("create_task", () => {
    it.todo("should POST to /crm/v3/objects/tasks with hs_task_subject");
    it.todo("should include priority and status props when provided");
    it.todo("should build associations for contacts/companies/deals");
  });

  describe("create_email_activity", () => {
    it.todo("should POST to /crm/v3/objects/emails with email props");
    it.todo("should set hs_email_direction to EMAIL");
  });

  describe("list_engagements", () => {
    it.todo("should fetch notes, tasks, and emails for the object");
    it.todo("should gracefully skip failed individual engagement fetches");
    it.todo("should return at most limit results");
  });
});
