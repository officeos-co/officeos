"use client";

import { useCallback, useEffect, useState } from "react";
import { Trash2, MessageSquare, Play } from "lucide-react";
import type { Agent } from "@/hooks/useAgents";
import { agentFetch } from "@/lib/agentProxy";
import { getOrCreateSessionId, resetSessionId } from "@/lib/chatSession";

type PodSession = {
  session_id: string;
  created_at?: string;
  last_activity?: string;
  message_count?: number;
  name?: string | null;
};

type SessionListResponse = PodSession[] | { sessions: PodSession[] };

function unwrapSessions(data: SessionListResponse): PodSession[] {
  if (Array.isArray(data)) return data;
  return data.sessions ?? [];
}

function formatRelative(iso?: string): string {
  if (!iso) return "—";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString();
}

export function AgentSessionsPanel({ agent }: { agent: Agent }) {
  const [sessions, setSessions] = useState<PodSession[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [activeId, setActiveId] = useState<string>(() => getOrCreateSessionId(agent.id));

  const load = useCallback(async () => {
    try {
      setError(null);
      const data = await agentFetch<SessionListResponse>(agent.id, "/api/sessions");
      const list = unwrapSessions(data);
      list.sort((a, b) => {
        const at = a.last_activity ?? a.created_at ?? "";
        const bt = b.last_activity ?? b.created_at ?? "";
        return bt.localeCompare(at);
      });
      setSessions(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load sessions");
      setSessions([]);
    }
  }, [agent.id]);

  useEffect(() => {
    load();
  }, [load]);

  const startNewSession = () => {
    const id = resetSessionId(agent.id);
    setActiveId(id);
    // Optimistically prepend; backend list refreshes on pod interaction
    setSessions((prev) => [
      { session_id: id, message_count: 0, created_at: new Date().toISOString() },
      ...(prev ?? []),
    ]);
  };

  const deleteSession = async (id: string) => {
    if (!confirm(`Delete session ${id.slice(0, 8)}…?`)) return;
    try {
      await agentFetch(agent.id, `/api/sessions/${encodeURIComponent(id)}`, {
        method: "DELETE",
      });
      setSessions((prev) => (prev ? prev.filter((s) => s.session_id !== id) : prev));
      if (id === activeId) {
        const fresh = resetSessionId(agent.id);
        setActiveId(fresh);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete session");
    }
  };

  const activate = (id: string) => {
    localStorage.setItem(`eaos_chat_session:${agent.id}`, id);
    setActiveId(id);
  };

  if (error && sessions === null) {
    return (
      <div className="mx-8 my-6 rounded-md border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
        {error}
      </div>
    );
  }

  if (sessions === null) {
    return (
      <div className="mx-8 my-6 text-sm text-[var(--eaos-text-muted)]">Loading sessions…</div>
    );
  }

  return (
    <div className="mx-8 my-6">
      <div className="mb-4 flex items-center justify-between">
        <div className="text-sm text-[var(--eaos-text-muted)]">
          {sessions.length} session{sessions.length === 1 ? "" : "s"}
        </div>
        <button
          type="button"
          onClick={startNewSession}
          className="flex items-center gap-1 rounded-md border border-[var(--eaos-border)] px-3 py-1.5 text-xs hover:bg-white hover:text-black"
        >
          <MessageSquare className="h-3.5 w-3.5" />
          New session
        </button>
      </div>

      {error && (
        <div className="mb-3 rounded-md border border-red-500/30 bg-red-500/10 px-4 py-2 text-xs text-red-300">
          {error}
        </div>
      )}

      {sessions.length === 0 ? (
        <div className="rounded-xl border border-dashed border-[var(--eaos-border)] bg-[var(--eaos-panel)] px-6 py-12 text-center text-sm text-[var(--eaos-text-muted)]">
          No sessions yet. Sessions are created automatically when you start a chat.
        </div>
      ) : (
        <div className="overflow-hidden rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)]">
          <table className="w-full text-sm">
            <thead className="text-left text-[var(--eaos-text-muted)]">
              <tr className="border-b border-[var(--eaos-border)]">
                <th className="px-4 py-3 font-normal">ID</th>
                <th className="px-4 py-3 font-normal">Messages</th>
                <th className="px-4 py-3 font-normal">Last activity</th>
                <th className="px-4 py-3 font-normal">Created</th>
                <th className="px-4 py-3 text-right font-normal">Actions</th>
              </tr>
            </thead>
            <tbody>
              {sessions.map((s) => {
                const isActive = s.session_id === activeId;
                return (
                  <tr
                    key={s.session_id}
                    className="border-b border-[var(--eaos-border)] last:border-b-0 hover:bg-black/30"
                  >
                    <td className="px-4 py-3 font-mono text-xs">
                      {isActive && (
                        <span className="mr-2 inline-block rounded-full border border-emerald-500/40 px-1.5 text-[10px] text-emerald-300">
                          active
                        </span>
                      )}
                      {s.session_id.slice(0, 12)}…
                    </td>
                    <td className="px-4 py-3 text-[var(--eaos-text-muted)]">
                      {s.message_count ?? "—"}
                    </td>
                    <td className="px-4 py-3 text-[var(--eaos-text-muted)]">
                      {formatRelative(s.last_activity)}
                    </td>
                    <td className="px-4 py-3 text-[var(--eaos-text-muted)]">
                      {formatRelative(s.created_at)}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => activate(s.session_id)}
                          disabled={isActive}
                          className="flex items-center gap-1 rounded-md border border-[var(--eaos-border)] px-2 py-1 text-[11px] hover:bg-white hover:text-black disabled:opacity-40"
                        >
                          <Play className="h-3 w-3" />
                          Activate
                        </button>
                        <button
                          type="button"
                          onClick={() => deleteSession(s.session_id)}
                          className="flex items-center gap-1 rounded-md border border-red-500/40 px-2 py-1 text-[11px] text-red-300 hover:bg-red-500/20"
                        >
                          <Trash2 className="h-3 w-3" />
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
