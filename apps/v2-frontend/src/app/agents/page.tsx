"use client";

import { useState } from "react";
import { RefreshCw } from "lucide-react";
import { TopBar } from "@/components/TopBar";
import { AgentsTable } from "@/components/AgentsTable";
import { EmptyState } from "@/components/EmptyState";
import { NewAgentOverlay } from "@/components/NewAgentOverlay";
import { useAgents } from "@/hooks/useAgents";

export default function AgentsPage() {
  const { agents, loading, error, refetch, remove } = useAgents();
  const [overlayOpen, setOverlayOpen] = useState(false);

  return (
    <div>
      <TopBar
        title="Agents"
        subtitle="Deploy and manage agents running in your workspace."
        action={
          <div className="flex items-center gap-2">
            <button
              onClick={() => refetch()}
              className="rounded-md border border-[var(--eaos-border)] p-2 text-[var(--eaos-text-muted)] hover:bg-white hover:text-black"
              aria-label="Refresh"
            >
              <RefreshCw className="h-4 w-4" />
            </button>
            <button
              onClick={() => setOverlayOpen(true)}
              className="rounded-md bg-white px-4 py-2 text-sm font-medium text-black hover:bg-white/90"
            >
              + New agent
            </button>
          </div>
        }
      />

      {error && (
        <div className="mx-8 mt-6 flex items-center justify-between rounded-md border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          <span>{error}</span>
          <button
            onClick={() => refetch()}
            className="ml-4 rounded border border-red-500/30 px-3 py-1 text-xs hover:bg-red-500/20"
          >
            Retry
          </button>
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
        <AgentsTable agents={agents} onDelete={(id) => remove(id)} />
      )}

      <NewAgentOverlay open={overlayOpen} onClose={() => setOverlayOpen(false)} />
    </div>
  );
}
