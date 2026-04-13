"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";
import type { Skill } from "@/types/skill";

export type { CredentialField, LlmTool, Skill } from "@/types/skill";

export function useSkills() {
  const [skills, setSkills] = useState<Skill[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setError(null);
    try {
      const list = await apiFetch<Skill[]>("/api/skills");
      setSkills(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load skills");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const install = useCallback(async (name: string) => {
    const updated = await apiFetch<Skill>(
      `/api/skills/${encodeURIComponent(name)}/install`,
      { method: "POST" },
    );
    setSkills((prev) => prev.map((s) => (s.name === updated.name ? updated : s)));
    return updated;
  }, []);

  const uninstall = useCallback(async (name: string) => {
    const updated = await apiFetch<Skill>(
      `/api/skills/${encodeURIComponent(name)}/uninstall`,
      { method: "POST" },
    );
    setSkills((prev) => prev.map((s) => (s.name === updated.name ? updated : s)));
    return updated;
  }, []);

  const putCredentials = useCallback(
    async (name: string, credentials: Record<string, string>) => {
      const updated = await apiFetch<Skill>(
        `/api/skills/${encodeURIComponent(name)}/credentials`,
        {
          method: "PUT",
          body: JSON.stringify({ credentials }),
        },
      );
      setSkills((prev) => prev.map((s) => (s.name === updated.name ? updated : s)));
      return updated;
    },
    [],
  );

  const setRunTarget = useCallback(
    async (name: string, runTarget: "cloud" | "runner") => {
      const updated = await apiFetch<Skill>(
        `/api/skills/${encodeURIComponent(name)}/run-target`,
        {
          method: "PUT",
          body: JSON.stringify({ runTarget }),
        },
      );
      setSkills((prev) => prev.map((s) => (s.name === updated.name ? updated : s)));
      return updated;
    },
    [],
  );

  return { skills, loading, error, refresh, install, uninstall, putCredentials, setRunTarget };
}
