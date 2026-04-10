"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";

export type Agent = {
  id: string;
  name: string;
  provider: string;
  model: string | null;
  status: string;
  podName: string | null;
  serviceUrl: string | null;
  createdAt: string;
};

export type CreateAgentInput = {
  name: string;
  provider: string;
  model?: string;
};

let cache: Agent[] | null = null;
const listeners = new Set<(agents: Agent[]) => void>();

function publish(agents: Agent[]) {
  cache = agents;
  listeners.forEach((fn) => fn(agents));
}

const POLL_INTERVAL_MS = 10_000;

export function useAgents() {
  const [agents, setAgents] = useState<Agent[]>(cache ?? []);
  const [loading, setLoading] = useState(cache === null);
  const [error, setError] = useState<string | null>(null);

  const refetch = useCallback(async () => {
    setLoading(true);
    try {
      const data = await apiFetch<Agent[]>("/api/agents");
      publish(data);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load agents");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const listener = (next: Agent[]) => setAgents(next);
    listeners.add(listener);
    if (cache === null) {
      refetch();
    }
    return () => {
      listeners.delete(listener);
    };
  }, [refetch]);

  // Auto-poll every 10s when the tab is visible
  useEffect(() => {
    const tick = () => {
      if (document.visibilityState === "visible") {
        refetch();
      }
    };
    const id = setInterval(tick, POLL_INTERVAL_MS);
    return () => clearInterval(id);
  }, [refetch]);

  const create = useCallback(
    async (input: CreateAgentInput) => {
      const created = await apiFetch<Agent>("/api/agents", {
        method: "POST",
        body: JSON.stringify(input),
      });
      await refetch();
      return created;
    },
    [refetch],
  );

  const remove = useCallback(
    async (id: string) => {
      await apiFetch(`/api/agents/${id}`, { method: "DELETE" });
      await refetch();
    },
    [refetch],
  );

  return { agents, loading, error, refetch, create, remove };
}
