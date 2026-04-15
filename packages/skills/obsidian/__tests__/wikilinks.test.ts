import { describe, it, expect } from "bun:test";
import { extractWikilinks, replaceWikilinks } from "../core/wikilinks.ts";

describe("extractWikilinks", () => {
  it("should extract simple wikilinks", () => {
    const result = extractWikilinks("See [[Note A]] and [[Note B]].");
    expect(result).toEqual(["Note A", "Note B"]);
  });

  it("should strip display text after pipe", () => {
    const result = extractWikilinks("See [[Note A|my display]].");
    expect(result).toEqual(["Note A"]);
  });

  it("should strip heading after hash", () => {
    const result = extractWikilinks("See [[Note A#Section]].");
    expect(result).toEqual(["Note A"]);
  });

  it("should handle heading and display together", () => {
    const result = extractWikilinks("See [[Note A#Section|label]].");
    expect(result).toEqual(["Note A"]);
  });

  it("should ignore wikilinks inside fenced code blocks", () => {
    const text = "Before\n```\n[[Inside Code]]\n```\nAfter [[Outside]]";
    const result = extractWikilinks(text);
    expect(result).toEqual(["Outside"]);
  });

  it("should ignore wikilinks inside inline code", () => {
    const result = extractWikilinks("Use `[[Not a link]]` but [[Real link]]");
    expect(result).toEqual(["Real link"]);
  });

  it("should skip empty wikilinks", () => {
    const result = extractWikilinks("Empty [[]] and [[ ]] here.");
    expect(result).toEqual([]);
  });

  it("should return empty array for no wikilinks", () => {
    const result = extractWikilinks("Just plain text.");
    expect(result).toEqual([]);
  });
});

describe("replaceWikilinks", () => {
  it("should replace simple wikilinks", () => {
    const { text, count } = replaceWikilinks("See [[Old Name]] here.", "Old Name", "New Name");
    expect(text).toBe("See [[New Name]] here.");
    expect(count).toBe(1);
  });

  it("should preserve display text", () => {
    const { text } = replaceWikilinks("See [[Old|label]].", "Old", "New");
    expect(text).toBe("See [[New|label]].");
  });

  it("should preserve heading", () => {
    const { text } = replaceWikilinks("See [[Old#Section]].", "Old", "New");
    expect(text).toBe("See [[New#Section]].");
  });

  it("should preserve heading and display", () => {
    const { text } = replaceWikilinks("See [[Old#Section|label]].", "Old", "New");
    expect(text).toBe("See [[New#Section|label]].");
  });

  it("should be case-insensitive", () => {
    const { text, count } = replaceWikilinks("See [[old name]].", "Old Name", "New Name");
    expect(text).toBe("See [[New Name]].");
    expect(count).toBe(1);
  });

  it("should not modify wikilinks inside code blocks", () => {
    const input = "```\n[[Old]]\n```\n[[Old]]";
    const { text, count } = replaceWikilinks(input, "Old", "New");
    expect(text).toBe("```\n[[Old]]\n```\n[[New]]");
    expect(count).toBe(1);
  });

  it("should not modify wikilinks inside inline code", () => {
    const input = "`[[Old]]` and [[Old]]";
    const { text, count } = replaceWikilinks(input, "Old", "New");
    expect(text).toBe("`[[Old]]` and [[New]]");
    expect(count).toBe(1);
  });

  it("should replace multiple occurrences", () => {
    const { text, count } = replaceWikilinks("[[A]] then [[A]]", "A", "B");
    expect(text).toBe("[[B]] then [[B]]");
    expect(count).toBe(2);
  });

  it("should return count 0 when no matches", () => {
    const { text, count } = replaceWikilinks("No links here.", "Missing", "New");
    expect(text).toBe("No links here.");
    expect(count).toBe(0);
  });

  it("should handle empty text", () => {
    const { text, count } = replaceWikilinks("", "Old", "New");
    expect(text).toBe("");
    expect(count).toBe(0);
  });
});
