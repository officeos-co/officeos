"use client";

import { useState } from "react";
import { RefreshCw, Plus } from "lucide-react";
import { TopBar } from "@/components/shared/TopBar";
import { AgentsTable } from "@/components/agents/AgentsTable";
import { EmptyState } from "@/components/shared/EmptyState";
import { NewAgentOverlay } from "@/components/agents/NewAgentOverlay";
import { useAgents } from "@/hooks/useAgents";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";

export default function AgentsPage() {
  const { agents, loading, error, refetch, remove } = useAgents();
  const [overlayOpen, setOverlayOpen] = useState(false);

  return (
    <div>
      <TopBar
        title="Agents"
        action={
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="sm" onClick={() => refetch()} className="h-7 w-7 p-0">
              <RefreshCw className="h-3.5 w-3.5" />
            </Button>
            <Button size="sm" onClick={() => setOverlayOpen(true)} className="h-7">
              <Plus className="mr-1 h-3 w-3" />
              New agent
            </Button>
          </div>
        }
      />

      {error && (
        <div className="mx-6 mt-4 flex items-center justify-between rounded-md border border-destructive/20 bg-destructive/5 px-4 py-2.5 text-[13px] text-destructive">
          <span>{error}</span>
          <Button variant="ghost" size="sm" onClick={() => refetch()} className="h-7 text-[12px]">
            Retry
          </Button>
        </div>
      )}

      {loading && agents.length === 0 ? (
        <div className="px-6 py-6 space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="flex items-center gap-4 py-3">
              <Skeleton className="h-4 w-16" />
              <Skeleton className="h-4 w-28" />
              <Skeleton className="h-4 w-20" />
              <Skeleton className="h-4 w-16" />
              <Skeleton className="h-3 w-12" />
            </div>
          ))}
        </div>
      ) : agents.length === 0 ? (
        <EmptyState
          title="No agents yet"
          description="Create your first agent to get started."
          action={
            <Button size="sm" onClick={() => setOverlayOpen(true)}>
              Create agent
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
