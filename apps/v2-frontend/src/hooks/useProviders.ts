"use client";

import { useEffect, useState } from "react";
import { apiFetch } from "./useApi";

export type Provider = {
  id: string;
  name: string;
  displayName: string;
  configured: boolean;
};

export function useProviders() {
  const [providers, setProviders] = useState<Provider[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    apiFetch<Provider[]>("/api/providers")
      .then((data) => {
        if (!cancelled) setProviders(data);
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Failed to load providers");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  return { providers, loading, error };
}
