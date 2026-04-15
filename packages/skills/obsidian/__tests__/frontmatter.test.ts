import { describe, it, expect } from "bun:test";
import {
  parseFrontmatter,
  buildNote,
  setProperty,
  removeProperty,
} from "../core/frontmatter.ts";

describe("parseFrontmatter", () => {
  it("should parse simple key-value pairs", () => {
    const content = "---\ntitle: My Note\nstatus: active\n---\n# Body";
    const [metadata, body] = parseFrontmatter(content);
    expect(metadata.title).toBe("My Note");
    expect(metadata.status).toBe("active");
    expect(body).toBe("# Body");
  });

  it("should parse tags as array", () => {
    const content = "---\ntags:\n  - project\n  - active\n---\nBody";
    const [metadata] = parseFrontmatter(content);
    expect(metadata.tags).toEqual(["project", "active"]);
  });

  it("should parse inline array", () => {
    const content = "---\ntags: [a, b, c]\n---\nBody";
    const [metadata] = parseFrontmatter(content);
    expect(metadata.tags).toEqual(["a", "b", "c"]);
  });

  it("should return empty metadata for no frontmatter", () => {
    const [metadata, body] = parseFrontmatter("# Just a heading");
    expect(metadata).toEqual({});
    expect(body).toBe("# Just a heading");
  });

  it("should handle boolean and number values", () => {
    const content = "---\ndraft: true\npriority: 5\n---\nBody";
    const [metadata] = parseFrontmatter(content);
    expect(metadata.draft).toBe(true);
    expect(metadata.priority).toBe(5);
  });
});

describe("buildNote", () => {
  it("should build a note with frontmatter", () => {
    const result = buildNote({ title: "Test" }, "# Body");
    expect(result).toBe("---\ntitle: Test\n---\n# Body");
  });

  it("should return just body when metadata is empty", () => {
    const result = buildNote({}, "# Body");
    expect(result).toBe("# Body");
  });
});

describe("setProperty", () => {
  it("should add a property to existing frontmatter", () => {
    const content = "---\ntitle: Test\n---\n# Body";
    const result = setProperty(content, "status", "active");
    const [metadata] = parseFrontmatter(result);
    expect(metadata.status).toBe("active");
    expect(metadata.title).toBe("Test");
  });

  it("should create frontmatter if none exists", () => {
    const result = setProperty("# Body", "title", "New");
    const [metadata, body] = parseFrontmatter(result);
    expect(metadata.title).toBe("New");
    expect(body).toBe("# Body");
  });
});

describe("removeProperty", () => {
  it("should remove a property", () => {
    const content = "---\ntitle: Test\nstatus: active\n---\n# Body";
    const result = removeProperty(content, "status");
    const [metadata] = parseFrontmatter(result);
    expect(metadata.status).toBeUndefined();
    expect(metadata.title).toBe("Test");
  });

  it("should be a no-op if property does not exist", () => {
    const content = "---\ntitle: Test\n---\n# Body";
    const result = removeProperty(content, "missing");
    const [metadata] = parseFrontmatter(result);
    expect(metadata.title).toBe("Test");
  });
});
