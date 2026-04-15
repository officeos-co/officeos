import { describe, it } from "bun:test";

describe("env", () => {
  describe("list_env_vars", () => {
    it.todo("should call /v9/projects/:id/env");
    it.todo("should return env vars with nullable values");
  });

  describe("get_env_var", () => {
    it.todo("should call /v9/projects/:id/env/:envId");
  });

  describe("create_env_var", () => {
    it.todo("should POST to /v10/projects/:id/env");
    it.todo("should default target to all environments");
    it.todo("should default type to encrypted");
  });

  describe("update_env_var", () => {
    it.todo("should PATCH /v9/projects/:id/env/:envId with changed fields");
  });

  describe("delete_env_var", () => {
    it.todo("should DELETE /v9/projects/:id/env/:envId");
  });
});
