"use client";

// Vault browser — read-only view of the CouchDB per-agent vaults.
//
// Left sidebar: database picker + .md file list for the selected
// database. Main panel: raw markdown content of the selected file
// (no rendering — straight text).

import { useCallback, useEffect, useState } from "react";

import { getSessionToken } from "@/auth/clerk";
import { getApiBaseUrl } from "@/lib/api-base";

export const dynamic = "force-dynamic";

type FileEntry = {
  path: string;
  id: string;
  mtime?: number | null;
  size?: number | null;
};

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getSessionToken();
  const res = await fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    headers: {
      ...(init?.headers || {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`${res.status} ${res.statusText}: ${text}`);
  }
  return (await res.json()) as T;
}

export default function VaultPage() {
  const [databases, setDatabases] = useState<string[]>([]);
  const [selectedDb, setSelectedDb] = useState<string | null>(null);
  const [files, setFiles] = useState<FileEntry[]>([]);
  const [selectedPath, setSelectedPath] = useState<string | null>(null);
  const [content, setContent] = useState<string>("");
  const [error, setError] = useState<string | null>(null);
  const [loadingList, setLoadingList] = useState(false);
  const [loadingNote, setLoadingNote] = useState(false);

  const loadDatabases = useCallback(async () => {
    setError(null);
    try {
      const data = await api<{ databases: string[] }>("/api/vault/databases");
      setDatabases(data.databases);
      if (data.databases.length > 0 && selectedDb === null) {
        setSelectedDb(data.databases[0]);
      }
    } catch (e) {
      setError((e as Error).message);
    }
  }, [selectedDb]);

  const loadFiles = useCallback(async (db: string) => {
    setLoadingList(true);
    setError(null);
    setFiles([]);
    setSelectedPath(null);
    setContent("");
    try {
      const data = await api<{ files: FileEntry[] }>(
        `/api/vault/${encodeURIComponent(db)}/files`,
      );
      setFiles(data.files);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoadingList(false);
    }
  }, []);

  const loadNote = useCallback(async (db: string, path: string) => {
    setLoadingNote(true);
    setError(null);
    try {
      const data = await api<{ content: string }>(
        `/api/vault/${encodeURIComponent(db)}/files?path=${encodeURIComponent(path)}`,
      );
      setContent(data.content);
    } catch (e) {
      setError((e as Error).message);
      setContent("");
    } finally {
      setLoadingNote(false);
    }
  }, []);

  useEffect(() => {
    void loadDatabases();
  }, [loadDatabases]);

  useEffect(() => {
    if (selectedDb) void loadFiles(selectedDb);
  }, [selectedDb, loadFiles]);

  useEffect(() => {
    if (selectedDb && selectedPath) void loadNote(selectedDb, selectedPath);
  }, [selectedDb, selectedPath, loadNote]);

  return (
    <main className="mx-auto flex h-[calc(100vh-4rem)] max-w-[1600px] gap-4 px-6 py-6">
      {/* Sidebar: database picker + file list */}
      <aside className="flex w-80 shrink-0 flex-col gap-3 rounded-2xl border border-[color:var(--border)] bg-[color:var(--surface)] p-4">
        <div>
          <label className="mb-1 block text-xs font-semibold uppercase tracking-wide text-muted">
            Database
          </label>
          <select
            className="w-full rounded-md border border-[color:var(--border)] bg-transparent px-3 py-2 text-sm"
            value={selectedDb ?? ""}
            onChange={(e) => setSelectedDb(e.target.value || null)}
          >
            {databases.length === 0 && <option value="">(none)</option>}
            {databases.map((db) => (
              <option key={db} value={db}>
                {db}
              </option>
            ))}
          </select>
        </div>

        <div className="flex items-center justify-between">
          <span className="text-xs font-semibold uppercase tracking-wide text-muted">
            Files
          </span>
          <span className="text-xs text-muted">{files.length}</span>
        </div>

        <div className="flex-1 overflow-y-auto">
          {loadingList ? (
            <p className="px-2 py-1 text-xs text-muted">Loading…</p>
          ) : files.length === 0 ? (
            <p className="px-2 py-1 text-xs text-muted">No files.</p>
          ) : (
            <ul className="space-y-0.5">
              {files.map((f) => (
                <li key={f.id}>
                  <button
                    type="button"
                    onClick={() => setSelectedPath(f.path)}
                    className={`w-full truncate rounded-md px-2 py-1.5 text-left text-xs transition ${
                      selectedPath === f.path
                        ? "bg-blue-100 text-blue-900 font-medium"
                        : "text-slate-700 hover:bg-slate-100"
                    }`}
                    title={f.path}
                  >
                    {f.path}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </aside>

      {/* Main panel: raw .md content */}
      <section className="flex flex-1 flex-col gap-3 rounded-2xl border border-[color:var(--border)] bg-[color:var(--surface)] p-4">
        <header className="flex items-center justify-between">
          <h1 className="truncate font-mono text-sm">
            {selectedPath ?? "Select a file to view"}
          </h1>
          {loadingNote && <span className="text-xs text-muted">Loading…</span>}
        </header>
        {error && (
          <div className="rounded-md border border-red-500/40 bg-red-500/10 px-3 py-2 text-xs text-red-400">
            {error}
          </div>
        )}
        <pre className="flex-1 overflow-auto whitespace-pre-wrap break-words rounded-md border border-[color:var(--border)] bg-[color:var(--surface-muted,transparent)] p-4 font-mono text-xs leading-relaxed">
          {content}
        </pre>
      </section>
    </main>
  );
}
