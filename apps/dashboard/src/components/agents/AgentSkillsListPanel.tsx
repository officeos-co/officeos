"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Wrench, RefreshCw } from "lucide-react";
import type { Agent } from "@/types/agent";
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
  configured: "border-emerald-200 bg-emerald-50 text-emerald-700",
  installed: "border-yellow-200 bg-yellow-50 text-yellow-700",
  available: "border-border bg-muted text-muted-foreground",
};

function skillStatus(s: Skill): keyof typeof statusStyle {
  if (s.configured) return "configured";
  if (s.installed) return "installed";
  return "available";
}

export function AgentSkillsListPanel({ agent }: { agent: Agent }) {
  const [skills, setSkills] = useState<Skill[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();

  const load = async () => {
    setError(null);
    try {
      const data = await apiFetch<Skill[]>("/api/skills");
      setSkills(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load tools");
      setSkills([]);
    }
  };

  useEffect(() => {
    load();
  }, [agent.id]);

  if (skills === null) {
    return (
      <div className="mx-8 my-6 text-sm text-muted-foreground">Loading tools…</div>
    );
  }

  return (
    <div className="mx-8 my-6">
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Wrench className="h-4 w-4" />
          {skills.length} tool{skills.length === 1 ? "" : "s"}
        </div>
        <button
          type="button"
          onClick={load}
          className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 text-xs hover:bg-accent"
        >
          <RefreshCw className="h-3 w-3" />
          Refresh
        </button>
      </div>

      {error && (
        <div className="mb-3 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-2 text-xs text-destructive">
          {error}
        </div>
      )}

      {skills.length === 0 ? (
        <div className="rounded-xl border border-dashed border-border bg-card px-6 py-12 text-center text-sm text-muted-foreground">
          No tools available.
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
          {skills.map((s) => {
            const status = skillStatus(s);
            return (
              <button
                key={s.name}
                type="button"
                onClick={() => router.push(`/skills/${s.name}`)}
                className="rounded-xl border border-border bg-card p-4 text-left transition-colors hover:bg-muted"
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2 text-sm font-medium">
                      <span>{s.emoji}</span>
                      <span>{s.title}</span>
                    </div>
                    <div className="mt-1 text-xs text-muted-foreground">
                      {s.description}
                    </div>
                    {s.configured && s.llmTools.length > 0 && (
                      <div className="mt-2 flex flex-wrap gap-1">
                        {s.llmTools.map((t) => (
                          <span
                            key={t.name}
                            className="rounded border border-border px-1.5 py-0.5 font-mono text-[10px] text-muted-foreground"
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
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
