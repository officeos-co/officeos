import { describe, it } from "bun:test";

// Detailed tests live in __tests__/boards.test.ts and __tests__/cards.test.ts

describe("trello", () => {
  describe("actions", () => {
    // Boards
    it.todo("should expose list_boards action");
    it.todo("should expose get_board action");
    it.todo("should expose create_board action");
    it.todo("should expose update_board action");
    it.todo("should expose list_board_labels action");
    it.todo("should expose create_label action");
    it.todo("should expose list_board_members action");
    // Lists
    it.todo("should expose list_lists action");
    it.todo("should expose create_list action");
    it.todo("should expose update_list action");
    // Cards
    it.todo("should expose list_cards action");
    it.todo("should expose get_card action");
    it.todo("should expose create_card action");
    it.todo("should expose update_card action");
    it.todo("should expose delete_card action");
    it.todo("should expose move_card action");
    // Comments
    it.todo("should expose list_card_comments action");
    it.todo("should expose add_card_comment action");
    it.todo("should expose delete_card_comment action");
    // Checklists
    it.todo("should expose list_card_checklists action");
    it.todo("should expose create_checklist action");
    it.todo("should expose add_checklist_item action");
  });

  describe("credentials", () => {
    it.todo("should require api_key credential");
    it.todo("should require token credential");
    it.todo("should append api_key and token as query params to every request");
    it.todo("should never send credentials in headers");
  });
});
