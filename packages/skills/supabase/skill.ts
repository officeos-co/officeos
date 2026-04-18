import { defineSkill, z } from "@harro/skill-sdk";

// ── Helpers ─────────────────────────────────────────────────────────

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

function baseUrl(ctx: Ctx) {
  return ctx.credentials.url.replace(/\/+$/, "");
}

function restHeaders(ctx: Ctx): Record<string, string> {
  return {
    apikey: ctx.credentials.service_role_key,
    Authorization: `Bearer ${ctx.credentials.service_role_key}`,
    "Content-Type": "application/json",
  };
}

async function sbFetch(ctx: Ctx, url: string, init?: RequestInit) {
  const res = await ctx.fetch(url, {
    ...init,
    headers: { ...restHeaders(ctx), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Supabase API ${res.status}: ${body}`);
  }
  const text = await res.text();
  return text ? JSON.parse(text) : {};
}

async function sbFetchRaw(ctx: Ctx, url: string, init?: RequestInit) {
  const res = await ctx.fetch(url, {
    ...init,
    headers: { ...restHeaders(ctx), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Supabase API ${res.status}: ${body}`);
  }
  return res;
}

function enc(s: string) {
  return encodeURIComponent(s);
}

// ── Skill definition ────────────────────────────────────────────────

import doc from "./SKILL.md";

export default defineSkill({
  name: "supabase",
  title: "Supabase",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M11.9 1.036c-.015-.986-1.26-1.41-1.874-.637L.764 12.05C-.33 13.427.65 15.455 2.409 15.455h9.579l.113 7.51c.014.985 1.259 1.408 1.873.636l9.262-11.653c1.093-1.375.113-3.403-1.645-3.403h-9.642z\"/></svg>",
  description:
    "Full Supabase platform coverage: database, PostgREST CRUD, auth, storage, edge functions, realtime, vector search, and RPC via the Supabase REST APIs.",
  doc,

  credentials: {
    url: {
      label: "Project URL",
      kind: "text",
      placeholder: "https://xyzcompany.supabase.co",
      help: "Your Supabase project URL. Find it in Settings > API.",
    },
    service_role_key: {
      label: "Service Role Key",
      kind: "password",
      placeholder: "eyJhbGciOi\u2026",
      help: "Service role key (bypasses RLS). Find it in Settings > API. Use anon key if you want RLS enforced.",
    },
  },

  actions: {
    // ── Database ──────────────────────────────────────────────────────

    query: {
      description: "Execute a raw SQL query via PostgREST RPC.",
      params: z.object({
        sql: z.string().describe("SQL query to execute"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        // Uses the pg_rest sql endpoint through RPC
        return sbFetch(ctx, `${baseUrl(ctx)}/rest/v1/rpc/exec_sql`, {
          method: "POST",
          body: JSON.stringify({ query: params.sql }),
        });
      },
    },

    list_tables: {
      description: "List all tables in the public schema.",
      params: z.object({}),
      returns: z.array(
        z.object({
          table_name: z.string(),
          row_count: z.number(),
          size: z.string(),
        }),
      ),
      execute: async (_params, ctx) => {
        const sql = `
          SELECT
            schemaname || '.' || relname AS table_name,
            n_live_tup AS row_count,
            pg_size_pretty(pg_total_relation_size(relid)) AS size
          FROM pg_stat_user_tables
          WHERE schemaname = 'public'
          ORDER BY relname
        `;
        return sbFetch(ctx, `${baseUrl(ctx)}/rest/v1/rpc/exec_sql`, {
          method: "POST",
          body: JSON.stringify({ query: sql }),
        });
      },
    },

    get_table: {
      description: "Get column details for a table.",
      params: z.object({
        table: z.string().describe("Table name"),
      }),
      returns: z.array(
        z.object({
          column_name: z.string(),
          data_type: z.string(),
          is_nullable: z.string(),
          column_default: z.string().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const sql = `
          SELECT column_name, data_type, is_nullable, column_default
          FROM information_schema.columns
          WHERE table_schema = 'public' AND table_name = '${params.table}'
          ORDER BY ordinal_position
        `;
        return sbFetch(ctx, `${baseUrl(ctx)}/rest/v1/rpc/exec_sql`, {
          method: "POST",
          body: JSON.stringify({ query: sql }),
        });
      },
    },

    // ── CRUD (PostgREST) ──────────────────────────────────────────────

    select: {
      description: "Select rows from a table via PostgREST.",
      params: z.object({
        table: z.string().describe("Table name"),
        columns: z.string().default("*").describe("Comma-separated column names"),
        filter: z
          .string()
          .optional()
          .describe("PostgREST filter (e.g. active=eq.true)"),
        order: z
          .string()
          .optional()
          .describe("Column and direction (e.g. created_at.desc)"),
        limit: z.number().min(1).max(1000).default(100).describe("Max rows"),
        offset: z.number().min(0).default(0).describe("Rows to skip"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const qs = new URLSearchParams();
        qs.set("select", params.columns);
        if (params.filter) {
          // PostgREST expects filter as query params: column=operator.value
          const parts = params.filter.split("&");
          for (const p of parts) {
            const eqIdx = p.indexOf("=");
            if (eqIdx > 0) {
              qs.set(p.slice(0, eqIdx), p.slice(eqIdx + 1));
            }
          }
        }
        if (params.order) qs.set("order", params.order);
        qs.set("limit", String(params.limit));
        qs.set("offset", String(params.offset));
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/rest/v1/${enc(params.table)}?${qs.toString()}`,
        );
      },
    },

    insert: {
      description: "Insert rows into a table.",
      params: z.object({
        table: z.string().describe("Table name"),
        records: z
          .string()
          .describe("JSON array of objects to insert"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/rest/v1/${enc(params.table)}`,
          {
            method: "POST",
            headers: { Prefer: "return=representation" },
            body: params.records,
          },
        );
      },
    },

    update: {
      description: "Update rows matching a filter.",
      params: z.object({
        table: z.string().describe("Table name"),
        filter: z.string().describe("PostgREST filter to match rows"),
        data: z.string().describe("JSON object with columns to update"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const qs = new URLSearchParams();
        const parts = params.filter.split("&");
        for (const p of parts) {
          const eqIdx = p.indexOf("=");
          if (eqIdx > 0) qs.set(p.slice(0, eqIdx), p.slice(eqIdx + 1));
        }
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/rest/v1/${enc(params.table)}?${qs.toString()}`,
          {
            method: "PATCH",
            headers: { Prefer: "return=representation" },
            body: params.data,
          },
        );
      },
    },

    delete: {
      description: "Delete rows matching a filter.",
      params: z.object({
        table: z.string().describe("Table name"),
        filter: z.string().describe("PostgREST filter to match rows"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const qs = new URLSearchParams();
        const parts = params.filter.split("&");
        for (const p of parts) {
          const eqIdx = p.indexOf("=");
          if (eqIdx > 0) qs.set(p.slice(0, eqIdx), p.slice(eqIdx + 1));
        }
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/rest/v1/${enc(params.table)}?${qs.toString()}`,
          {
            method: "DELETE",
            headers: { Prefer: "return=representation" },
          },
        );
      },
    },

    upsert: {
      description: "Upsert rows into a table.",
      params: z.object({
        table: z.string().describe("Table name"),
        records: z.string().describe("JSON array of objects to upsert"),
        on_conflict: z
          .string()
          .optional()
          .describe("Comma-separated conflict column names"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const headers: Record<string, string> = {
          Prefer: "return=representation,resolution=merge-duplicates",
        };
        const qs = new URLSearchParams();
        if (params.on_conflict) qs.set("on_conflict", params.on_conflict);
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/rest/v1/${enc(params.table)}?${qs.toString()}`,
          {
            method: "POST",
            headers,
            body: params.records,
          },
        );
      },
    },

    // ── Auth ──────────────────────────────────────────────────────────

    list_users: {
      description: "List users (admin API).",
      params: z.object({
        page: z.number().min(1).default(1).describe("Page number"),
        per_page: z.number().min(1).max(1000).default(50).describe("Results per page"),
      }),
      returns: z.object({
        users: z.array(z.record(z.unknown())),
      }),
      execute: async (params, ctx) => {
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/auth/v1/admin/users?page=${params.page}&per_page=${params.per_page}`,
        );
      },
    },

    get_user: {
      description: "Get a user by UUID.",
      params: z.object({
        user_id: z.string().describe("User UUID"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) => {
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/auth/v1/admin/users/${enc(params.user_id)}`,
        );
      },
    },

    create_user: {
      description: "Create a new user (admin API).",
      params: z.object({
        email: z.string().describe("User email"),
        password: z.string().describe("User password"),
        email_confirm: z.boolean().default(false).describe("Auto-confirm email"),
        user_metadata: z
          .string()
          .optional()
          .describe("Custom user metadata as JSON object"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = {
          email: params.email,
          password: params.password,
          email_confirm: params.email_confirm,
        };
        if (params.user_metadata) {
          body.user_metadata = JSON.parse(params.user_metadata);
        }
        return sbFetch(ctx, `${baseUrl(ctx)}/auth/v1/admin/users`, {
          method: "POST",
          body: JSON.stringify(body),
        });
      },
    },

    delete_user: {
      description: "Delete a user by UUID.",
      params: z.object({
        user_id: z.string().describe("User UUID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await sbFetch(
          ctx,
          `${baseUrl(ctx)}/auth/v1/admin/users/${enc(params.user_id)}`,
          { method: "DELETE" },
        );
        return { success: true };
      },
    },

    invite_user: {
      description: "Invite a user by email.",
      params: z.object({
        email: z.string().describe("Email to invite"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) => {
        return sbFetch(ctx, `${baseUrl(ctx)}/auth/v1/invite`, {
          method: "POST",
          body: JSON.stringify({ email: params.email }),
        });
      },
    },

    update_user: {
      description: "Update user attributes.",
      params: z.object({
        user_id: z.string().describe("User UUID"),
        email: z.string().optional().describe("New email"),
        password: z.string().optional().describe("New password"),
        user_metadata: z
          .string()
          .optional()
          .describe("Updated user metadata as JSON object"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = {};
        if (params.email) body.email = params.email;
        if (params.password) body.password = params.password;
        if (params.user_metadata) {
          body.user_metadata = JSON.parse(params.user_metadata);
        }
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/auth/v1/admin/users/${enc(params.user_id)}`,
          { method: "PUT", body: JSON.stringify(body) },
        );
      },
    },

    // ── Storage ───────────────────────────────────────────────────────

    list_buckets: {
      description: "List all storage buckets.",
      params: z.object({}),
      returns: z.array(
        z.object({
          id: z.string(),
          name: z.string(),
          public: z.boolean(),
          created_at: z.string(),
        }),
      ),
      execute: async (_params, ctx) => {
        return sbFetch(ctx, `${baseUrl(ctx)}/storage/v1/bucket`);
      },
    },

    create_bucket: {
      description: "Create a storage bucket.",
      params: z.object({
        name: z.string().describe("Bucket name"),
        public: z.boolean().default(false).describe("Whether bucket is public"),
      }),
      returns: z.object({ name: z.string() }),
      execute: async (params, ctx) => {
        return sbFetch(ctx, `${baseUrl(ctx)}/storage/v1/bucket`, {
          method: "POST",
          body: JSON.stringify({ id: params.name, name: params.name, public: params.public }),
        });
      },
    },

    delete_bucket: {
      description: "Delete a storage bucket.",
      params: z.object({
        id: z.string().describe("Bucket ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        // Empty the bucket first, then delete
        await sbFetch(
          ctx,
          `${baseUrl(ctx)}/storage/v1/bucket/${enc(params.id)}/empty`,
          { method: "POST", body: JSON.stringify({}) },
        );
        await sbFetch(
          ctx,
          `${baseUrl(ctx)}/storage/v1/bucket/${enc(params.id)}`,
          { method: "DELETE" },
        );
        return { success: true };
      },
    },

    list_files: {
      description: "List files in a storage bucket.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        path: z.string().default("").describe("Folder path within bucket"),
        limit: z.number().min(1).max(1000).default(100).describe("Max files"),
        offset: z.number().min(0).default(0).describe("Files to skip"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/storage/v1/object/list/${enc(params.bucket)}`,
          {
            method: "POST",
            body: JSON.stringify({
              prefix: params.path,
              limit: params.limit,
              offset: params.offset,
            }),
          },
        );
      },
    },

    upload_file: {
      description: "Upload a file to storage (base64-encoded content).",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        path: z.string().describe("File path within bucket"),
        content: z.string().describe("Base64-encoded file content"),
        content_type: z
          .string()
          .optional()
          .describe("MIME type (e.g. image/png)"),
        upsert: z.boolean().default(false).describe("Overwrite if file exists"),
      }),
      returns: z.object({ Key: z.string() }),
      execute: async (params, ctx) => {
        const buffer = Uint8Array.from(atob(params.content), (c) => c.charCodeAt(0));
        const headers: Record<string, string> = {
          ...restHeaders(ctx),
          "Content-Type": params.content_type ?? "application/octet-stream",
        };
        if (params.upsert) headers["x-upsert"] = "true";
        // Remove the JSON content-type from restHeaders for binary upload
        delete headers["Content-Type"];
        const ct = params.content_type ?? "application/octet-stream";
        const res = await ctx.fetch(
          `${baseUrl(ctx)}/storage/v1/object/${enc(params.bucket)}/${params.path}`,
          {
            method: "POST",
            headers: {
              apikey: ctx.credentials.service_role_key,
              Authorization: `Bearer ${ctx.credentials.service_role_key}`,
              "Content-Type": ct,
              ...(params.upsert ? { "x-upsert": "true" } : {}),
            },
            body: buffer,
          },
        );
        if (!res.ok) {
          const body = await res.text();
          throw new Error(`Supabase Storage ${res.status}: ${body}`);
        }
        return res.json();
      },
    },

    download_file: {
      description: "Download a file from storage.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        path: z.string().describe("File path within bucket"),
      }),
      returns: z.object({
        content: z.string(),
        content_type: z.string(),
      }),
      execute: async (params, ctx) => {
        const res = await sbFetchRaw(
          ctx,
          `${baseUrl(ctx)}/storage/v1/object/${enc(params.bucket)}/${params.path}`,
        );
        const ct = res.headers.get("content-type") ?? "application/octet-stream";
        const buf = await res.arrayBuffer();
        const bytes = new Uint8Array(buf);
        let binary = "";
        for (let i = 0; i < bytes.length; i++) {
          binary += String.fromCharCode(bytes[i]);
        }
        return { content: btoa(binary), content_type: ct };
      },
    },

    delete_file: {
      description: "Delete files from a storage bucket.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        paths: z.array(z.string()).describe("Array of file paths to delete"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/storage/v1/object/${enc(params.bucket)}`,
          {
            method: "DELETE",
            body: JSON.stringify({ prefixes: params.paths }),
          },
        );
      },
    },

    get_public_url: {
      description: "Get the public URL for a file (public buckets only).",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        path: z.string().describe("File path within bucket"),
      }),
      returns: z.object({ public_url: z.string() }),
      execute: async (params, ctx) => {
        return {
          public_url: `${baseUrl(ctx)}/storage/v1/object/public/${enc(params.bucket)}/${params.path}`,
        };
      },
    },

    create_signed_url: {
      description: "Create a signed URL for temporary file access.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        path: z.string().describe("File path within bucket"),
        expires_in: z
          .number()
          .min(1)
          .default(3600)
          .describe("Seconds until URL expires"),
      }),
      returns: z.object({ signedURL: z.string() }),
      execute: async (params, ctx) => {
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/storage/v1/object/sign/${enc(params.bucket)}/${params.path}`,
          {
            method: "POST",
            body: JSON.stringify({ expiresIn: params.expires_in }),
          },
        );
      },
    },

    // ── Edge Functions ────────────────────────────────────────────────

    list_functions: {
      description: "List deployed edge functions.",
      params: z.object({}),
      returns: z.array(z.record(z.unknown())),
      execute: async (_params, ctx) => {
        // Edge function management API is project-level; list via management API
        // For self-hosted or via the REST endpoint
        return sbFetch(ctx, `${baseUrl(ctx)}/functions/v1/`);
      },
    },

    get_function: {
      description: "Get details of an edge function.",
      params: z.object({
        slug: z.string().describe("Function slug"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) => {
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/functions/v1/${enc(params.slug)}`,
        );
      },
    },

    invoke_function: {
      description: "Invoke an edge function.",
      params: z.object({
        slug: z.string().describe("Function slug"),
        body: z.string().optional().describe("Request body as JSON"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) => {
        const init: RequestInit = { method: "POST" };
        if (params.body) init.body = params.body;
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/functions/v1/${enc(params.slug)}`,
          init,
        );
      },
    },

    // ── Realtime ──────────────────────────────────────────────────────

    list_channels: {
      description: "List active realtime channels.",
      params: z.object({}),
      returns: z.array(z.record(z.unknown())),
      execute: async (_params, ctx) => {
        return sbFetch(ctx, `${baseUrl(ctx)}/realtime/v1/channels`);
      },
    },

    // ── Vector / Embeddings ───────────────────────────────────────────

    vector_search: {
      description:
        "Perform vector similarity search via a match_<table> RPC function (pgvector).",
      params: z.object({
        table: z.string().describe("Table with vector column"),
        query_embedding: z.string().describe("Query vector as JSON array of floats"),
        match_count: z.number().min(1).default(10).describe("Max results"),
        match_threshold: z
          .number()
          .min(0)
          .max(1)
          .default(0.5)
          .describe("Minimum similarity threshold"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/rest/v1/rpc/match_${enc(params.table)}`,
          {
            method: "POST",
            body: JSON.stringify({
              query_embedding: JSON.parse(params.query_embedding),
              match_count: params.match_count,
              match_threshold: params.match_threshold,
            }),
          },
        );
      },
    },

    // ── RPC ───────────────────────────────────────────────────────────

    rpc: {
      description: "Invoke an arbitrary Postgres function via PostgREST RPC.",
      params: z.object({
        function_name: z.string().describe("Postgres function name"),
        params: z
          .string()
          .default("{}")
          .describe("Arguments as JSON object"),
      }),
      returns: z.unknown(),
      execute: async (params, ctx) => {
        return sbFetch(
          ctx,
          `${baseUrl(ctx)}/rest/v1/rpc/${enc(params.function_name)}`,
          {
            method: "POST",
            body: params.params,
          },
        );
      },
    },
  },
});
