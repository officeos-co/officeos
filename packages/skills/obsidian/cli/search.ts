/**
 * Search action: full-text search across vault content.
 *
 * Ported from obsctl's cli/search.py.
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

export const search: Record<string, ActionDefinition> = {
  search: {
    description: "Full-text search across vault note content.",
    params: z.object({
      query: z.string().describe("Search query (case-insensitive)"),
      folder: z.string().optional().describe("Restrict search to a folder"),
      limit: z.number().min(1).max(100).default(10).describe("Max results"),
      context: z.boolean().default(false).describe("Include surrounding lines in results"),
    }),
    returns: z.array(
      z.object({
        path: z.string(),
        line: z.string(),
        line_number: z.number(),
        context: z.array(z.string()).optional(),
      }),
    ),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const index = await loadIndex(ctx.fetch, c);

      let results = index.searchContent(params.query, params.context);

      // Filter by folder
      if (params.folder) {
        const folderLower = params.folder.toLowerCase().replace(/\/$/, "");
        results = results.filter((r) =>
          r.path.toLowerCase().startsWith(folderLower + "/"),
        );
      }

      // Apply limit
      return results.slice(0, params.limit);
    },
  },
};
