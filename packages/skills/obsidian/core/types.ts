/**
 * Shared types for the Obsidian vault CouchDB backend.
 *
 * These mirror the document shapes used by Obsidian LiveSync in CouchDB.
 */

/** Credentials needed to connect to a CouchDB-backed vault. */
export interface VaultCredentials {
  couchdb_url: string;
  couchdb_user: string;
  couchdb_password: string;
  vault_name: string;
}

/** A CouchDB document representing a vault note (metadata row). */
export interface CouchNoteDoc {
  _id: string;
  _rev?: string;
  path: string;
  children: string[];
  ctime?: number;
  mtime?: number;
  size?: number;
  type?: string;
  eden?: Record<string, unknown>;
  deleted?: boolean;
  [key: string]: unknown;
}

/** A CouchDB chunk document (content leaf). */
export interface CouchChunkDoc {
  _id: string;
  _rev?: string;
  type: "leaf";
  data: string;
}

/** A note as returned by read operations. */
export interface Note {
  path: string;
  content: string;
  ctime?: number;
  mtime?: number;
  metadata?: CouchNoteDoc;
}

/** Minimal note info from list operations. */
export interface NoteInfo {
  path: string;
  id: string;
  mtime: number;
  size: number;
}

/** A search match result. */
export interface SearchMatch {
  path: string;
  line: string;
  line_number: number;
  context?: string[];
}

/** An unresolved wikilink reference. */
export interface UnresolvedLink {
  source: string;
  link: string;
}

/** Result of a backlink update operation. */
export interface BacklinkUpdateResult {
  total_links: number;
  total_notes: number;
  details: { path: string; count: number }[];
}
