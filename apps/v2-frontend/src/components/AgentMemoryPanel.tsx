"use client";

import { useCallback, useEffect, useState } from "react";
import { FilePlus, Pencil, Save, Trash2, X, RefreshCw, AlertTriangle } from "lucide-react";
import type { Agent } from "@/hooks/useAgents";
import { apiFetch } from "@/hooks/useApi";

type Props = {
  agent: Agent;
};

export function AgentMemoryPanel({ agent }: Props) {
  const [files, setFiles] = useState<string[] | null>(null);
  const [selected, setSelected] = useState<string | null>(null);
  const [content, setContent] = useState<string>("");
  const [draft, setDraft] = useState<string>("");
  const [editing, setEditing] = useState(false);
  const [loadingContent, setLoadingContent] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [creatingNew, setCreatingNew] = useState(false);
  const [newFileName, setNewFileName] = useState("");

  const loadList = useCallback(async () => {
    setError(null);
    try {
      const list = await apiFetch<string[]>(`/api/agents/${agent.id}/memory`);
      setFiles(list);
      if (selected === null && list.length > 0) {
        setSelected(list[0]);
      } else if (selected && !list.includes(selected)) {
        setSelected(list[0] ?? null);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load memory");
      setFiles([]);
    }
  }, [agent.id, selected]);

  useEffect(() => {
    loadList();
  }, [loadList]);

  useEffect(() => {
    if (!selected) {
      setContent("");
      setDraft("");
      return;
    }
    let cancelled = false;
    setLoadingContent(true);
    setEditing(false);
    fetch(`/api/agents/${agent.id}/memory/${encodeURIComponent(selected)}`)
      .then(async (res) => {
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
        return res.text();
      })
      .then((text) => {
        if (!cancelled) {
          setContent(text);
          setDraft(text);
        }
      })
      .catch((err) => {
        if (!cancelled) {
          const msg = err instanceof Error ? err.message : String(err);
          setContent(`Failed to load: ${msg}`);
          setDraft("");
        }
      })
      .finally(() => {
        if (!cancelled) setLoadingContent(false);
      });
    return () => {
      cancelled = true;
    };
  }, [agent.id, selected]);

  const save = async () => {
    if (!selected) return;
    setSaving(true);
    setError(null);
    try {
      await apiFetch(`/api/agents/${agent.id}/memory/${encodeURIComponent(selected)}`, {
        method: "PUT",
        body: JSON.stringify({ content: draft }),
      });
      setContent(draft);
      setEditing(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save file");
    } finally {
      setSaving(false);
    }
  };

  const cancelEdit = () => {
    setDraft(content);
    setEditing(false);
  };

  const remove = async (fileName: string) => {
    if (!confirm(`Delete ${fileName} from the vault? This cannot be undone.`)) return;
    setError(null);
    try {
      await apiFetch(`/api/agents/${agent.id}/memory/${encodeURIComponent(fileName)}`, {
        method: "DELETE",
      });
      setFiles((prev) => (prev ? prev.filter((f) => f !== fileName) : prev));
      if (selected === fileName) {
        setSelected(null);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete file");
    }
  };

  const createFile = async () => {
    const name = newFileName.trim();
    if (!name) return;
    setError(null);
    try {
      await apiFetch(`/api/agents/${agent.id}/memory/${encodeURIComponent(name)}`, {
        method: "PUT",
        body: JSON.stringify({ content: `# ${name.replace(/\.md$/i, "")}\n\n` }),
      });
      setCreatingNew(false);
      setNewFileName("");
      await loadList();
      setSelected(name);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create file");
    }
  };

  if (files === null) {
    return <div className="mx-8 my-6 text-sm text-muted-foreground">Loading memory…</div>;
  }

  return (
    <div className="mx-8 my-6">
      <div className="mb-3 flex items-start gap-2 rounded-md border border-yellow-500/30 bg-yellow-500/5 px-3 py-2 text-xs text-yellow-200">
        <AlertTriangle className="mt-0.5 h-3.5 w-3.5 flex-shrink-0" />
        <div>
          CouchDB is the source of truth for vault files. The agent caches them on its PVC at
          boot — edits made here take effect on the next pod restart.
        </div>
      </div>

      {error && (
        <div className="mb-3 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-2 text-xs text-destructive">
          {error}
        </div>
      )}

      <div className="grid grid-cols-1 gap-4 md:grid-cols-[260px_1fr]">
        <div className="rounded-xl border border-border bg-card p-2">
          <div className="mb-2 flex items-center justify-between px-2 pt-1">
            <span className="text-[11px] uppercase tracking-wider text-muted-foreground">
              Files
            </span>
            <button
              type="button"
              onClick={loadList}
              className="rounded p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
              aria-label="Refresh"
            >
              <RefreshCw className="h-3 w-3" />
            </button>
          </div>
          <ul className="flex flex-col">
            {files.map((f) => {
              const isActive = f === selected;
              return (
                <li key={f} className="group flex items-center gap-1">
                  <button
                    type="button"
                    onClick={() => setSelected(f)}
                    className={[
                      "flex-1 rounded-md px-3 py-2 text-left font-mono text-xs",
                      isActive
                        ? "bg-primary text-primary-foreground"
                        : "text-muted-foreground hover:bg-accent hover:text-foreground",
                    ].join(" ")}
                  >
                    {f}
                  </button>
                  <button
                    type="button"
                    onClick={() => remove(f)}
                    className="rounded p-1 text-muted-foreground opacity-0 hover:text-destructive group-hover:opacity-100"
                    aria-label={`Delete ${f}`}
                  >
                    <Trash2 className="h-3 w-3" />
                  </button>
                </li>
              );
            })}
          </ul>
          <div className="mt-2 border-t border-border pt-2">
            {creatingNew ? (
              <div className="flex flex-col gap-1 px-2">
                <input
                  value={newFileName}
                  onChange={(e) => setNewFileName(e.target.value)}
                  placeholder="NOTES.md"
                  className="rounded-md border border-border bg-muted px-2 py-1 text-xs outline-none focus:border-primary"
                />
                <div className="flex gap-1">
                  <button
                    type="button"
                    onClick={createFile}
                    disabled={!newFileName.trim()}
                    className="flex-1 rounded border border-border px-2 py-1 text-[11px] hover:bg-primary hover:text-primary-foreground disabled:opacity-40"
                  >
                    Create
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setCreatingNew(false);
                      setNewFileName("");
                    }}
                    className="rounded border border-border px-2 py-1 text-[11px] hover:bg-accent"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </div>
              </div>
            ) : (
              <button
                type="button"
                onClick={() => setCreatingNew(true)}
                className="flex w-full items-center justify-center gap-1 rounded-md px-2 py-2 text-[11px] text-muted-foreground hover:bg-accent hover:text-foreground"
              >
                <FilePlus className="h-3 w-3" />
                New file
              </button>
            )}
          </div>
        </div>

        <div className="rounded-xl border border-border bg-card p-4">
          {!selected ? (
            <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
              Select a file to view or edit.
            </div>
          ) : loadingContent ? (
            <div className="text-sm text-muted-foreground">Loading…</div>
          ) : editing ? (
            <div className="flex h-full flex-col gap-3">
              <div className="flex items-center justify-between">
                <span className="font-mono text-xs text-muted-foreground">{selected}</span>
                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={save}
                    disabled={saving || draft === content}
                    className="flex items-center gap-1 rounded-md bg-primary px-3 py-1 text-xs font-medium text-primary-foreground disabled:opacity-50"
                  >
                    <Save className="h-3 w-3" />
                    {saving ? "Saving…" : "Save"}
                  </button>
                  <button
                    type="button"
                    onClick={cancelEdit}
                    className="flex items-center gap-1 rounded-md border border-border px-3 py-1 text-xs hover:bg-accent"
                  >
                    <X className="h-3 w-3" />
                    Cancel
                  </button>
                </div>
              </div>
              <textarea
                value={draft}
                onChange={(e) => setDraft(e.target.value)}
                spellCheck={false}
                className="min-h-[400px] flex-1 resize-none rounded-md border border-border bg-accent p-3 font-mono text-xs outline-none focus:border-primary"
              />
            </div>
          ) : (
            <div className="flex h-full flex-col gap-3">
              <div className="flex items-center justify-between">
                <span className="font-mono text-xs text-muted-foreground">{selected}</span>
                <button
                  type="button"
                  onClick={() => {
                    setDraft(content);
                    setEditing(true);
                  }}
                  className="flex items-center gap-1 rounded-md border border-border px-3 py-1 text-xs hover:bg-primary hover:text-primary-foreground"
                >
                  <Pencil className="h-3 w-3" />
                  Edit
                </button>
              </div>
              <pre className="min-h-[400px] flex-1 overflow-auto whitespace-pre-wrap break-words rounded-md border border-border bg-muted p-3 font-mono text-xs leading-relaxed">
                {content}
              </pre>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
