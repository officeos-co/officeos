/**
 * Backlink-aware rename/move orchestration.
 *
 * Ported from obsctl's core/backlinks.py. Scans the entire vault for notes
 * containing [[oldName]] wikilinks and rewrites them to [[newName]].
 */

import type { VaultCredentials, BacklinkUpdateResult } from "./types.ts";
import { replaceWikilinks } from "./wikilinks.ts";
import { listNotes, readNote, writeNote } from "./client.ts";

/**
 * Find and rewrite all wikilinks targeting oldName across the vault.
 *
 * Reads every note, checks for [[oldName]] references (case-insensitive),
 * and rewrites them to [[newName]]. Handles all wikilink variants.
 */
export async function updateBacklinks(
  fetchFn: typeof globalThis.fetch,
  creds: VaultCredentials,
  oldName: string,
  newName: string,
  dryRun = false,
): Promise<BacklinkUpdateResult> {
  const allNotes = await listNotes(fetchFn, creds);
  const details: { path: string; count: number }[] = [];
  let totalLinks = 0;

  for (const noteInfo of allNotes) {
    const note = await readNote(fetchFn, creds, noteInfo.path);
    if (!note) continue;

    const { text: newContent, count } = replaceWikilinks(
      note.content,
      oldName,
      newName,
    );

    if (count > 0) {
      totalLinks += count;
      details.push({ path: noteInfo.path, count });
      if (!dryRun) {
        await writeNote(fetchFn, creds, noteInfo.path, newContent);
      }
    }
  }

  return {
    total_links: totalLinks,
    total_notes: details.length,
    details,
  };
}
