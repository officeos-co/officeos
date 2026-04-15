import { describe, it, expect } from "bun:test";

describe("move_note", () => {
  it.todo("resolves source note by name (exact, name.md, basename)");
  it.todo("calls moveNote with source path and destination path");
  it.todo("rewrites backlinks when filename changes");
  it.todo("skips backlink rewriting when no_backlinks=true");
  it.todo("skips backlink rewriting when filename unchanged (folder-only move)");
  it.todo("returns dry_run report without writing when dry_run=true");
  it.todo("returns guardrail warnings for unknown destination folder");
  it.todo("throws on guardrail violation when strict=true");
});

describe("rename_note", () => {
  it.todo("renames note in same folder");
  it.todo("appends .md if new name does not include it");
  it.todo("rewrites backlinks across vault to new name");
  it.todo("skips backlink rewriting when no_backlinks=true");
  it.todo("returns dry_run report without writing when dry_run=true");
  it.todo("throws when source note does not exist");
});
