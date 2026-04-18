import { defineSkill, z } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

// ── Firestore value conversion ────────────────────────────────────────────────

function toFirestoreValue(value: unknown): Record<string, unknown> {
  if (value === null || value === undefined) return { nullValue: null };
  if (typeof value === "boolean") return { booleanValue: value };
  if (typeof value === "string") return { stringValue: value };
  if (typeof value === "number") {
    return Number.isInteger(value) ? { integerValue: String(value) } : { doubleValue: value };
  }
  if (Array.isArray(value)) {
    return { arrayValue: { values: value.map(toFirestoreValue) } };
  }
  if (typeof value === "object") {
    const fields: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(value as Record<string, unknown>)) {
      fields[k] = toFirestoreValue(v);
    }
    return { mapValue: { fields } };
  }
  return { stringValue: String(value) };
}

function toFirestoreFields(obj: Record<string, unknown>): Record<string, unknown> {
  const fields: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(obj)) {
    fields[k] = toFirestoreValue(v);
  }
  return fields;
}

function fromFirestoreValue(val: Record<string, unknown>): unknown {
  if ("stringValue" in val) return val.stringValue;
  if ("integerValue" in val) return parseInt(val.integerValue as string, 10);
  if ("doubleValue" in val) return val.doubleValue;
  if ("booleanValue" in val) return val.booleanValue;
  if ("nullValue" in val) return null;
  if ("arrayValue" in val) {
    const arr = val.arrayValue as { values?: Record<string, unknown>[] };
    return (arr.values ?? []).map(fromFirestoreValue);
  }
  if ("mapValue" in val) {
    const map = val.mapValue as { fields?: Record<string, Record<string, unknown>> };
    const out: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(map.fields ?? {})) out[k] = fromFirestoreValue(v);
    return out;
  }
  return null;
}

function fromFirestoreFields(fields: Record<string, Record<string, unknown>>): Record<string, unknown> {
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(fields ?? {})) out[k] = fromFirestoreValue(v);
  return out;
}

function parseDocName(name: string): { id: string; path: string } {
  const parts = name.split("/documents/");
  const docPath = parts[1] ?? name;
  const segments = docPath.split("/");
  return { id: segments[segments.length - 1] ?? "", path: docPath };
}

// ── Firestore & Auth fetch helpers ────────────────────────────────────────────

const FIRESTORE_BASE = "https://firestore.googleapis.com/v1";
const IDENTITY_BASE = "https://identitytoolkit.googleapis.com/v1";
const FUNCTIONS_BASE = "https://cloudfunctions.googleapis.com/v2";

interface FbCtx {
  fetch: typeof globalThis.fetch;
  credentials: Record<string, string>;
}

async function firestoreFetch(
  ctx: FbCtx,
  method: string,
  path: string,
  body?: unknown,
  query?: Record<string, string>,
): Promise<unknown> {
  const url = new URL(`${FIRESTORE_BASE}${path}`);
  url.searchParams.set("key", ctx.credentials.api_key);
  if (query) Object.entries(query).forEach(([k, v]) => url.searchParams.set(k, v));
  const res = await ctx.fetch(url.toString(), {
    method,
    headers: body ? { "Content-Type": "application/json" } : {},
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(`Firestore ${res.status}: ${(err as any)?.error?.message ?? res.statusText}`);
  }
  if (res.status === 204) return {};
  return res.json();
}

async function authFetch(
  ctx: FbCtx,
  endpoint: string,
  body: unknown,
): Promise<unknown> {
  const url = `${IDENTITY_BASE}/accounts${endpoint}?key=${ctx.credentials.api_key}`;
  const res = await ctx.fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(`Firebase Auth ${res.status}: ${(err as any)?.error?.message ?? res.statusText}`);
  }
  return res.json();
}

function firestorePath(project: string, docPath?: string): string {
  const base = `/projects/${project}/databases/(default)/documents`;
  return docPath ? `${base}/${docPath}` : base;
}

