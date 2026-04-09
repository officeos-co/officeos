"use client";

import { useState } from "react";
import { TopBar } from "@/components/TopBar";
import { AgentsTable } from "@/components/AgentsTable";
import { EmptyState } from "@/components/EmptyState";
import { NewAgentOverlay } from "@/components/NewAgentOverlay";
import { useAgents } from "@/hooks/useAgents";

export default function AgentsPage() {
  const { agents, loading, error } = useAgents();
  const [overlayOpen, setOverlayOpen] = useState(false);

  return (
    <div>
      <TopBar
        title="Agents"
        subtitle="Deploy and manage agents running in your workspace."
        action={
          <button
            onClick={() => setOverlayOpen(true)}
            className="rounded-md bg-white px-4 py-2 text-sm font-medium text-black hover:bg-white/90"
          >
            + New agent
          </button>
        }
      />

      {error && (
        <div className="mx-8 mt-6 rounded-md border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          {error}
        </div>
      )}

      {loading && agents.length === 0 ? (
        <div className="px-8 py-12 text-sm text-[var(--eaos-text-muted)]">Loading...</div>
      ) : agents.length === 0 ? (
        <EmptyState
          title="No agents yet"
          description="Create your first agent to get started."
          action={
            <button
              onClick={() => setOverlayOpen(true)}
              className="rounded-md border border-[var(--eaos-border)] bg-black px-4 py-2 text-sm hover:bg-white hover:text-black"
            >
              Get started with agents
            </button>
          }
        />
      ) : (
        <AgentsTable agents={agents} />
      )}

      <NewAgentOverlay open={overlayOpen} onClose={() => setOverlayOpen(false)} />
    </div>
  );
}
