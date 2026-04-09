"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { TopBar } from "@/components/TopBar";
import { AgentDetailTabs } from "@/components/AgentDetailTabs";
import { apiFetch } from "@/hooks/useApi";
import type { Agent } from "@/hooks/useAgents";

export default function AgentDetailPage() {
  const params = useParams<{ id: string }>();
  const id = params.id;
  const [agent, setAgent] = useState<Agent | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    apiFetch<Agent>(`/api/agents/${id}`)
      .then((data) => {
        if (!cancelled) setAgent(data);
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Failed to load agent");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [id]);

  if (loading) {
    return (
      <div className="px-8 py-12 text-sm text-[var(--eaos-text-muted)]">Loading...</div>
    );
  }

  if (error || !agent) {
    return (
      <div>
        <TopBar title="Agent not found" />
        <div className="mx-8 mt-6 rounded-md border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          {error ?? "Unknown agent"}
        </div>
        <div className="mx-8 mt-4">
          <Link
            href="/agents"
            className="rounded-md border border-[var(--eaos-border)] px-3 py-1 text-xs hover:bg-white hover:text-black"
          >
            ← Back to agents
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div>
      <TopBar
        title={agent.name}
        subtitle={`${agent.model ?? "no model"} · ${agent.status}`}
        action={
          <Link
            href="/agents"
            className="rounded-md border border-[var(--eaos-border)] px-3 py-2 text-sm hover:bg-white hover:text-black"
          >
            ← All agents
          </Link>
        }
      />
      <AgentDetailTabs agent={agent} />
    </div>
  );
}
