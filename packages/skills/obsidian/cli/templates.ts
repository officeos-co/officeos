/**
 * Template actions: list_templates, read_template.
 *
 * Ported from obsctl's cli/templates.py.
 */

import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { listNotes, readNote } from "../core/client.ts";
import type { VaultCredentials } from "../core/types.ts";

const TEMPLATES_FOLDER = "Templates";

function creds(ctx: { credentials: Record<string, string> }): VaultCredentials {
  return {
    couchdb_url: ctx.credentials.couchdb_url,
    couchdb_user: ctx.credentials.couchdb_user,
    couchdb_password: ctx.credentials.couchdb_password,
    vault_name: ctx.credentials.vault_name,
  };
}

export const templates: Record<string, ActionDefinition> = {
  list_templates: {
    description: "List available templates in the Templates folder.",
    params: z.object({}),
    returns: z.array(z.string()),
    execute: async (_params, ctx) => {
      const c = creds(ctx);
      const notes = await listNotes(ctx.fetch, c);

      const templateNotes = notes
        .filter(
          (n) =>
            n.path.toLowerCase().startsWith(TEMPLATES_FOLDER.toLowerCase() + "/") &&
            n.path.toLowerCase().endsWith(".md"),
        )
        .map((n) => {
          // Show just the template name without the folder prefix
          return n.path.includes("/") ? n.path.split("/").slice(1).join("/") : n.path;
        })
        .sort();

      return templateNotes;
    },
  },

  read_template: {
    description: "Read a template's content.",
    params: z.object({
      name: z.string().describe("Template name (e.g. daily-note)"),
    }),
    returns: z.object({
      name: z.string(),
      path: z.string(),
      content: z.string(),
    }),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const templateName = params.name.endsWith(".md") ? params.name : params.name + ".md";
      const path = `${TEMPLATES_FOLDER}/${templateName}`;

      const note = await readNote(ctx.fetch, c, path);
      if (!note) {
        throw new Error(`Template not found: ${path}`);
      }

      return { name: params.name, path, content: note.content };
    },
  },
};
