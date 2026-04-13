"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";

export type RegistrySkill = {
  id: string;
  name: string;
  version: string;
  npmPackage: string | null;
  bundleUrl: string | null;
  status: "active" | "disabled";
  publishedById: string | null;
  createdAt: string;
  updatedAt: string;
};

export function useSkillRegistry() {
  const [entries, setEntries] = useState<RegistrySkill[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setLoading(true);
    try {
      const data = await apiFetch<RegistrySkill[]>("/api/skill-registry");
      setEntries(data);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load skill registry");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const publish = useCallback(
    async (body: {
      name: string;
      version?: string;
      npmPackage?: string;
      bundleUrl?: string;
      manifestJson?: unknown;
    }) => {
      const result = await apiFetch<RegistrySkill>("/api/skill-registry/publish", {
        method: "POST",
        body: JSON.stringify(body),
      });
      await refresh();
      return result;
    },
    [refresh],
  );

  const updateStatus = useCallback(
    async (name: string, status: "active" | "disabled") => {
      const result = await apiFetch<RegistrySkill>(
        `/api/skill-registry/${encodeURIComponent(name)}`,
        {
          method: "PATCH",
          body: JSON.stringify({ status }),
        },
      );
      await refresh();
      return result;
    },
    [refresh],
  );

  const remove = useCallback(
    async (name: string) => {
      await apiFetch(`/api/skill-registry/${encodeURIComponent(name)}`, {
        method: "DELETE",
      });
      await refresh();
    },
    [refresh],
  );

  return { entries, loading, error, refresh, publish, updateStatus, remove };
}
