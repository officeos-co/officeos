"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";
import type { Runner, CreateRunnerResult } from "@/types/runner";

export type { Runner, CreateRunnerResult } from "@/types/runner";

let cache: Runner[] | null = null;
const listeners = new Set<(runners: Runner[]) => void>();

function publish(runners: Runner[]) {
  cache = runners;
  listeners.forEach((fn) => fn(runners));
}

const POLL_INTERVAL_MS = 10_000;

export function useRunners() {
  const [runners, setRunners] = useState<Runner[]>(cache ?? []);
  const [loading, setLoading] = useState(cache === null);
  const [error, setError] = useState<string | null>(null);

  const refetch = useCallback(async () => {
    setLoading(true);
    try {
      const data = await apiFetch<Runner[]>("/api/runners");
      publish(data);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load runners");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const listener = (next: Runner[]) => setRunners(next);
    listeners.add(listener);
    if (cache === null) {
      refetch();
    }
    return () => {
      listeners.delete(listener);
    };
  }, [refetch]);

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
    async (name: string): Promise<CreateRunnerResult> => {
      const result = await apiFetch<CreateRunnerResult>("/api/runners", {
        method: "POST",
        body: JSON.stringify({ name }),
      });
      await refetch();
      return result;
    },
    [refetch],
  );

  const remove = useCallback(
    async (id: string) => {
      await apiFetch(`/api/runners/${id}`, { method: "DELETE" });
      await refetch();
    },
    [refetch],
  );

  return { runners, loading, error, refetch, create, remove };
}
