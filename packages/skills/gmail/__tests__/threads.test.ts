import { describe, it } from "bun:test";

describe("threads", () => {
  describe("list_threads", () => {
    it.todo("should call /threads with maxResults");
    it.todo("should pass q param when query is provided");
    it.todo("should return thread_id, snippet, history_id");
  });

  describe("get_thread", () => {
    it.todo("should call /threads/{id}?format=metadata with header params");
    it.todo("should return messages array in chronological order");
  });

  describe("trash_thread", () => {
    it.todo("should POST to /threads/{id}/trash");
    it.todo("should return thread_id");
  });
});
