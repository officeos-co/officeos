"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { Trash2 } from "lucide-react";
import { TopBar } from "@/components/TopBar";
import { AgentDetailTabs } from "@/components/AgentDetailTabs";
import { StatusBadge } from "@/components/StatusBadge";
import { apiFetch } from "@/hooks/useApi";
import { useAgents, type Agent } from "@/hooks/useAgents";

const POLL_INTERVAL_MS = 10_000;

export default function AgentDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const id = params.id;
  const { remove } = useAgents();
  const [agent, setAgent] = useState<Agent | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [deleting, setDeleting] = useState(false);

  const fetchAgent = useCallback(async () => {
    try {
      const data = await apiFetch<Agent>(`/api/agents/${id}`);
      setAgent(data);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load agent");
    }
  }, [id]);

  useEffect(() => {
    setLoading(true);
    fetchAgent().finally(() => setLoading(false));
  }, [fetchAgent]);

  // Auto-poll every 10s
  useEffect(() => {
    const tick = () => {
      if (document.visibilityState === "visible") {
        fetchAgent();
      }
    };
    const interval = setInterval(tick, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [fetchAgent]);

  const handleDelete = async () => {
    if (!confirm(`Delete agent "${agent?.name}"? This removes the pod, data, and vault.`)) return;
    setDeleting(true);
    try {
      await remove(id);
      router.push("/agents");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete agent");
      setDeleting(false);
    }
  };

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
        subtitle={
          <span className="flex items-center gap-2">
            {agent.model ?? "no model"} · <StatusBadge status={agent.status} />
          </span>
        }
        action={
          <div className="flex items-center gap-2">
            <button
              onClick={handleDelete}
              disabled={deleting}
              className="flex items-center gap-1 rounded-md border border-red-500/30 px-3 py-2 text-sm text-red-300 hover:bg-red-500/20 disabled:opacity-50"
            >
              <Trash2 className="h-4 w-4" />
              {deleting ? "Deleting..." : "Delete"}
            </button>
            <Link
              href="/agents"
              className="rounded-md border border-[var(--eaos-border)] px-3 py-2 text-sm hover:bg-white hover:text-black"
            >
              ← All agents
            </Link>
          </div>
        }
      />

      {agent.status === "failed" && (
        <div className="mx-8 mt-4 rounded-md border border-yellow-500/30 bg-yellow-500/10 px-4 py-3 text-sm text-yellow-300">
          This agent failed to deploy. You may want to delete it and try again.
        </div>
      )}
      {agent.status === "not_found" && (
        <div className="mx-8 mt-4 rounded-md border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          This agent's pod was not found in the cluster. It may have been deleted externally or
          crashed. Delete and recreate the agent.
        </div>
      )}
      {agent.status === "stopped" && (
        <div className="mx-8 mt-4 rounded-md border border-yellow-500/30 bg-yellow-500/10 px-4 py-3 text-sm text-yellow-300">
          This agent's pod has stopped.
        </div>
      )}

      <AgentDetailTabs agent={agent} />
    </div>
  );
}
