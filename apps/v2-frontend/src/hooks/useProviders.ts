"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";

export type Provider = {
  id: string;
  name: string;
  displayName: string;
  configured: boolean;
  configuredAt: string | null;
};

let cache: Provider[] | null = null;
const listeners = new Set<(providers: Provider[]) => void>();

function publish(providers: Provider[]) {
  cache = providers;
  listeners.forEach((fn) => fn(providers));
}

export function useProviders() {
  const [providers, setProviders] = useState<Provider[]>(cache ?? []);
  const [loading, setLoading] = useState(cache === null);
  const [error, setError] = useState<string | null>(null);

  const refetch = useCallback(async () => {
    setLoading(true);
    try {
      const data = await apiFetch<Provider[]>("/api/providers");
      publish(data);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load providers");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const listener = (next: Provider[]) => setProviders(next);
    listeners.add(listener);
    if (cache === null) {
      refetch();
    }
    return () => {
      listeners.delete(listener);
    };
  }, [refetch]);

  const configure = useCallback(
    async (id: string, apiKey: string) => {
      await apiFetch<Provider>(`/api/providers/${id}`, {
        method: "PUT",
        body: JSON.stringify({ apiKey }),
      });
      await refetch();
    },
    [refetch],
  );

  const clear = useCallback(
    async (id: string) => {
      await apiFetch<void>(`/api/providers/${id}/key`, { method: "DELETE" });
      await refetch();
    },
    [refetch],
  );

  return { providers, loading, error, refetch, configure, clear };
}
