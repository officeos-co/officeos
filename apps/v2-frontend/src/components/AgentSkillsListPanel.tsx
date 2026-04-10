"use client";

import { useEffect, useState } from "react";
import { Puzzle, RefreshCw } from "lucide-react";
import type { Agent } from "@/hooks/useAgents";
import { apiFetch } from "@/hooks/useApi";

type Skill = {
  name: string;
  title: string;
  description: string;
  emoji: string;
  installed: boolean;
  configured: boolean;
  llmTools: { name: string; description: string }[];
};

const statusStyle = {
  configured: "border-emerald-500/40 bg-emerald-500/10 text-emerald-300",
  installed: "border-yellow-500/40 bg-yellow-500/10 text-yellow-300",
  available: "border-[var(--eaos-border)] bg-black/30 text-[var(--eaos-text-muted)]",
};

function skillStatus(s: Skill): keyof typeof statusStyle {
  if (s.configured) return "configured";
  if (s.installed) return "installed";
  return "available";
}

export function AgentSkillsListPanel({ agent }: { agent: Agent }) {
  const [skills, setSkills] = useState<Skill[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setError(null);
    try {
      const data = await apiFetch<Skill[]>("/api/skills");
      setSkills(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load skills");
      setSkills([]);
    }
  };

  useEffect(() => {
    load();
  }, [agent.id]);

  if (skills === null) {
    return (
      <div className="mx-8 my-6 text-sm text-[var(--eaos-text-muted)]">Loading skills…</div>
    );
  }

  return (
    <div className="mx-8 my-6">
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2 text-sm text-[var(--eaos-text-muted)]">
          <Puzzle className="h-4 w-4" />
          {skills.length} skill{skills.length === 1 ? "" : "s"}
        </div>
        <button
          type="button"
          onClick={load}
          className="flex items-center gap-1 rounded-md border border-[var(--eaos-border)] px-3 py-1.5 text-xs hover:bg-black/40"
        >
          <RefreshCw className="h-3 w-3" />
          Refresh
        </button>
      </div>

      {error && (
        <div className="mb-3 rounded-md border border-red-500/30 bg-red-500/10 px-4 py-2 text-xs text-red-300">
          {error}
        </div>
      )}

      {skills.length === 0 ? (
        <div className="rounded-xl border border-dashed border-[var(--eaos-border)] bg-[var(--eaos-panel)] px-6 py-12 text-center text-sm text-[var(--eaos-text-muted)]">
          No skills available.
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
          {skills.map((s) => {
            const status = skillStatus(s);
            return (
              <div
                key={s.name}
                className="rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)] p-4"
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2 text-sm font-medium">
                      <span>{s.emoji}</span>
                      <span>{s.title}</span>
                    </div>
                    <div className="mt-1 text-xs text-[var(--eaos-text-muted)]">
                      {s.description}
                    </div>
                    {s.configured && s.llmTools.length > 0 && (
                      <div className="mt-2 flex flex-wrap gap-1">
                        {s.llmTools.map((t) => (
                          <span
                            key={t.name}
                            className="rounded border border-[var(--eaos-border)] px-1.5 py-0.5 font-mono text-[10px] text-[var(--eaos-text-muted)]"
                            title={t.description}
                          >
                            {t.name}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                  <span
                    className={`flex-shrink-0 rounded border px-2 py-0.5 text-[10px] ${statusStyle[status]}`}
                  >
                    {status}
                  </span>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
