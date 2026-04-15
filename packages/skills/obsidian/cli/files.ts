/**
 * File and folder listing actions.
 *
 * Ported from obsctl's cli/files.py.
 */

import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { listNotes, moveNote } from "../core/client.ts";
import type { VaultCredentials } from "../core/types.ts";

function creds(ctx: { credentials: Record<string, string> }): VaultCredentials {
  return {
    couchdb_url: ctx.credentials.couchdb_url,
    couchdb_user: ctx.credentials.couchdb_user,
    couchdb_password: ctx.credentials.couchdb_password,
    vault_name: ctx.credentials.vault_name,
  };
}

export const files: Record<string, ActionDefinition> = {
  list_files: {
    description: "List all files in the vault, optionally filtered by folder or extension.",
    params: z.object({
      folder: z.string().optional().describe("Filter by folder prefix"),
      ext: z.string().optional().describe("Filter by extension (e.g. md)"),
    }),
    returns: z.array(z.string()),
    execute: async (params, ctx) => {
      const notes = await listNotes(ctx.fetch, creds(ctx));
      let filtered = notes;

      if (params.folder) {
        const folderLower = params.folder.toLowerCase().replace(/\/$/, "");
        filtered = filtered.filter((n) =>
          n.path.toLowerCase().startsWith(folderLower + "/"),
        );
      }

      if (params.ext) {
        const extDot = params.ext.startsWith(".") ? params.ext : "." + params.ext;
        filtered = filtered.filter((n) =>
          n.path.toLowerCase().endsWith(extDot.toLowerCase()),
        );
      }

      return filtered.map((n) => n.path).sort();
    },
  },

  list_folders: {
    description: "List all folders in the vault.",
    params: z.object({}),
    returns: z.array(z.string()),
    execute: async (_params, ctx) => {
      const notes = await listNotes(ctx.fetch, creds(ctx));
      const folderSet = new Set<string>();

      for (const note of notes) {
        if (note.path.includes("/")) {
          const folder = note.path.slice(0, note.path.lastIndexOf("/"));
          folderSet.add(folder);
        }
      }

      return [...folderSet].sort();
    },
  },

  move_note: {
    description: "Move a note from one path to another.",
    params: z.object({
      from: z.string().describe("Current note path"),
      to: z.string().describe("Destination path"),
    }),
    returns: z.object({ from: z.string(), to: z.string(), moved: z.boolean() }),
    execute: async (params, ctx) => {
      await moveNote(ctx.fetch, creds(ctx), params.from, params.to);
      return { from: params.from, to: params.to, moved: true };
    },
  },

  rename_note: {
    description: "Rename a note (keeps it in the same folder).",
    params: z.object({
      path: z.string().describe("Current note path"),
      new_name: z.string().describe("New file name (e.g. new-name.md)"),
    }),
    returns: z.object({ old_path: z.string(), new_path: z.string(), renamed: z.boolean() }),
    execute: async (params, ctx) => {
      const folder = params.path.includes("/")
        ? params.path.slice(0, params.path.lastIndexOf("/"))
        : "";
      const newPath = folder ? `${folder}/${params.new_name}` : params.new_name;
      await moveNote(ctx.fetch, creds(ctx), params.path, newPath);
      return { old_path: params.path, new_path: newPath, renamed: true };
    },
  },
};
