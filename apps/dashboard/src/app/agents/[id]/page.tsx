"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import posthog from "posthog-js";
import { Trash2 } from "lucide-react";
import { AgentDetailTabs } from "@/components/agents/AgentDetailTabs";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
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
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

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
    setDeleting(true);
    try {
      await remove(id);
      posthog.capture("agent_deleted", { agent_id: id, agent_name: agent?.name });
      router.push("/agents");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete agent");
      setDeleting(false);
    }
  };

  if (loading) {
    return (
      <div className="flex h-full flex-col overflow-hidden">
        <div className="shrink-0 bg-background z-20">
          <div className="px-8 pt-8 pb-0">
            {/* Breadcrumb skeleton */}
            <div className="mb-4 flex items-center gap-1">
              <Skeleton className="h-4 w-14" />
              <span className="text-muted-foreground">/</span>
              <Skeleton className="h-4 w-28" />
            </div>
            {/* Title row skeleton */}
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <Skeleton className="h-7 w-48" />
                <Skeleton className="h-6 w-16 rounded-full" />
              </div>
              <Skeleton className="h-9 w-24 rounded-md" />
            </div>
            {/* ID + model skeleton */}
            <div className="mt-2 flex items-center gap-2">
              <Skeleton className="h-3 w-64" />
              <Skeleton className="h-3 w-20" />
            </div>
          </div>
        </div>
        {/* Tabs skeleton */}
        <div className="flex-1 overflow-hidden mt-6">
          <div className="flex h-full flex-col">
            <div className="shrink-0 flex gap-1 border-b border-border bg-background px-8">
              {Array.from({ length: 7 }).map((_, i) => (
                <Skeleton key={i} className="h-5 w-16 my-3" />
              ))}
            </div>
            <div className="flex-1 overflow-y-auto px-8 py-6 space-y-6">
              {/* Model selector skeleton */}
              <div className="py-4 border-b border-border">
                <Skeleton className="h-3 w-16 mb-2" />
                <Skeleton className="h-10 w-full rounded-lg" />
              </div>
              {/* Tools section skeleton */}
              <div className="py-4">
                <Skeleton className="h-3 w-28 mb-3" />
                <div className="space-y-3">
                  {Array.from({ length: 3 }).map((_, i) => (
                    <div key={i} className="flex items-center gap-3 rounded-lg border border-border p-4">
                      <Skeleton className="h-8 w-8 rounded-lg shrink-0" />
                      <div className="flex-1 space-y-2">
                        <Skeleton className="h-4 w-32" />
                        <Skeleton className="h-3 w-48" />
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
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
                onClick={() => setDeleteDialogOpen(true)}
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

      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete agent?</AlertDialogTitle>
            <AlertDialogDescription>
              This removes the pod, all session data, and the agent vault. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Delete agent
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
