"use client";

import { useEffect, useState } from "react";
import { Plug, RefreshCw } from "lucide-react";
import type { Agent } from "@/hooks/useAgents";
import { agentFetch } from "@/lib/agentProxy";

type IntegrationStatus = "Available" | "Active" | "ComingSoon";

type Integration = {
  name: string;
  description: string;
  category: string;
  status: IntegrationStatus;
};

type IntegrationsResponse = Integration[] | { integrations: Integration[] };

function unwrap(data: IntegrationsResponse): Integration[] {
  if (Array.isArray(data)) return data;
  return data.integrations ?? [];
}

const statusStyle: Record<IntegrationStatus, string> = {
  Active: "border-emerald-500/40 bg-emerald-500/10 text-emerald-300",
  Available: "border-[var(--eaos-border)] bg-black/30 text-[var(--eaos-text-muted)]",
  ComingSoon: "border-yellow-500/40 bg-yellow-500/10 text-yellow-300",
};

export function AgentIntegrationsPanel({ agent }: { agent: Agent }) {
  const [integrations, setIntegrations] = useState<Integration[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setError(null);
    try {
      const data = await agentFetch<IntegrationsResponse>(agent.id, "/api/integrations");
      setIntegrations(unwrap(data));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load integrations");
      setIntegrations([]);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [agent.id]);

  if (integrations === null) {
    return (
      <div className="mx-8 my-6 text-sm text-[var(--eaos-text-muted)]">Loading integrations…</div>
    );
  }

  const byCategory = integrations.reduce<Record<string, Integration[]>>((acc, i) => {
    (acc[i.category] ??= []).push(i);
    return acc;
  }, {});

  return (
    <div className="mx-8 my-6">
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2 text-sm text-[var(--eaos-text-muted)]">
          <Plug className="h-4 w-4" />
          {integrations.length} integration{integrations.length === 1 ? "" : "s"}
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

      {Object.keys(byCategory).length === 0 ? (
        <div className="rounded-xl border border-dashed border-[var(--eaos-border)] bg-[var(--eaos-panel)] px-6 py-12 text-center text-sm text-[var(--eaos-text-muted)]">
          No integrations reported.
        </div>
      ) : (
        <div className="space-y-6">
          {Object.entries(byCategory)
            .sort(([a], [b]) => a.localeCompare(b))
            .map(([category, items]) => (
              <div key={category}>
                <div className="mb-2 text-[11px] uppercase tracking-wider text-[var(--eaos-text-muted)]">
                  {category}
                </div>
                <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                  {items.map((i) => (
                    <div
                      key={i.name}
                      className="rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)] p-4"
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0 flex-1">
                          <div className="text-sm font-medium">{i.name}</div>
                          <div className="mt-1 text-xs text-[var(--eaos-text-muted)]">
                            {i.description}
                          </div>
                        </div>
                        <span
                          className={`flex-shrink-0 rounded border px-2 py-0.5 text-[10px] ${statusStyle[i.status]}`}
                        >
                          {i.status === "ComingSoon" ? "coming soon" : i.status.toLowerCase()}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ))}
        </div>
      )}
    </div>
  );
}
