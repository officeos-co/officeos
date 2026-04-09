"use client";

import { useEffect, useState } from "react";
import type { Agent } from "@/hooks/useAgents";
import { apiFetch } from "@/hooks/useApi";

type Props = {
  agent: Agent;
};

export function AgentMemoryPanel({ agent }: Props) {
  const [files, setFiles] = useState<string[] | null>(null);
  const [selected, setSelected] = useState<string | null>(null);
  const [content, setContent] = useState<string>("");
  const [error, setError] = useState<string | null>(null);
  const [loadingContent, setLoadingContent] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setError(null);
    apiFetch<string[]>(`/api/agents/${agent.id}/memory`)
      .then((list) => {
        if (cancelled) return;
        setFiles(list);
        if (list.length > 0) setSelected(list[0]);
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Failed to load memory");
      });
    return () => {
      cancelled = true;
    };
  }, [agent.id]);

  useEffect(() => {
    if (!selected) {
      setContent("");
      return;
    }
    let cancelled = false;
    setLoadingContent(true);
    fetch(`/api/agents/${agent.id}/memory/${encodeURIComponent(selected)}`)
      .then(async (res) => {
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
        return res.text();
      })
      .then((text) => {
        if (!cancelled) setContent(text);
      })
      .catch((err) => {
        if (!cancelled) setContent(`Failed to load: ${err instanceof Error ? err.message : String(err)}`);
      })
      .finally(() => {
        if (!cancelled) setLoadingContent(false);
      });
    return () => {
      cancelled = true;
    };
  }, [agent.id, selected]);

  if (error) {
    return (
      <div className="mx-8 my-6 rounded-md border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
        {error}
      </div>
    );
  }

  if (files === null) {
    return (
      <div className="mx-8 my-6 text-sm text-[var(--eaos-text-muted)]">Loading memory...</div>
    );
  }

  if (files.length === 0) {
    return (
      <div className="mx-8 my-6 rounded-xl border border-dashed border-[var(--eaos-border)] bg-[var(--eaos-panel)] px-6 py-12 text-center text-sm text-[var(--eaos-text-muted)]">
        No memory files found for this agent.
      </div>
    );
  }

  return (
    <div className="mx-8 my-6 grid grid-cols-1 gap-4 md:grid-cols-[240px_1fr]">
      <div className="rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)] p-2">
        <ul className="flex flex-col">
          {files.map((f) => {
            const isActive = f === selected;
            return (
              <li key={f}>
                <button
                  onClick={() => setSelected(f)}
                  className={[
                    "w-full rounded-md px-3 py-2 text-left font-mono text-xs",
                    isActive
                      ? "bg-white text-black"
                      : "text-[var(--eaos-text-muted)] hover:bg-black/30 hover:text-white",
                  ].join(" ")}
                >
                  {f}
                </button>
              </li>
            );
          })}
        </ul>
      </div>
      <div className="rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)] p-4">
        {loadingContent ? (
          <div className="text-sm text-[var(--eaos-text-muted)]">Loading...</div>
        ) : (
          <pre className="whitespace-pre-wrap break-words font-mono text-xs leading-relaxed">
            {content}
          </pre>
        )}
      </div>
    </div>
  );
}
