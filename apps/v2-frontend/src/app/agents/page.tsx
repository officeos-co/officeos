"use client";

import { useState } from "react";
import { RefreshCw, Plus } from "lucide-react";
import { TopBar } from "@/components/TopBar";
import { AgentsTable } from "@/components/AgentsTable";
import { EmptyState } from "@/components/EmptyState";
import { NewAgentOverlay } from "@/components/NewAgentOverlay";
import { useAgents } from "@/hooks/useAgents";
import { Button } from "@/components/ui/button";

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
            <Button variant="outline" size="sm" onClick={() => refetch()}>
              <RefreshCw className="h-3.5 w-3.5" />
            </Button>
            <Button size="sm" onClick={() => setOverlayOpen(true)}>
              <Plus className="mr-1.5 h-3.5 w-3.5" />
              New agent
            </Button>
          </div>
        }
      />

      {error && (
        <div className="mx-8 mt-6 flex items-center justify-between rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          <span>{error}</span>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Retry
          </Button>
        </div>
      )}

      {loading && agents.length === 0 ? (
        <div className="px-8 py-12 text-sm text-muted-foreground">Loading...</div>
      ) : agents.length === 0 ? (
        <EmptyState
          title="No agents yet"
          description="Create your first agent to get started."
          action={
            <Button variant="outline" size="sm" onClick={() => setOverlayOpen(true)}>
              Get started with agents
            </Button>
          }
        />
      ) : (
        <AgentsTable agents={agents} onDelete={(id) => remove(id)} />
      )}

      <NewAgentOverlay open={overlayOpen} onClose={() => setOverlayOpen(false)} />
    </div>
  );
}
