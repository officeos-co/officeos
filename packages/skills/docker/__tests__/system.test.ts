import { describe, it } from "bun:test";

describe("system", () => {
  describe("system_info", () => {
    it.todo("should call GET /info and map server version, os, cpus, memory");
    it.todo("should return container counts and storage driver");
  });

  describe("system_df", () => {
    it.todo("should call GET /system/df and return images, containers, volumes, build_cache");
  });

  describe("system_prune", () => {
    it.todo("should prune containers, images, and networks");
    it.todo("should also prune volumes when volumes=true");
    it.todo("should include dangling=false filter when all=true");
    it.todo("should sum space_reclaimed from all prune operations");
  });
});