// ── Skill definition ──────────────────────────────────────────────────────────

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Firestore Collections ─────────────────────────────────────────

    collections_list: {
      description: "List top-level Firestore collections (or sub-collections under a parent document).",
      params: z.object({
        parent: z.string().optional().describe("Parent document path for sub-collections (omit for root)"),
      }),
      returns: z.array(z.object({ collectionId: z.string() })),
      execute: async (params, ctx) => {
        const parentPath = params.parent
          ? firestorePath(ctx.credentials.project_id, params.parent)
          : firestorePath(ctx.credentials.project_id);
        const data: any = await firestoreFetch(ctx, "POST", `${parentPath}:listCollectionIds`, {});
        return (data.collectionIds ?? []).map((id: string) => ({ collectionId: id }));
      },
    },

    // ── Firestore Documents ───────────────────────────────────────────

    documents_list: {
      description: "List documents in a Firestore collection.",
      params: z.object({
        collection: z.string().describe("Collection path (e.g. 'users' or 'users/uid/posts')"),
        page_size: z.number().int().min(1).max(300).default(20).describe("Documents per page"),
        page_token: z.string().optional().describe("Token from a previous response for pagination"),
      }),
      returns: z.object({
        documents: z.array(
          z.object({ id: z.string(), path: z.string(), fields: z.record(z.unknown()), createTime: z.string(), updateTime: z.string() }),
        ),
        next_page_token: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const path = firestorePath(ctx.credentials.project_id, params.collection);
        const query: Record<string, string> = { pageSize: String(params.page_size) };
        if (params.page_token) query.pageToken = params.page_token;
        const data: any = await firestoreFetch(ctx, "GET", path, undefined, query);
        const docs = (data.documents ?? []).map((doc: any) => {
          const { id, path: docPath } = parseDocName(doc.name);
          return {
            id,
            path: docPath,
            fields: fromFirestoreFields(doc.fields ?? {}),
            createTime: doc.createTime ?? "",
            updateTime: doc.updateTime ?? "",
          };
        });
        return { documents: docs, next_page_token: data.nextPageToken ?? null };
      },
    },

    documents_get: {
      description: "Get a single Firestore document by path.",
      params: z.object({
        path: z.string().describe("Document path (e.g. 'users/user123')"),
      }),
      returns: z.object({
        id: z.string(),
        path: z.string(),
        fields: z.record(z.unknown()),
        createTime: z.string(),
        updateTime: z.string(),
      }),
      execute: async (params, ctx) => {
        const docPath = firestorePath(ctx.credentials.project_id, params.path);
        const doc: any = await firestoreFetch(ctx, "GET", docPath);
        const { id, path } = parseDocName(doc.name);
        return {
          id,
          path,
          fields: fromFirestoreFields(doc.fields ?? {}),
          createTime: doc.createTime ?? "",
          updateTime: doc.updateTime ?? "",
        };
      },
    },

    documents_create: {
      description: "Create a new Firestore document with an auto-generated ID.",
      params: z.object({
        collection: z.string().describe("Collection path (e.g. 'users')"),
        fields: z.record(z.unknown()).describe("Document fields as plain JS values"),
      }),
      returns: z.object({ id: z.string(), path: z.string(), createTime: z.string() }),
      execute: async (params, ctx) => {
        const colPath = firestorePath(ctx.credentials.project_id, params.collection);
        const doc: any = await firestoreFetch(ctx, "POST", colPath, {
          fields: toFirestoreFields(params.fields),
        });
        const { id, path } = parseDocName(doc.name);
        return { id, path, createTime: doc.createTime ?? "" };
      },
    },

    documents_set: {
      description: "Create or overwrite a Firestore document at a specific path.",
      params: z.object({
        path: z.string().describe("Full document path (e.g. 'users/user123')"),
        fields: z.record(z.unknown()).describe("Document fields as plain JS values"),
      }),
      returns: z.object({ id: z.string(), path: z.string(), updateTime: z.string() }),
      execute: async (params, ctx) => {
        const docPath = firestorePath(ctx.credentials.project_id, params.path);
        const doc: any = await firestoreFetch(ctx, "PATCH", docPath, {
          fields: toFirestoreFields(params.fields),
        });
        const { id, path } = parseDocName(doc.name);
        return { id, path, updateTime: doc.updateTime ?? "" };
      },
    },

    documents_update: {
      description: "Update specific fields of a Firestore document using a field mask.",
      params: z.object({
        path: z.string().describe("Document path (e.g. 'users/user123')"),
        fields: z.record(z.unknown()).describe("Fields to update as plain JS values"),
        update_mask: z.array(z.string()).optional().describe("Field names to update; omit to update all provided fields"),
      }),
      returns: z.object({ id: z.string(), path: z.string(), updateTime: z.string() }),
      execute: async (params, ctx) => {
        const docPath = firestorePath(ctx.credentials.project_id, params.path);
        const maskFields = params.update_mask ?? Object.keys(params.fields);
        const query: Record<string, string> = {};
        maskFields.forEach((f) => { query[`updateMask.fieldPaths`] = f; });
        const doc: any = await firestoreFetch(
          ctx,
          "PATCH",
          docPath,
          { fields: toFirestoreFields(params.fields) },
          maskFields.length > 0
            ? Object.fromEntries(maskFields.map((f, i) => [`updateMask.fieldPaths`, f]))
            : {},
        );
        const { id, path } = parseDocName(doc.name);
        return { id, path, updateTime: doc.updateTime ?? "" };
      },
    },

    documents_delete: {
      description: "Delete a Firestore document.",
      params: z.object({
        path: z.string().describe("Document path (e.g. 'users/user123')"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        const docPath = firestorePath(ctx.credentials.project_id, params.path);
        await firestoreFetch(ctx, "DELETE", docPath);
        return { success: true };
      },
    },

    documents_query: {
      description: "Run a structured query against a Firestore collection.",
      params: z.object({
        collection: z.string().describe("Collection ID to query (top-level only)"),
        where: z
          .array(
            z.object({
              field: z.string().describe("Field name"),
              op: z
                .enum(["==", "<", "<=", ">", ">=", "!=", "array-contains"])
                .describe("Comparison operator"),
              value: z.unknown().describe("Value to compare against"),
            }),
          )
          .optional()
          .describe("Filter conditions (ANDed together)"),
        order_by: z
          .object({
            field: z.string().describe("Field to sort by"),
            direction: z.enum(["ASCENDING", "DESCENDING"]).default("ASCENDING"),
          })
          .optional()
          .describe("Sort order"),
        limit: z.number().int().min(1).max(300).default(20).describe("Maximum documents to return"),
      }),
      returns: z.array(z.object({ id: z.string(), path: z.string(), fields: z.record(z.unknown()) })),
      execute: async (params, ctx) => {
        const opMap: Record<string, string> = {
          "==": "EQUAL",
          "<": "LESS_THAN",
          "<=": "LESS_THAN_OR_EQUAL",
          ">": "GREATER_THAN",
          ">=": "GREATER_THAN_OR_EQUAL",
          "!=": "NOT_EQUAL",
          "array-contains": "ARRAY_CONTAINS",
        };
        const filters = (params.where ?? []).map((w) => ({
          fieldFilter: {
            field: { fieldPath: w.field },
            op: opMap[w.op] ?? "EQUAL",
            value: toFirestoreValue(w.value),
          },
        }));

        const structuredQuery: Record<string, unknown> = {
          from: [{ collectionId: params.collection }],
          limit: params.limit,
        };
        if (filters.length === 1) {
          structuredQuery.where = filters[0];
        } else if (filters.length > 1) {
          structuredQuery.where = { compositeFilter: { op: "AND", filters } };
        }
        if (params.order_by) {
          structuredQuery.orderBy = [{ field: { fieldPath: params.order_by.field }, direction: params.order_by.direction }];
        }

        const parentPath = firestorePath(ctx.credentials.project_id);
        const data: any = await firestoreFetch(ctx, "POST", `${parentPath}:runQuery`, { structuredQuery });
        return (data as any[])
          .filter((r: any) => r.document)
          .map((r: any) => {
            const { id, path } = parseDocName(r.document.name);
            return { id, path, fields: fromFirestoreFields(r.document.fields ?? {}) };
          });
      },
    },

    // ── Firebase Auth ─────────────────────────────────────────────────

    auth_users_list: {
      description: "List Firebase Auth users.",
      params: z.object({
        max_results: z.number().int().min(1).max(1000).default(100).describe("Maximum users to return"),
        page_token: z.string().optional().describe("Token for pagination"),
      }),
      returns: z.object({
        users: z.array(
          z.object({
            uid: z.string(),
            email: z.string().optional(),
            displayName: z.string().optional(),
            disabled: z.boolean(),
            createdAt: z.string(),
          }),
        ),
        next_page_token: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = { maxResults: params.max_results };
        if (params.page_token) body.nextPageToken = params.page_token;
        const data: any = await authFetch(ctx, ":query", body);
        const users = (data.users ?? []).map((u: any) => ({
          uid: u.localId,
          email: u.email,
          displayName: u.displayName,
          disabled: u.disabled ?? false,
          createdAt: u.createdAt ? new Date(parseInt(u.createdAt, 10)).toISOString() : "",
        }));
        return { users, next_page_token: data.nextPageToken ?? null };
      },
    },

    auth_users_get: {
      description: "Get a Firebase Auth user by UID or email.",
      params: z.object({
        uid: z.string().optional().describe("User UID"),
        email: z.string().optional().describe("User email address"),
      }),
      returns: z.object({
        uid: z.string(),
        email: z.string().optional(),
        displayName: z.string().optional(),
        photoUrl: z.string().optional(),
        disabled: z.boolean(),
        createdAt: z.string(),
        lastSignIn: z.string().optional(),
      }),
      execute: async (params, ctx) => {
        if (!params.uid && !params.email) throw new Error("auth_users_get: provide uid or email");
        const body = params.uid ? { localId: [params.uid] } : { email: [params.email] };
        const data: any = await authFetch(ctx, ":lookup", body);
        const u = data.users?.[0];
        if (!u) throw new Error("Firebase Auth: user not found");
        return {
          uid: u.localId,
          email: u.email,
          displayName: u.displayName,
          photoUrl: u.photoUrl,
          disabled: u.disabled ?? false,
          createdAt: u.createdAt ? new Date(parseInt(u.createdAt, 10)).toISOString() : "",
          lastSignIn: u.lastLoginAt ? new Date(parseInt(u.lastLoginAt, 10)).toISOString() : undefined,
        };
      },
    },

    auth_users_create: {
      description: "Create a new Firebase Auth user.",
      params: z.object({
        email: z.string().email().describe("User email address"),
        password: z.string().min(6).describe("Password (min 6 characters)"),
        display_name: z.string().optional().describe("Display name"),
        disabled: z.boolean().default(false).describe("Whether the user account is disabled"),
      }),
      returns: z.object({ uid: z.string(), email: z.string(), displayName: z.string().optional() }),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = {
          email: params.email,
          password: params.password,
          disabled: params.disabled,
        };
        if (params.display_name) body.displayName = params.display_name;
        const data: any = await authFetch(ctx, "", body);
        return {
          uid: data.localId,
          email: data.email,
          displayName: data.displayName,
        };
      },
    },

    auth_users_update: {
      description: "Update a Firebase Auth user's profile or credentials.",
      params: z.object({
        uid: z.string().describe("User UID"),
        email: z.string().email().optional().describe("New email address"),
        display_name: z.string().optional().describe("New display name"),
        disabled: z.boolean().optional().describe("Enable or disable the account"),
        password: z.string().min(6).optional().describe("New password"),
      }),
      returns: z.object({ uid: z.string(), email: z.string().optional(), displayName: z.string().optional(), disabled: z.boolean() }),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = { localId: params.uid };
        if (params.email !== undefined) body.email = params.email;
        if (params.display_name !== undefined) body.displayName = params.display_name;
        if (params.disabled !== undefined) body.disableUser = params.disabled;
        if (params.password !== undefined) body.password = params.password;
        const data: any = await authFetch(ctx, ":update", body);
        return {
          uid: data.localId,
          email: data.email,
          displayName: data.displayName,
          disabled: data.disabled ?? false,
        };
      },
    },

    auth_users_delete: {
      description: "Delete a Firebase Auth user by UID.",
      params: z.object({
        uid: z.string().describe("User UID to delete"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await authFetch(ctx, ":delete", { localId: params.uid });
        return { success: true };
      },
    },

    // ── Cloud Functions ───────────────────────────────────────────────

    functions_list: {
      description: "List Cloud Functions deployed in the Firebase project.",
      params: z.object({
        region: z.string().default("us-central1").describe("GCP region (e.g. us-central1, europe-west1)"),
      }),
      returns: z.array(
        z.object({
          name: z.string(),
          state: z.string(),
          runtime: z.string().optional(),
          updateTime: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const url = `${FUNCTIONS_BASE}/projects/${ctx.credentials.project_id}/locations/${params.region}/functions?key=${ctx.credentials.api_key}`;
        const res = await ctx.fetch(url);
        if (!res.ok) {
          const err = await res.json().catch(() => ({}));
          throw new Error(`Cloud Functions ${res.status}: ${(err as any)?.error?.message ?? res.statusText}`);
        }
        const data: any = await res.json();
        return (data.functions ?? []).map((f: any) => ({
          name: f.name.split("/").pop() ?? f.name,
          state: f.state ?? "",
          runtime: f.buildConfig?.runtime,
          updateTime: f.updateTime ?? "",
        }));
      },
    },

    functions_describe: {
      description: "Get details of a specific Cloud Function.",
      params: z.object({
        function_name: z.string().describe("Function name (short name or full resource name)"),
        region: z.string().default("us-central1").describe("GCP region"),
      }),
      returns: z.object({
        name: z.string(),
        state: z.string(),
        runtime: z.string().optional(),
        entry_point: z.string().optional(),
        update_time: z.string(),
      }),
      execute: async (params, ctx) => {
        const fullName = params.function_name.startsWith("projects/")
          ? params.function_name
          : `projects/${ctx.credentials.project_id}/locations/${params.region}/functions/${params.function_name}`;
        const url = `${FUNCTIONS_BASE}/${fullName}?key=${ctx.credentials.api_key}`;
        const res = await ctx.fetch(url);
        if (!res.ok) {
          const err = await res.json().catch(() => ({}));
          throw new Error(`Cloud Functions ${res.status}: ${(err as any)?.error?.message ?? res.statusText}`);
        }
        const f: any = await res.json();
        return {
          name: f.name.split("/").pop() ?? f.name,
          state: f.state ?? "",
          runtime: f.buildConfig?.runtime,
          entry_point: f.buildConfig?.entryPoint,
          update_time: f.updateTime ?? "",
        };
      },
    },
  },
});
