/**
 * CouchDB HTTP client for Obsidian vault operations.
 *
 * Ported from obsctl's core/client.py. Uses the same document structure
 * as Obsidian LiveSync: metadata docs with `children` arrays pointing to
 * content-addressed chunk documents (prefix `h:`).
 */

import type {
  VaultCredentials,
  CouchNoteDoc,
  Note,
  NoteInfo,
} from "./types.ts";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Strip dangerous control characters while preserving normal whitespace and Unicode. */
export function sanitizeUnicode(text: string): string {
  return text.replace(/[\x00-\x08\x0e-\x1f]/g, "");
}

/** Convert a vault path to a CouchDB document ID. */
export function pathToId(path: string): string {
  let docId = path.toLowerCase();
  if (docId.startsWith("_")) {
    docId = "/" + docId;
  }
  return docId;
}

/** Split content into chunks of `chunkSize` characters. */
export function createChunks(content: string, chunkSize = 50_000): string[] {
  if (!content) return [""];
  const chunks: string[] = [];
  for (let i = 0; i < content.length; i += chunkSize) {
    chunks.push(content.slice(i, i + chunkSize));
  }
  return chunks.length > 0 ? chunks : [""];
}

/** Generate a content-addressed chunk ID: `h:` + first 12 chars of SHA-256. */
export async function createChunkId(data: string): Promise<string> {
  const encoded = new TextEncoder().encode(data);
  const hashBuffer = await crypto.subtle.digest("SHA-256", encoded);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  const hex = hashArray.map((b) => b.toString(16).padStart(2, "0")).join("");
  return `h:${hex.slice(0, 12)}`;
}

// ---------------------------------------------------------------------------
// CouchDB request helpers
// ---------------------------------------------------------------------------

function buildBaseUrl(creds: VaultCredentials): string {
  return `${creds.couchdb_url}/${creds.vault_name}`;
}

function buildHeaders(creds: VaultCredentials): Record<string, string> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };
  if (creds.couchdb_user) {
    const encoded = btoa(`${creds.couchdb_user}:${creds.couchdb_password}`);
    headers["Authorization"] = `Basic ${encoded}`;
  }
  return headers;
}

async function couchGet(
  fetch: typeof globalThis.fetch,
  creds: VaultCredentials,
  path: string,
): Promise<Response> {
  const url = `${buildBaseUrl(creds)}/${encodeURIComponent(path)}`;
  return fetch(url, { headers: buildHeaders(creds) });
}

async function couchGetJson<T>(
  fetch: typeof globalThis.fetch,
  creds: VaultCredentials,
  path: string,
): Promise<T | null> {
  const resp = await couchGet(fetch, creds, path);
  if (resp.status === 404) return null;
  if (!resp.ok) {
    const text = await resp.text().catch(() => "");
    throw new Error(`CouchDB ${resp.status}: ${text || resp.statusText}`);
  }
  return resp.json() as Promise<T>;
}

async function couchPut(
  fetch: typeof globalThis.fetch,
  creds: VaultCredentials,
  path: string,
  body: unknown,
): Promise<{ ok: boolean; id: string; rev: string }> {
  const url = `${buildBaseUrl(creds)}/${encodeURIComponent(path)}`;
  const resp = await fetch(url, {
    method: "PUT",
    headers: buildHeaders(creds),
    body: JSON.stringify(body),
  });
  if (!resp.ok) {
    const text = await resp.text().catch(() => "");
    throw new Error(`CouchDB PUT ${resp.status}: ${text || resp.statusText}`);
  }
  return resp.json() as Promise<{ ok: boolean; id: string; rev: string }>;
}

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

/** List all notes, filtering out chunks, system docs, and deleted docs. */
export async function listNotes(
  fetch: typeof globalThis.fetch,
  creds: VaultCredentials,
): Promise<NoteInfo[]> {
  const url = `${buildBaseUrl(creds)}/_all_docs?include_docs=true`;
  const resp = await fetch(url, { headers: buildHeaders(creds) });
  if (!resp.ok) {
    const text = await resp.text().catch(() => "");
    throw new Error(`CouchDB ${resp.status}: ${text || resp.statusText}`);
  }
  const data = (await resp.json()) as {
    rows: { id: string; doc: CouchNoteDoc }[];
  };

  const notes: NoteInfo[] = [];
  for (const row of data.rows) {
    const rowId = row.id;
    const doc = row.doc;

    // Filter out chunk documents
    if (rowId.startsWith("h:")) continue;
    // Filter out system documents
    if (rowId.startsWith("_")) continue;
    // Filter out LiveSync version doc
    if (rowId === "obsydian_livesync_version") continue;
    // Filter out deleted docs
    if (doc.deleted) continue;

    notes.push({
      path: doc.path ?? "",
      id: rowId,
      mtime: doc.mtime ?? 0,
      size: doc.size ?? 0,
    });
  }
  return notes;
}

/** Read a note by path. Returns null if not found. */
export async function readNote(
  fetch: typeof globalThis.fetch,
  creds: VaultCredentials,
  path: string,
): Promise<Note | null> {
  const docId = pathToId(path);
  const metadata = await couchGetJson<CouchNoteDoc>(fetch, creds, docId);
  if (!metadata) return null;

  // Fetch content chunks
  const children = metadata.children ?? [];
  const chunks: string[] = [];
  for (const chunkId of children) {
    const chunk = await couchGetJson<{ data: string }>(fetch, creds, chunkId);
    if (chunk) {
      chunks.push(chunk.data ?? "");
    }
  }

  const content = chunks.join("");

  return {
    path: metadata.path ?? path,
    content,
    ctime: metadata.ctime,
    mtime: metadata.mtime,
    metadata,
  };
}

