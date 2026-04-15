/**
 * CRUD actions: read, create, write, append, prepend, delete.
 *
 * Ported from obsctl's cli/crud.py. Thin glue between Zod schemas and core/client.
 */

import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { readNote, writeNote, deleteNote } from "../core/client.ts";
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

export const crud: Record<string, ActionDefinition> = {
  read_note: {
    description: "Read a note's content by path.",
    params: z.object({
      path: z.string().describe("Note path in the vault (e.g. Daily/2026-04-12.md)"),
    }),
    returns: z.object({
      path: z.string(),
      content: z.string(),
      ctime: z.number().optional(),
      mtime: z.number().optional(),
    }),
    execute: async (params, ctx) => {
      const note = await readNote(ctx.fetch, creds(ctx), params.path);
      if (!note) {
        throw new Error(`Note not found: ${params.path}`);
      }
      return {
        path: note.path,
        content: note.content,
        ctime: note.ctime,
        mtime: note.mtime,
      };
    },
  },

  create_note: {
    description:
      "Create a new note. Fails if the note already exists. Returns guardrail warnings if the path violates vault design rules (wrong folder, missing categories, etc.).",
    params: z.object({
      name: z.string().describe("Note name (e.g. my-idea)"),
      content: z.string().default("").describe("Markdown content"),
      folder: z.string().optional().describe("Target folder (e.g. Projects)"),
      strict: z
        .boolean()
        .default(false)
        .describe("Treat vault rule violations as hard errors instead of warnings"),
    }),
    returns: z.object({
      path: z.string(),
      created: z.boolean(),
      warnings: z.array(z.string()).describe("Vault rule violation warnings"),
    }),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      let name = params.name;
      if (!name.endsWith(".md")) name += ".md";
      const path = params.folder ? `${params.folder}/${name}` : name;

      // Check if note already exists
      const existing = await readNote(ctx.fetch, c, path);
      if (existing) {
        throw new Error(`Note already exists: ${path}. Use write_note with force=true to overwrite.`);
      }

      // Vault rule guardrails
      const warnings = await enforceGuardrails(
        ctx.fetch,
        c,
        path,
        params.content,
        params.strict,
      );

      await writeNote(ctx.fetch, c, path, params.content);
      return { path, created: true, warnings };
    },
  },

  write_note: {
    description:
      "Write (overwrite) a note's content. Creates the note if it does not exist. Returns guardrail warnings if the path violates vault design rules.",
    params: z.object({
      path: z.string().describe("Exact path from vault root"),
      content: z.string().describe("New markdown content"),
      force: z.boolean().default(false).describe("Overwrite existing note without warning"),
      strict: z
        .boolean()
        .default(false)
        .describe("Treat vault rule violations as hard errors instead of warnings"),
    }),
    returns: z.object({
      path: z.string(),
      written: z.boolean(),
      warnings: z.array(z.string()).describe("Vault rule violation warnings"),
    }),
    execute: async (params, ctx) => {
      const c = creds(ctx);

      if (!params.force) {
        const existing = await readNote(ctx.fetch, c, params.path);
        if (existing) {
          throw new Error(
            `Note already exists (${existing.content.length} chars). Use force=true to overwrite.`,
          );
        }
      }

      // Vault rule guardrails
      const warnings = await enforceGuardrails(
        ctx.fetch,
        c,
        params.path,
        params.content,
        params.strict,
      );

      await writeNote(ctx.fetch, c, params.path, params.content);
      return { path: params.path, written: true, warnings };
    },
  },

  append_note: {
    description: "Append content to an existing note.",
    params: z.object({
      path: z.string().describe("Note path"),
      content: z.string().describe("Content to append"),
      inline: z.boolean().default(false).describe("No newline separator before appended text"),
    }),
    returns: z.object({ path: z.string(), appended: z.boolean() }),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const note = await readNote(ctx.fetch, c, params.path);
      if (!note) {
        throw new Error(`Note not found: ${params.path}`);
      }

      const separator = params.inline ? "" : "\n";
      const newContent = note.content + separator + params.content;
      await writeNote(ctx.fetch, c, params.path, newContent);
      return { path: params.path, appended: true };
    },
  },

  prepend_note: {
    description: "Prepend content to an existing note.",
    params: z.object({
      path: z.string().describe("Note path"),
      content: z.string().describe("Content to prepend"),
    }),
    returns: z.object({ path: z.string(), prepended: z.boolean() }),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const note = await readNote(ctx.fetch, c, params.path);
      if (!note) {
        throw new Error(`Note not found: ${params.path}`);
      }

      const newContent = params.content + "\n" + note.content;
      await writeNote(ctx.fetch, c, params.path, newContent);
      return { path: params.path, prepended: true };
    },
  },

  delete_note: {
    description: "Delete a note (soft-delete, LiveSync compatible).",
    params: z.object({
      path: z.string().describe("Note path to delete"),
    }),
    returns: z.object({ path: z.string(), deleted: z.boolean() }),
    execute: async (params, ctx) => {
      await deleteNote(ctx.fetch, creds(ctx), params.path);
      return { path: params.path, deleted: true };
    },
  },
};
