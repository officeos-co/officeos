import { describe, it, expect } from "bun:test";
import { VaultIndex } from "../core/search.ts";

function buildIndex(entries: { path: string; content: string }[]): VaultIndex {
  const index = new VaultIndex();
  index.build(entries);
  return index;
}

describe("VaultIndex", () => {
  const entries = [
    { path: "Projects/idea.md", content: "# Idea\nSee [[Meeting Notes]] and [[Todo]]." },
    { path: "Meeting Notes.md", content: "---\ntags:\n  - meetings\n---\n# Meeting\nDiscussed [[Projects/idea]]." },
    { path: "Todo.md", content: "---\ntags:\n  - tasks\n---\n# Todo List\n- Item 1\n- Item 2" },
    { path: "Orphan.md", content: "# Orphan\nNo links here, nobody links to me." },
  ];

  describe("getLinks", () => {
    it("should return outgoing links for a note", () => {
      const index = buildIndex(entries);
      const links = index.getLinks("Projects/idea.md");
      expect(links).toEqual(["Meeting Notes", "Todo"]);
    });

    it("should return empty array for note with no outgoing links", () => {
      const index = buildIndex(entries);
      const links = index.getLinks("Orphan.md");
      expect(links).toEqual([]);
    });
  });

  describe("getBacklinks", () => {
    it("should return incoming links for a note", () => {
      const index = buildIndex(entries);
      const backlinks = index.getBacklinks("Meeting Notes.md");
      expect(backlinks).toContain("Projects/idea.md");
    });

    it("should return empty array for orphan note", () => {
      const index = buildIndex(entries);
      const backlinks = index.getBacklinks("Orphan.md");
      expect(backlinks).toEqual([]);
    });
  });

  describe("getOrphans", () => {
    it("should find notes with zero incoming links", () => {
      const index = buildIndex(entries);
      const orphans = index.getOrphans();
      // Projects/idea.md is linked by Meeting Notes via [[Projects/idea]]
      // but the link target "Projects/idea" doesn't match basename "idea.md" -> orphan
      expect(orphans).toContain("Orphan.md");
    });
  });

  describe("getUnresolved", () => {
    it("should find links pointing to nonexistent notes", () => {
      const index = buildIndex([
        { path: "A.md", content: "See [[NonExistent]]." },
        { path: "B.md", content: "See [[A]]." },
      ]);
      const unresolved = index.getUnresolved();
      expect(unresolved).toContain("NonExistent");
      expect(unresolved).not.toContain("A");
    });
  });

  describe("getAllTags", () => {
    it("should extract tags from frontmatter", () => {
      const index = buildIndex(entries);
      const allTags = index.getAllTags();
      expect(allTags["meetings"]).toBe(1);
      expect(allTags["tasks"]).toBe(1);
    });
  });

  describe("searchContent", () => {
    it("should find matching lines case-insensitively", () => {
      const index = buildIndex(entries);
      const results = index.searchContent("item");
      expect(results.length).toBe(2);
      expect(results[0].path).toBe("Todo.md");
    });

    it("should include context when requested", () => {
      const index = buildIndex(entries);
      const results = index.searchContent("Item 1", true);
      expect(results.length).toBe(1);
      expect(results[0].context).toBeDefined();
      expect(results[0].context!.length).toBeGreaterThan(0);
    });

    it("should return empty for no matches", () => {
      const index = buildIndex(entries);
      const results = index.searchContent("zzz_no_match");
      expect(results).toEqual([]);
    });
  });

  describe("findByTag", () => {
    it("should find notes with a specific tag", () => {
      const index = buildIndex(entries);
      const matching = index.findByTag("meetings");
      expect(matching).toContain("Meeting Notes.md");
      expect(matching).not.toContain("Todo.md");
    });

    it("should return empty for unknown tag", () => {
      const index = buildIndex(entries);
      const matching = index.findByTag("nonexistent");
      expect(matching).toEqual([]);
    });
  });
});