/**
 * Write or update a note. Creates content chunks and a metadata document.
 * When updating, merges into the existing CouchDB document to preserve
 * LiveSync-internal fields.
 */
export async function writeNote(
  fetchFn: typeof globalThis.fetch,
  creds: VaultCredentials,
  path: string,
  content: string,
  options?: { type?: string },
): Promise<{ ok: boolean; id: string; rev: string }> {
  const safeContent = sanitizeUnicode(content);
  const docId = pathToId(path);
  const now = Date.now();

  // Check if doc already exists
  const existingDoc = await couchGetJson<CouchNoteDoc>(fetchFn, creds, docId);

  // Create chunks
  const chunkDataList = createChunks(safeContent);
  const chunkIds: string[] = [];

  for (const chunkData of chunkDataList) {
    const chunkId = await createChunkId(chunkData);
    chunkIds.push(chunkId);

    // Check if chunk already exists
    const existing = await couchGetJson<unknown>(fetchFn, creds, chunkId);
    if (!existing) {
      await couchPut(fetchFn, creds, chunkId, {
        _id: chunkId,
        type: "leaf",
        data: chunkData,
      });
    }
  }

  let metadata: Record<string, unknown>;

  if (existingDoc) {
    // Merge into existing — preserve LiveSync-internal fields
    metadata = { ...existingDoc };
    delete metadata._revs_info;
    metadata.children = chunkIds;
    metadata.mtime = now;
    metadata.size = new TextEncoder().encode(safeContent).byteLength;
    metadata.path = path;
    // Un-delete if previously soft-deleted
    delete metadata.deleted;
  } else {
    // Brand-new note
    metadata = {
      _id: docId,
      children: chunkIds,
      path,
      ctime: now,
      mtime: now,
      size: new TextEncoder().encode(safeContent).byteLength,
      type: options?.type ?? "plain",
      eden: {},
    };
  }

  return couchPut(fetchFn, creds, docId, metadata);
}

/** Soft-delete a note (LiveSync-compatible). */
export async function deleteNote(
  fetchFn: typeof globalThis.fetch,
  creds: VaultCredentials,
  path: string,
): Promise<{ ok: boolean }> {
  const docId = pathToId(path);
  const doc = await couchGetJson<CouchNoteDoc>(fetchFn, creds, docId);
  if (!doc) {
    throw new Error(`Note not found: ${path}`);
  }

  const updated: Record<string, unknown> = { ...doc };
  updated.deleted = true;
  updated.data = "";
  updated.children = [];
  updated.mtime = Date.now();

  await couchPut(fetchFn, creds, docId, updated);
  return { ok: true };
}

/** Move a note from one path to another. */
export async function moveNote(
  fetchFn: typeof globalThis.fetch,
  creds: VaultCredentials,
  fromPath: string,
  toPath: string,
): Promise<{ ok: boolean }> {
  const note = await readNote(fetchFn, creds, fromPath);
  if (!note) {
    throw new Error(`Note not found: ${fromPath}`);
  }
  await writeNote(fetchFn, creds, toPath, note.content);
  await deleteNote(fetchFn, creds, fromPath);
  return { ok: true };
}

/** List all soft-deleted notes, optionally filtered by folder prefix. */
export async function listDeleted(
  fetchFn: typeof globalThis.fetch,
  creds: VaultCredentials,
  folder?: string,
): Promise<NoteInfo[]> {
  const url = `${buildBaseUrl(creds)}/_all_docs?include_docs=true`;
  const resp = await fetchFn(url, { headers: buildHeaders(creds) });
  if (!resp.ok) {
    const text = await resp.text().catch(() => "");
    throw new Error(`CouchDB ${resp.status}: ${text || resp.statusText}`);
  }
  const data = (await resp.json()) as {
    rows: { id: string; doc: CouchNoteDoc }[];
  };

  const deleted: NoteInfo[] = [];
  for (const row of data.rows) {
    const rowId = row.id;
    const doc = row.doc;

    if (!doc.deleted) continue;
    if (rowId.startsWith("h:") || rowId.startsWith("_")) continue;

    const notePath = doc.path ?? "";
    if (folder && !notePath.toLowerCase().startsWith(folder.toLowerCase())) {
      continue;
    }

    deleted.push({
      path: notePath,
      id: rowId,
      mtime: doc.mtime ?? 0,
      size: doc.size ?? 0,
    });
  }
  return deleted;
}

/** Recover a soft-deleted note by removing the deleted flag. */
export async function recoverNote(
  fetchFn: typeof globalThis.fetch,
  creds: VaultCredentials,
  docId: string,
): Promise<{ ok: boolean; path: string; recovered?: boolean; skipped?: boolean } | null> {
  const doc = await couchGetJson<CouchNoteDoc>(fetchFn, creds, docId);
  if (!doc) return null;

  if (!doc.deleted) {
    return { ok: true, path: doc.path ?? docId, skipped: true };
  }

  const updated: Record<string, unknown> = { ...doc };
  delete updated.deleted;

  await couchPut(fetchFn, creds, docId, updated);
  return { ok: true, path: doc.path ?? docId, recovered: true };
}
