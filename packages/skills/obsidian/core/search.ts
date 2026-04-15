/**
 * In-memory vault index for graph operations, tags, and full-text search.
 *
 * Ported from obsctl's core/index.py (VaultIndex class). Provides link graph
 * traversal, tag extraction, orphan detection, and content search.
 */

import type { SearchMatch, UnresolvedLink } from "./types.ts";
import { extractWikilinks } from "./wikilinks.ts";
import { parseFrontmatter } from "./frontmatter.ts";

/** A (path, content) pair used to build the index. */
export interface IndexEntry {
  path: string;
  content: string;
}

/** In-memory index of vault notes for graph and search operations. */
export class VaultIndex {
  notes: Map<string, string> = new Map();
  private _links: Map<string, string[]> = new Map();
  private _backlinks: Map<string, string[]> = new Map();
  private _tags: Map<string, number> = new Map();
  private _noteNames: Map<string, string> = new Map();

  /**
   * Build the index from a list of {path, content} entries.
   */
  build(entries: IndexEntry[]): void {
    this.notes.clear();
    this._links.clear();
    this._backlinks.clear();
    this._tags.clear();
    this._noteNames.clear();

    // First pass: index all notes and extract data
    for (const { path, content } of entries) {
      this.notes.set(path, content);

      // Build name -> path mapping (basename without .md, lowercase)
      const basename = path.includes("/") ? path.split("/").pop()! : path;
      const name = basename.toLowerCase().endsWith(".md")
        ? basename.slice(0, -3)
        : basename;
      this._noteNames.set(name.toLowerCase(), path);

      // Extract wikilinks (deduplicated, preserving order)
      const links = extractWikilinks(content);
      const unique = [...new Map(links.map((l) => [l, l])).values()];
      this._links.set(path, unique);

      // Extract tags from frontmatter
      const [metadata] = parseFrontmatter(content);
      const tags = metadata.tags;
      if (Array.isArray(tags)) {
        for (const tag of tags) {
          if (tag && String(tag).trim()) {
            const tagStr = String(tag).trim();
            this._tags.set(tagStr, (this._tags.get(tagStr) ?? 0) + 1);
          }
        }
      }
    }

    // Second pass: build backlinks
    for (const [path, links] of this._links) {
      for (const linkName of links) {
        const lower = linkName.toLowerCase();
        const existing = this._backlinks.get(lower) ?? [];
        existing.push(path);
        this._backlinks.set(lower, existing);
      }
    }
  }

  /** Get outgoing wikilink targets for a note. */
  getLinks(path: string): string[] {
    return this._links.get(path) ?? [];
  }

  /** Get incoming links for a note (case-insensitive on basename). */
  getBacklinks(path: string): string[] {
    const basename = path.includes("/") ? path.split("/").pop()! : path;
    const name = basename.toLowerCase().endsWith(".md")
      ? basename.slice(0, -3)
      : basename;
    return this._backlinks.get(name.toLowerCase()) ?? [];
  }

  /** Get wikilink targets that don't correspond to any existing note. */
  getUnresolved(): string[] {
    const unresolved = new Set<string>();

    for (const [, links] of this._links) {
      for (const linkName of links) {
        if (!this._noteNames.has(linkName.toLowerCase())) {
          unresolved.add(linkName);
        }
      }
    }

    return [...unresolved].sort();
  }

  /** Get notes with zero incoming links. */
  getOrphans(): string[] {
    const orphans: string[] = [];

    for (const path of this.notes.keys()) {
      const basename = path.includes("/") ? path.split("/").pop()! : path;
      const name = basename.toLowerCase().endsWith(".md")
        ? basename.slice(0, -3)
        : basename;
      const backlinks = this._backlinks.get(name.toLowerCase()) ?? [];
      if (backlinks.length === 0) {
        orphans.push(path);
      }
    }

    return orphans;
  }

  /** Get all tags with counts. */
  getAllTags(): Record<string, number> {
    return Object.fromEntries(this._tags);
  }

  /**
   * Full-text search across all note content (case-insensitive).
   */
  searchContent(query: string, context = false): SearchMatch[] {
    const results: SearchMatch[] = [];
    const queryLower = query.toLowerCase();

    for (const [path, content] of this.notes) {
      const lines = content.split("\n");
      for (let i = 0; i < lines.length; i++) {
        if (lines[i].toLowerCase().includes(queryLower)) {
          const match: SearchMatch = {
            path,
            line: lines[i],
            line_number: i + 1,
          };
          if (context) {
            const start = Math.max(0, i - 2);
            const end = Math.min(lines.length, i + 3);
            match.context = lines.slice(start, end);
          }
          results.push(match);
        }
      }
    }

    return results;
  }

  /**
   * Find notes matching a specific tag in frontmatter.
   */
  findByTag(tagName: string): string[] {
    const matching: string[] = [];

    for (const [path, content] of this.notes) {
      const [metadata] = parseFrontmatter(content);
      const tags = metadata.tags;
      if (Array.isArray(tags) && tags.includes(tagName)) {
        matching.push(path);
      }
    }

    return matching;
  }
}
