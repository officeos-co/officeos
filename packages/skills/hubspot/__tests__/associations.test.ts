import { describe, it, expect } from "bun:test";

describe("associations", () => {
  describe("create_association", () => {
    it.todo("should PUT to association endpoint with correct path");
    it.todo("should return from_id, to_id, association_type");
  });

  describe("list_associations", () => {
    it.todo("should GET /crm/v3/objects/:type/:id/associations/:toType");
    it.todo("should return array of id + type");
  });

  describe("remove_association", () => {
    it.todo("should DELETE association endpoint with correct path");
    it.todo("should return success: true");
  });
});
