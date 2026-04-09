"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";

export type Agent = {
  id: string;
  name: string;
  model: string | null;
  status: string;
  createdAt: string;
};

export type CreateAgentInput = {
  name: string;
  model?: string;
};

let cache: Agent[] | null = null;
const listeners = new Set<(agents: Agent[]) => void>();

function publish(agents: Agent[]) {
  cache = agents;
  listeners.forEach((fn) => fn(agents));
}

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

  return { agents, loading, error, refetch, create };
}
