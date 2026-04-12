import { defineSkill, z } from "@harro/skill-sdk";
import { execFile } from "node:child_process";
import doc from "./SKILL.md";

/**
 * Run an obsctl CLI command and return stdout.
 * Credentials are passed as environment variables so they never appear in argv.
 */
function obsctl(
  args: string[],
  creds: Record<string, string>,
): Promise<string> {
  return new Promise((resolve, reject) => {
    execFile(
      "obsctl",
      args,
      {
        env: {
          ...process.env,
          OBSCTL_COUCHDB_URL: creds.couchdb_url,
          OBSCTL_COUCHDB_USER: creds.couchdb_user,
          OBSCTL_COUCHDB_PASSWORD: creds.couchdb_password,
          OBSCTL_VAULT: creds.vault_name,
        },
        maxBuffer: 10 * 1024 * 1024,
        timeout: 30_000,
      },
      (err, stdout, stderr) => {
        if (err) {
          reject(new Error(stderr || err.message));
        } else {
          resolve(stdout);
        }
      },
    );
  });
}

/** Parse JSON output from obsctl, falling back to raw text. */
function parse(output: string): unknown {
  try {
    return JSON.parse(output);
  } catch {
    return output.trim();
  }
}

export default defineSkill({
  name: "obsidian",
  title: "Obsidian",
  emoji: "\ud83d\udcce",
  description:
    "Manage an Obsidian vault backed by CouchDB — read, write, search, graph, tags, templates.",
  doc,

  credentials: {
    couchdb_url: {
      label: "CouchDB URL",
      kind: "text",
      placeholder: "https://couch.example.com",
      help: "Full URL to your CouchDB instance (Self-hosted LiveSync backend).",
    },
    couchdb_user: {
      label: "CouchDB Username",
      kind: "text",
      placeholder: "admin",
    },
    couchdb_password: {
      label: "CouchDB Password",
      kind: "password",
    },
    vault_name: {
      label: "Vault Name",
      kind: "text",
      placeholder: "my-vault",
      help: "CouchDB database name for this vault.",
    },
  },

  actions: {
    // ── CRUD ──────────────────────────────────────────────

    read_note: {
      description: "Read a note by path.",
      params: z.object({
        path: z.string().describe("Note path in the vault"),
      }),
      returns: z.object({ path: z.string(), content: z.string() }),
      execute: async (params, ctx) => {
        const out = await obsctl(["read", params.path], ctx.credentials);
        return { path: params.path, content: out };
      },
    },

    create_note: {
      description: "Create a new note.",
      params: z.object({
        path: z.string().describe("Path for the new note"),
        content: z.string().describe("Markdown content"),
        frontmatter: z
          .string()
          .optional()
          .describe("YAML frontmatter (without fences)"),
      }),
      returns: z.object({ path: z.string(), created: z.boolean() }),
      execute: async (params, ctx) => {
        const args = ["create", params.path, "--content", params.content];
        if (params.frontmatter) {
          args.push("--frontmatter", params.frontmatter);
        }
        await obsctl(args, ctx.credentials);
        return { path: params.path, created: true };
      },
    },

    write_note: {
      description: "Overwrite a note's content.",
      params: z.object({
        path: z.string().describe("Note path"),
        content: z.string().describe("New markdown content"),
      }),
      returns: z.object({ path: z.string(), written: z.boolean() }),
      execute: async (params, ctx) => {
        await obsctl(
          ["write", params.path, "--content", params.content],
          ctx.credentials,
        );
        return { path: params.path, written: true };
      },
    },

    append_note: {
      description: "Append content to a note.",
      params: z.object({
        path: z.string().describe("Note path"),
        content: z.string().describe("Content to append"),
      }),
      returns: z.object({ path: z.string(), appended: z.boolean() }),
      execute: async (params, ctx) => {
        await obsctl(
          ["append", params.path, "--content", params.content],
          ctx.credentials,
        );
        return { path: params.path, appended: true };
      },
    },

    prepend_note: {
      description: "Prepend content to a note.",
      params: z.object({
        path: z.string().describe("Note path"),
        content: z.string().describe("Content to prepend"),
      }),
      returns: z.object({ path: z.string(), prepended: z.boolean() }),
      execute: async (params, ctx) => {
        await obsctl(
          ["prepend", params.path, "--content", params.content],
          ctx.credentials,
        );
        return { path: params.path, prepended: true };
      },
    },

    delete_note: {
      description: "Delete a note.",
      params: z.object({
        path: z.string().describe("Note path"),
        force: z.boolean().default(false).describe("Skip confirmation"),
      }),
      returns: z.object({ path: z.string(), deleted: z.boolean() }),
      execute: async (params, ctx) => {
        const args = ["delete", params.path];
        if (params.force) args.push("--force");
        await obsctl(args, ctx.credentials);
        return { path: params.path, deleted: true };
      },
    },

    // ── Navigation ───────────────────────────────────────

    list_files: {
      description: "List files in the vault or a specific folder.",
      params: z.object({
        folder: z
          .string()
          .optional()
          .describe("Folder path (root if omitted)"),
        pattern: z.string().optional().describe("Glob pattern filter"),
      }),
      returns: z.array(z.string()),
      execute: async (params, ctx) => {
        const args = ["files", "list"];
        if (params.folder) args.push("--folder", params.folder);
        if (params.pattern) args.push("--pattern", params.pattern);
        const out = await obsctl(args, ctx.credentials);
        return parse(out);
      },
    },

    list_folders: {
      description: "List all folders in the vault.",
      params: z.object({}),
      returns: z.array(z.string()),
      execute: async (_params, ctx) => {
        const out = await obsctl(["files", "folders"], ctx.credentials);
        return parse(out);
      },
    },

    move_note: {
      description: "Move a note to a new path.",
      params: z.object({
        from: z.string().describe("Current note path"),
        to: z.string().describe("Destination path"),
      }),
      returns: z.object({ from: z.string(), to: z.string(), moved: z.boolean() }),
      execute: async (params, ctx) => {
        await obsctl(["move", params.from, params.to], ctx.credentials);
        return { from: params.from, to: params.to, moved: true };
      },
    },

    rename_note: {
      description: "Rename a note.",
      params: z.object({
        path: z.string().describe("Current note path"),
        new_name: z.string().describe("New file name"),
      }),
      returns: z.object({
        path: z.string(),
        new_name: z.string(),
        renamed: z.boolean(),
      }),
      execute: async (params, ctx) => {
        await obsctl(
          ["rename", params.path, params.new_name],
          ctx.credentials,
        );
        return { path: params.path, new_name: params.new_name, renamed: true };
      },
    },

    // ── Graph ────────────────────────────────────────────

    get_backlinks: {
      description: "Get notes that link to this note.",
      params: z.object({
        path: z.string().describe("Note path"),
      }),
      returns: z.array(z.string()),
      execute: async (params, ctx) => {
        const out = await obsctl(
          ["graph", "backlinks", params.path],
          ctx.credentials,
        );
        return parse(out);
      },
    },

    get_links: {
      description: "Get outgoing links from a note.",
      params: z.object({
        path: z.string().describe("Note path"),
      }),
      returns: z.array(z.string()),
      execute: async (params, ctx) => {
        const out = await obsctl(
          ["graph", "links", params.path],
          ctx.credentials,
        );
        return parse(out);
      },
    },

    find_orphans: {
      description: "Find notes with no incoming or outgoing links.",
      params: z.object({}),
      returns: z.array(z.string()),
      execute: async (_params, ctx) => {
        const out = await obsctl(["graph", "orphans"], ctx.credentials);
        return parse(out);
      },
    },

    find_unresolved: {
      description: "Find broken/unresolved wikilinks in the vault.",
      params: z.object({}),
      returns: z.array(z.object({ source: z.string(), link: z.string() })),
      execute: async (_params, ctx) => {
        const out = await obsctl(["graph", "unresolved"], ctx.credentials);
        return parse(out);
      },
    },

    // ── Search & Tags ────────────────────────────────────

    search: {
      description: "Full-text search across the vault.",
      params: z.object({
        query: z.string().describe("Search query"),
        limit: z.number().min(1).max(100).default(10).describe("Max results"),
      }),
      returns: z.array(
        z.object({ path: z.string(), snippet: z.string() }),
      ),
      execute: async (params, ctx) => {
        const out = await obsctl(
          ["search", params.query, "--limit", String(params.limit)],
          ctx.credentials,
        );
        return parse(out);
      },
    },

    list_tags: {
      description: "List all tags used in the vault.",
      params: z.object({}),
      returns: z.array(z.string()),
      execute: async (_params, ctx) => {
        const out = await obsctl(["tags", "list"], ctx.credentials);
        return parse(out);
      },
    },

    find_by_tag: {
      description: "Find notes with a specific tag.",
      params: z.object({
        tag: z.string().describe("Tag name (without #)"),
      }),
      returns: z.array(z.string()),
      execute: async (params, ctx) => {
        const out = await obsctl(
          ["tags", "find", params.tag],
          ctx.credentials,
        );
        return parse(out);
      },
    },

    find_by_category: {
      description:
        "Find notes whose `category` frontmatter property matches the given value.",
      params: z.object({
        category: z.string().describe("Category value to match (e.g. 'project')"),
        limit: z
          .number()
          .min(1)
          .max(200)
          .default(50)
          .describe("Max results to return"),
      }),
      returns: z.array(
        z.object({
          path: z.string(),
          title: z.string(),
          category: z.string(),
          tags: z.array(z.string()),
        }),
      ),
      execute: async (params, ctx) => {
        const out = await obsctl(
          [
            "properties",
            "find-by",
            "category",
            params.category,
            "--limit",
            String(params.limit),
          ],
          ctx.credentials,
        );
        return parse(out);
      },
    },

    query_by_property: {
      description:
        "Query notes by any frontmatter property — a generalised bases-style filter.",
      params: z.object({
        property: z.string().describe("Frontmatter property name (e.g. 'status')"),
        value: z
          .string()
          .optional()
          .describe("Value to match (ignored when operator is 'exists')"),
        operator: z
          .enum(["eq", "contains", "exists"])
          .default("eq")
          .describe(
            "'eq' — exact match; 'contains' — substring / array membership; 'exists' — property is present",
          ),
        limit: z
          .number()
          .min(1)
          .max(200)
          .default(50)
          .describe("Max results to return"),
      }),
      returns: z.array(
        z.object({
          path: z.string(),
          title: z.string(),
          value: z.unknown(),
          tags: z.array(z.string()),
        }),
      ),
      execute: async (params, ctx) => {
        const args = [
          "properties",
          "query",
          "--property",
          params.property,
          "--operator",
          params.operator,
          "--limit",
          String(params.limit),
        ];
        if (params.operator !== "exists" && params.value !== undefined) {
          args.push("--value", params.value);
        }
        const out = await obsctl(args, ctx.credentials);
        return parse(out);
      },
    },

    // ── Properties ───────────────────────────────────────

    get_properties: {
      description: "Get frontmatter properties of a note.",
      params: z.object({
        path: z.string().describe("Note path"),
      }),
      returns: z.record(z.string(), z.unknown()),
      execute: async (params, ctx) => {
        const out = await obsctl(
          ["properties", "get", params.path],
          ctx.credentials,
        );
        return parse(out);
      },
    },

    set_property: {
      description: "Set a frontmatter property on a note.",
      params: z.object({
        path: z.string().describe("Note path"),
        key: z.string().describe("Property name"),
        value: z.string().describe("Property value"),
      }),
      returns: z.object({
        path: z.string(),
        key: z.string(),
        value: z.string(),
        set: z.boolean(),
      }),
      execute: async (params, ctx) => {
        await obsctl(
          ["properties", "set", params.path, params.key, params.value],
          ctx.credentials,
        );
        return {
          path: params.path,
          key: params.key,
          value: params.value,
          set: true,
        };
      },
    },

    // ── Templates ────────────────────────────────────────

    apply_template: {
      description: "Apply a template to a note.",
      params: z.object({
        path: z.string().describe("Note path"),
        template: z.string().describe("Template name"),
      }),
      returns: z.object({
        path: z.string(),
        template: z.string(),
        applied: z.boolean(),
      }),
      execute: async (params, ctx) => {
        await obsctl(
          ["templates", "apply", params.path, params.template],
          ctx.credentials,
        );
        return {
          path: params.path,
          template: params.template,
          applied: true,
        };
      },
    },

    list_templates: {
      description: "List available templates.",
      params: z.object({}),
      returns: z.array(z.string()),
      execute: async (_params, ctx) => {
        const out = await obsctl(["templates", "list"], ctx.credentials);
        return parse(out);
      },
    },
  },
});
