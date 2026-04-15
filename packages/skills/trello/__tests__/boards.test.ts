import { describe, it } from "bun:test";

describe("trello/boards", () => {
  describe("list_boards", () => {
    it.todo("should GET /members/me/boards with api_key and token as query params");
    it.todo("should accept optional filter defaulting to open");
    it.todo("should return array of id, name, desc, url, closed");
    it.todo("should throw on non-200 response");
  });

  describe("get_board", () => {
    it.todo("should require board_id");
    it.todo("should GET /boards/{id}");
    it.todo("should return id, name, desc, url, closed");
  });

  describe("create_board", () => {
    it.todo("should require name");
    it.todo("should POST to /boards");
    it.todo("should include defaultLists in body");
    it.todo("should include idOrganization when provided");
    it.todo("should return id, name, url");
  });

  describe("update_board", () => {
    it.todo("should require board_id");
    it.todo("should PUT to /boards/{id}");
    it.todo("should only include provided fields");
    it.todo("should return id, name");
  });

  describe("list_board_labels", () => {
    it.todo("should require board_id");
    it.todo("should GET /boards/{id}/labels");
    it.todo("should return array of id, name, color");
  });

  describe("create_label", () => {
    it.todo("should require board_id, name, color");
    it.todo("should POST to /boards/{id}/labels");
    it.todo("should return id, name, color");
  });

  describe("list_board_members", () => {
    it.todo("should require board_id");
    it.todo("should GET /boards/{id}/members");
    it.todo("should return array of id, username, full_name");
  });
});
