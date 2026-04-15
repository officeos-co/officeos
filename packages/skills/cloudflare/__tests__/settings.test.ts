import { describe, it } from "bun:test";

describe("settings", () => {
  describe("get_zone_settings", () => {
    it.todo("should call /zones/:id/settings");
    it.todo("should return settings as flat key-value record");
  });

  describe("update_zone_setting", () => {
    it.todo("should PATCH /zones/:id/settings/:setting");
    it.todo("should JSON parse the value before sending");
    it.todo("should keep string value when JSON parse fails");
  });
});
