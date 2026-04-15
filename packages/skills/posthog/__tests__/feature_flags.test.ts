import { describe, it } from "bun:test";

describe("feature_flags", () => {
  describe("list_feature_flags", () => {
    it.todo("should call /api/projects/{id}/feature_flags/");
    it.todo("should extract rollout_percentage from filters");
  });

  describe("get_feature_flag", () => {
    it.todo("should call /api/projects/{id}/feature_flags/{flag_id}/");
    it.todo("should return ensure_experience_continuity field");
  });

  describe("create_feature_flag", () => {
    it.todo("should POST to /api/projects/{id}/feature_flags/ with key and active");
    it.todo("should build filters with rollout_percentage when no custom filters");
    it.todo("should use provided filters object when given");
  });

  describe("update_feature_flag", () => {
    it.todo("should PATCH /api/projects/{id}/feature_flags/{flag_id}/");
    it.todo("should only send provided fields");
    it.todo("should build filters when rollout_percentage is updated");
  });
});
