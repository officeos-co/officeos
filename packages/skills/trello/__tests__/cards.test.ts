import { describe, it } from "bun:test";

describe("trello/cards", () => {
  describe("list_lists", () => {
    it.todo("should require board_id");
    it.todo("should GET /boards/{id}/lists with filter param");
    it.todo("should default filter to open");
    it.todo("should return array of id, name, closed, pos");
  });

  describe("create_list", () => {
    it.todo("should require board_id and name");
    it.todo("should POST to /lists with idBoard in body");
    it.todo("should return id, name, pos");
  });

  describe("update_list", () => {
    it.todo("should require list_id");
    it.todo("should PUT to /lists/{id}");
    it.todo("should support closed for archiving");
    it.todo("should return id, name");
  });

  describe("list_cards", () => {
    it.todo("should require either list_id or board_id");
    it.todo("should throw when neither is provided");
    it.todo("should GET /lists/{id}/cards when list_id provided");
    it.todo("should GET /boards/{id}/cards when board_id provided");
    it.todo("should return array of id, name, desc, url, due, closed, id_list, pos");
  });

  describe("get_card", () => {
    it.todo("should require card_id");
    it.todo("should GET /cards/{id}");
    it.todo("should return full card object");
  });

  describe("create_card", () => {
    it.todo("should require list_id and name");
    it.todo("should POST to /cards");
    it.todo("should pass idList in body");
    it.todo("should join id_members and id_labels as comma-separated strings");
    it.todo("should default pos to bottom");
    it.todo("should return id, name, url, id_list");
  });

  describe("update_card", () => {
    it.todo("should require card_id");
    it.todo("should PUT to /cards/{id}");
    it.todo("should map id_list to idList");
    it.todo("should map due_complete to dueComplete");
    it.todo("should return id, name, id_list");
  });

  describe("delete_card", () => {
    it.todo("should require card_id");
    it.todo("should DELETE /cards/{id}");
    it.todo("should return success: true");
  });

  describe("move_card", () => {
    it.todo("should require card_id and list_id");
    it.todo("should PUT to /cards/{id} with idList");
    it.todo("should default pos to bottom");
    it.todo("should return id, name, id_list");
  });

  describe("list_card_comments", () => {
    it.todo("should require card_id");
    it.todo("should GET /cards/{id}/actions with filter=commentCard");
    it.todo("should return array of id, text, date, member_creator");
    it.todo("should extract text from data.text");
  });

  describe("add_card_comment", () => {
    it.todo("should require card_id and text");
    it.todo("should POST to /cards/{id}/actions/comments");
    it.todo("should return id, text, date");
  });

  describe("delete_card_comment", () => {
    it.todo("should require card_id and comment_id");
    it.todo("should DELETE /cards/{card_id}/actions/{comment_id}/comments");
    it.todo("should return success: true");
  });

  describe("list_card_checklists", () => {
    it.todo("should require card_id");
    it.todo("should GET /cards/{id}/checklists");
    it.todo("should return array of id, name, check_items");
    it.todo("should map checkItems to check_items with id, name, state");
  });

  describe("create_checklist", () => {
    it.todo("should require card_id and name");
    it.todo("should POST to /checklists with idCard in body");
    it.todo("should return id, name");
  });

  describe("add_checklist_item", () => {
    it.todo("should require checklist_id and name");
    it.todo("should POST to /checklists/{id}/checkItems");
    it.todo("should accept optional checked boolean");
    it.todo("should return id, name, state");
  });
});
