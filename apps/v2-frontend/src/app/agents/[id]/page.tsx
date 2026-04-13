"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { Trash2 } from "lucide-react";
import { AgentDetailTabs } from "@/components/agents/AgentDetailTabs";
import { StatusBadge } from "@/components/shared/StatusBadge";
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
      <div className="px-8 py-12 text-sm text-muted-foreground">Loading…</div>
    );
  }

  if (error || !agent) {
    return (
      <div className="px-8 py-12">
        <div className="mb-4 text-sm text-muted-foreground">
          <Link href="/agents" className="hover:text-foreground transition-colors">
            Agents
          </Link>
        </div>
        <div className="mb-4 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {error ?? "Unknown agent"}
        </div>
        <Link
          href="/agents"
          className="rounded-md border border-border px-3 py-1 text-xs hover:bg-primary hover:text-primary-foreground"
        >
          ← Back to agents
        </Link>
      </div>
    );
  }

  return (
    <div className="flex h-full flex-col overflow-hidden">
      {/* ── Sticky header ──────────────────────────────────────────── */}
      <div className="shrink-0 bg-background z-20">
        <div className="px-8 pt-8 pb-0">
          {/* Breadcrumb */}
          <div className="mb-4 text-sm text-muted-foreground">
            <Link href="/agents" className="hover:text-foreground transition-colors">
              Agents
            </Link>
            {" / "}
            <span>{agent.name}</span>
          </div>

          {/* Title row */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <h1 className="text-2xl font-semibold">{agent.name}</h1>
              <StatusBadge status={agent.status} />
            </div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={handleDelete}
                disabled={deleting}
                className="flex items-center gap-1.5 rounded-md border border-destructive/30 px-3 py-2 text-sm text-destructive hover:bg-red-100 disabled:opacity-50 transition-colors"
              >
                <Trash2 className="h-4 w-4" />
                {deleting ? "Deleting…" : "Delete"}
              </button>
            </div>
          </div>

          {/* Agent ID + model */}
          <div className="mt-2 flex items-center gap-2 font-mono text-xs text-muted-foreground">
            <span>{agent.id}</span>
            <span>·</span>
            <span>{agent.model ?? "auto"}</span>
          </div>
        </div>

        {/* Status banners */}
        {agent.status === "failed" && (
          <div className="mx-8 mt-4 rounded-md border border-yellow-200 bg-yellow-50 px-4 py-3 text-sm text-yellow-700">
            This agent failed to deploy. You may want to delete it and try again.
          </div>
        )}
        {agent.status === "not_found" && (
          <div className="mx-8 mt-4 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
            This agent&apos;s pod was not found in the cluster. It may have been deleted externally or
            crashed. Delete and recreate the agent.
          </div>
        )}
        {agent.status === "stopped" && (
          <div className="mx-8 mt-4 rounded-md border border-yellow-200 bg-yellow-50 px-4 py-3 text-sm text-yellow-700">
            This agent&apos;s pod has stopped.
          </div>
        )}
      </div>

      {/* ── Tabs (bar is sticky, content scrolls) ─────────────────── */}
      <div className="flex-1 overflow-hidden mt-6">
        <AgentDetailTabs agent={agent} onAgentUpdated={fetchAgent} />
      </div>
    </div>
  );
}
