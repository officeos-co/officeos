/**
 * Tag actions: list_tags, find_by_tag.
 *
 * Ported from obsctl's cli/tags.py.
 */

import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { listNotes, readNote } from "../core/client.ts";
import { VaultIndex } from "../core/search.ts";
import type { VaultCredentials } from "../core/types.ts";

function creds(ctx: { credentials: Record<string, string> }): VaultCredentials {
  return {
    couchdb_url: ctx.credentials.couchdb_url,
    couchdb_user: ctx.credentials.couchdb_user,
    couchdb_password: ctx.credentials.couchdb_password,
    vault_name: ctx.credentials.vault_name,
  };
}

async function loadIndex(
  fetchFn: typeof globalThis.fetch,
  c: VaultCredentials,
): Promise<VaultIndex> {
  const notesMeta = await listNotes(fetchFn, c);
  const entries: { path: string; content: string }[] = [];

  for (const meta of notesMeta) {
    const note = await readNote(fetchFn, c, meta.path);
    if (note) {
      entries.push({ path: note.path, content: note.content });
    }
  }

  const index = new VaultIndex();
  index.build(entries);
  return index;
}

export const tags: Record<string, ActionDefinition> = {
  list_tags: {
    description: "List all tags used across the vault with counts.",
    params: z.object({
      sort: z
        .enum(["name", "count"])
        .default("name")
        .describe("Sort order: by name or by count"),
    }),
    returns: z.array(
      z.object({ tag: z.string(), count: z.number() }),
    ),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const index = await loadIndex(ctx.fetch, c);
      const allTags = index.getAllTags();

      let entries = Object.entries(allTags).map(([tag, count]) => ({ tag, count }));

      if (params.sort === "count") {
        entries.sort((a, b) => b.count - a.count);
      } else {
        entries.sort((a, b) => a.tag.localeCompare(b.tag));
      }

      return entries;
    },
  },

  find_by_tag: {
    description: "Find notes with a specific tag in frontmatter.",
    params: z.object({
      tag: z.string().describe("Tag name (without #)"),
    }),
    returns: z.object({
      tag: z.string(),
      notes: z.array(z.string()),
      count: z.number(),
    }),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const index = await loadIndex(ctx.fetch, c);
      const matching = index.findByTag(params.tag).sort();
      return { tag: params.tag, notes: matching, count: matching.length };
    },
  },
};
