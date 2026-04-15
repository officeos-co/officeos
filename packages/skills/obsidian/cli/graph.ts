/**
 * Graph traversal actions: backlinks, links, orphans, unresolved.
 *
 * Ported from obsctl's cli/graph.py. Builds an in-memory VaultIndex
 * and delegates to its graph methods.
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

/** Load all notes and build an in-memory VaultIndex. */
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

export const graph: Record<string, ActionDefinition> = {
  get_backlinks: {
    description: "Get notes that link TO this note (incoming wikilinks).",
    params: z.object({
      path: z.string().describe("Note path"),
    }),
    returns: z.object({
      path: z.string(),
      backlinks: z.array(z.string()),
      count: z.number(),
    }),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const index = await loadIndex(ctx.fetch, c);
      const results = index.getBacklinks(params.path);
      return { path: params.path, backlinks: results.sort(), count: results.length };
    },
  },

  get_links: {
    description: "Get outgoing wikilinks FROM this note.",
    params: z.object({
      path: z.string().describe("Note path"),
    }),
    returns: z.object({
      path: z.string(),
      links: z.array(z.string()),
    }),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const index = await loadIndex(ctx.fetch, c);
      const results = index.getLinks(params.path);
      return { path: params.path, links: results };
    },
  },

  find_orphans: {
    description: "Find notes with zero incoming links.",
    params: z.object({}),
    returns: z.object({
      orphans: z.array(z.string()),
      count: z.number(),
    }),
    execute: async (_params, ctx) => {
      const c = creds(ctx);
      const index = await loadIndex(ctx.fetch, c);
      const results = index.getOrphans().sort();
      return { orphans: results, count: results.length };
    },
  },

  find_unresolved: {
    description: "Find wikilinks pointing to notes that don't exist.",
    params: z.object({}),
    returns: z.object({
      unresolved: z.array(z.string()),
      count: z.number(),
    }),
    execute: async (_params, ctx) => {
      const c = creds(ctx);
      const index = await loadIndex(ctx.fetch, c);
      const results = index.getUnresolved();
      return { unresolved: results, count: results.length };
    },
  },
};
