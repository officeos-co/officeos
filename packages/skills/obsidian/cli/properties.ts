/**
 * Property actions: get_properties, set_property, remove_property, query_by_property.
 *
 * Ported from obsctl's cli/properties.py.
 */

import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { readNote, writeNote, listNotes } from "../core/client.ts";
import {
  parseFrontmatter,
  setProperty,
  removeProperty,
} from "../core/frontmatter.ts";
import type { VaultCredentials } from "../core/types.ts";

function creds(ctx: { credentials: Record<string, string> }): VaultCredentials {
  return {
    couchdb_url: ctx.credentials.couchdb_url,
    couchdb_user: ctx.credentials.couchdb_user,
    couchdb_password: ctx.credentials.couchdb_password,
    vault_name: ctx.credentials.vault_name,
  };
}

export const properties: Record<string, ActionDefinition> = {
  get_properties: {
    description: "Get all frontmatter properties of a note.",
    params: z.object({
      path: z.string().describe("Note path"),
    }),
    returns: z.object({
      path: z.string(),
      properties: z.record(z.string(), z.unknown()),
    }),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const note = await readNote(ctx.fetch, c, params.path);
      if (!note) {
        throw new Error(`Note not found: ${params.path}`);
      }

      const [metadata] = parseFrontmatter(note.content);
      return { path: params.path, properties: metadata };
    },
  },

  set_property: {
    description: "Set a frontmatter property on a note. Creates frontmatter if needed.",
    params: z.object({
      path: z.string().describe("Note path"),
      key: z.string().describe("Property name"),
      value: z.string().describe("Property value"),
      type: z
        .enum(["text", "list", "number", "checkbox"])
        .default("text")
        .describe("Value type for coercion"),
    }),
    returns: z.object({
      path: z.string(),
      key: z.string(),
      value: z.unknown(),
      set: z.boolean(),
    }),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const note = await readNote(ctx.fetch, c, params.path);
      if (!note) {
        throw new Error(`Note not found: ${params.path}`);
      }

      // Type coercion
      let typedValue: unknown;
      if (params.type === "number") {
        typedValue = params.value.includes(".")
          ? parseFloat(params.value)
          : parseInt(params.value, 10);
        if (isNaN(typedValue as number)) {
          throw new Error(`Invalid number: ${params.value}`);
        }
      } else if (params.type === "checkbox") {
        typedValue = ["true", "yes", "1"].includes(params.value.toLowerCase());
      } else if (params.type === "list") {
        const [metadata] = parseFrontmatter(note.content);
        const existing = metadata[params.key];
        if (Array.isArray(existing)) {
          if (!existing.includes(params.value)) {
            existing.push(params.value);
          }
          typedValue = existing;
        } else {
          typedValue = [params.value];
        }
      } else {
        typedValue = params.value;
      }

      const newContent = setProperty(note.content, params.key, typedValue);
      await writeNote(ctx.fetch, c, params.path, newContent);
      return { path: params.path, key: params.key, value: typedValue, set: true };
    },
  },

  remove_property: {
    description: "Remove a frontmatter property from a note.",
    params: z.object({
      path: z.string().describe("Note path"),
      key: z.string().describe("Property name to remove"),
    }),
    returns: z.object({
      path: z.string(),
      removed: z.string(),
    }),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const note = await readNote(ctx.fetch, c, params.path);
      if (!note) {
        throw new Error(`Note not found: ${params.path}`);
      }

      const newContent = removeProperty(note.content, params.key);
      await writeNote(ctx.fetch, c, params.path, newContent);
      return { path: params.path, removed: params.key };
    },
  },

  query_by_property: {
    description:
      "Query notes by any frontmatter property -- a generalised filter.",
    params: z.object({
      property: z.string().describe("Frontmatter property name (e.g. status)"),
      value: z
        .string()
        .optional()
        .describe("Value to match (ignored when operator is exists)"),
      operator: z
        .enum(["eq", "contains", "exists"])
        .default("eq")
        .describe("eq: exact match, contains: substring/array member, exists: property present"),
      limit: z.number().min(1).max(200).default(50).describe("Max results"),
    }),
    returns: z.array(
      z.object({
        path: z.string(),
        value: z.unknown(),
      }),
    ),
    execute: async (params, ctx) => {
      const c = creds(ctx);
      const allNotes = await listNotes(ctx.fetch, c);
      const results: { path: string; value: unknown }[] = [];

      for (const meta of allNotes) {
        if (results.length >= params.limit) break;

        const note = await readNote(ctx.fetch, c, meta.path);
        if (!note) continue;

        const [metadata] = parseFrontmatter(note.content);
        const propValue = metadata[params.property];

        if (params.operator === "exists") {
          if (propValue !== undefined && propValue !== null) {
            results.push({ path: meta.path, value: propValue });
          }
        } else if (params.operator === "contains") {
          if (Array.isArray(propValue) && params.value !== undefined) {
            if (propValue.includes(params.value)) {
              results.push({ path: meta.path, value: propValue });
            }
          } else if (
            typeof propValue === "string" &&
            params.value !== undefined &&
            propValue.toLowerCase().includes(params.value.toLowerCase())
          ) {
            results.push({ path: meta.path, value: propValue });
          }
        } else {
          // eq
          if (params.value !== undefined && String(propValue) === params.value) {
            results.push({ path: meta.path, value: propValue });
          }
        }
      }

      return results;
    },
  },
};
