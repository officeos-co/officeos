import { describe, it } from "bun:test";

describe("datasources", () => {
  describe("list_datasources", () => {
    it.todo("should call /api/datasources");
    it.todo("should map isDefault to is_default");
  });

  describe("get_datasource", () => {
    it.todo("should call /api/datasources/uid/:uid");
    it.todo("should return jsonData as json_data");
  });

  describe("create_datasource", () => {
    it.todo("should POST to /api/datasources");
    it.todo("should include jsonData when json_data is provided");
  });

  describe("delete_datasource", () => {
    it.todo("should DELETE /api/datasources/uid/:uid");
  });
});
