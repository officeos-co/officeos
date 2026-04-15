/**
 * Move and rename commands with backlink-aware wikilink rewriting.
 *
 * Ported from obsctl's cli/relocate.py.
 */

import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { readNote, writeNote, moveNote, listNotes } from "../core/client.ts";
import { updateBacklinks } from "../core/backlinks.ts";
import { enforceGuardrails } from "../core/guardrails.ts";
import type { VaultCredentials } from "../core/types.ts";

function creds(ctx: { credentials: Record<string, string> }): VaultCredentials {
  return {
    couchdb_url: ctx.credentials.couchdb_url,
    couchdb_user: ctx.credentials.couchdb_user,
    couchdb_password: ctx.credentials.couchdb_password,
    vault_name: ctx.credentials.vault_name,
  };
}

/** Extract basename without .md extension from a vault path. */
function basenameNoExt(path: string): string {
  const base = path.split("/").pop() ?? path;
  return base.endsWith(".md") ? base.slice(0, -3) : base;
}

/**
 * Resolve a loose note name to its full vault path.
 * Tries: exact path → name.md → basename match.
 * Throws if not found.
 */
async function resolveFile(
  fetchFn: typeof globalThis.fetch,
  c: VaultCredentials,
  fileName: string,
): Promise<string> {
  const notes = await listNotes(fetchFn, c);
  const nameLower = fileName.toLowerCase();
  const nameMd = nameLower.endsWith(".md") ? nameLower : nameLower + ".md";

  // 1. Exact path match
  for (const n of notes) {
    if (n.path.toLowerCase() === nameLower) return n.path;
  }
  // 2. name.md match
  for (const n of notes) {
    if (n.path.toLowerCase() === nameMd) return n.path;
  }
  // 3. Basename match (without extension)
  for (const n of notes) {
    const base = n.path.split("/").pop() ?? n.path;
    const baseNoExt = base.endsWith(".md") ? base.slice(0, -3).toLowerCase() : base.toLowerCase();
    if (baseNoExt === nameLower || base.toLowerCase() === nameMd) return n.path;
  }

  throw new Error(`Note not found: ${fileName}`);
}

export const relocate: Record<string, ActionDefinition> = {
  move_note: {
    description:
      "Move a note to a new path. Automatically rewrites all [[wikilinks]] that reference the note if its filename changes. Returns guardrail warnings if the destination violates vault design rules.",
    params: z.object({
      file: z.string().describe("Source note name or path"),
      to: z.string().describe("Destination vault path (e.g. References/My Note.md)"),
      dry_run: z
        .boolean()
        .default(false)
        .describe("Report what would happen without making changes"),
      no_backlinks: z
        .boolean()
        .default(false)
        .describe("Skip backlink rewriting even if the filename changes"),
      strict: z
        .boolean()
        .default(false)
        .describe("Treat vault rule violations as hard errors instead of warnings"),
    }),
    returns: z.object({
      moved: z.string().describe("'source -> destination' description"),
      backlinks: z
        .object({
          total_links: z.number(),
          total_notes: z.number(),
          details: z.array(z.object({ path: z.string(), count: z.number() })),
        })
        .optional(),
      warnings: z.array(z.string()).describe("Vault rule violation warnings"),
      dry_run: z.boolean(),
    }),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const sourcePath = await resolveFile(ctx.fetch, c, params.file);

      // Read source content for guardrail checks
      const note = await readNote(ctx.fetch, c, sourcePath);
      const noteContent = note?.content ?? "";

      // Vault rule guardrails on the destination
      const warnings = await enforceGuardrails(
        ctx.fetch,
        c,
        params.to,
        noteContent,
        params.strict,
      );

      const oldName = basenameNoExt(sourcePath);
      const newName = basenameNoExt(params.to);
      const nameChanged = oldName.toLowerCase() !== newName.toLowerCase();

      if (params.dry_run) {
        let backlinkDryRun = undefined;
        if (nameChanged && !params.no_backlinks) {
          backlinkDryRun = await updateBacklinks(
            ctx.fetch,
            c,
            oldName,
            newName,
            true,
          );
        }
        return {
          moved: `${sourcePath} -> ${params.to}`,
          backlinks: backlinkDryRun ?? undefined,
          warnings,
          dry_run: true,
        };
      }

      await moveNote(ctx.fetch, c, sourcePath, params.to);

      let backlinkResult = undefined;
      if (nameChanged && !params.no_backlinks) {
        backlinkResult = await updateBacklinks(ctx.fetch, c, oldName, newName);
      }

      return {
        moved: `${sourcePath} -> ${params.to}`,
        backlinks: backlinkResult,
        warnings,
        dry_run: false,
      };
    },
  },

  rename_note: {
    description:
      "Rename a note (keeps the same folder). Automatically rewrites all [[wikilinks]] referencing the old name across the entire vault.",
    params: z.object({
      file: z.string().describe("Note name or path to rename"),
      name: z.string().describe("New note name (without path or extension)"),
      dry_run: z
        .boolean()
        .default(false)
        .describe("Report what would happen without making changes"),
      no_backlinks: z
        .boolean()
        .default(false)
        .describe("Skip backlink rewriting"),
    }),
    returns: z.object({
      renamed: z.string().describe("'source -> destination' description"),
      backlinks: z
        .object({
          total_links: z.number(),
          total_notes: z.number(),
          details: z.array(z.object({ path: z.string(), count: z.number() })),
        })
        .optional(),
      dry_run: z.boolean(),
    }),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const sourcePath = await resolveFile(ctx.fetch, c, params.file);

      // Keep same folder, change filename
      const slashIdx = sourcePath.lastIndexOf("/");
      const folder = slashIdx >= 0 ? sourcePath.slice(0, slashIdx) : "";
      const newBasename = params.name.endsWith(".md")
        ? params.name
        : params.name + ".md";
      const newPath = folder ? `${folder}/${newBasename}` : newBasename;

      const oldName = basenameNoExt(sourcePath);
      const newName = basenameNoExt(newPath);

      if (params.dry_run) {
        let backlinkDryRun = undefined;
        if (!params.no_backlinks) {
          backlinkDryRun = await updateBacklinks(
            ctx.fetch,
            c,
            oldName,
            newName,
            true,
          );
        }
        return {
          renamed: `${sourcePath} -> ${newPath}`,
          backlinks: backlinkDryRun ?? undefined,
          dry_run: true,
        };
      }

      await moveNote(ctx.fetch, c, sourcePath, newPath);

      let backlinkResult = undefined;
      if (!params.no_backlinks) {
        backlinkResult = await updateBacklinks(ctx.fetch, c, oldName, newName);
      }

      return {
        renamed: `${sourcePath} -> ${newPath}`,
        backlinks: backlinkResult,
        dry_run: false,
      };
    },
  },
};
