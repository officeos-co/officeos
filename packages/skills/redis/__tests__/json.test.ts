import { describe, it } from "bun:test";

describe("json", () => {
  describe("json_set", () => {
    it.todo("should send JSON.SET with key, path, and value");
    it.todo("should include NX flag when nx=true");
    it.todo("should include XX flag when xx=true");
    it.todo("should default path to $");
  });

  describe("json_get", () => {
    it.todo("should send JSON.GET with key and path");
    it.todo("should JSON.parse string result");
    it.todo("should keep string value when JSON.parse fails");
  });
});
