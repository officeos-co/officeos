"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";
import type { CustomSkill } from "@/types/skill";

export type { CustomSkill } from "@/types/skill";

export function useCustomSkills() {
  const [skills, setSkills] = useState<CustomSkill[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refetch = useCallback(async () => {
    setLoading(true);
    try {
      const data = await apiFetch<CustomSkill[]>("/api/custom-skills");
      setSkills(data);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load custom skills");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    refetch();
  }, [refetch]);

  const upload = useCallback(
    async (file: File) => {
      const formData = new FormData();
      formData.append("file", file);
      const res = await fetch("/api/custom-skills/upload", {
        method: "POST",
        body: formData,
      });
      if (!res.ok) {
        const text = await res.text();
        throw new Error(`Upload failed: ${text}`);
      }
      const result = await res.json();
      await refetch();
      return result;
    },
    [refetch],
  );

  const connectGitHub = useCallback(
    async (repoUrl: string, branch?: string) => {
      const result = await apiFetch("/api/custom-skills/github", {
        method: "POST",
        body: JSON.stringify({ repoUrl, branch }),
      });
      await refetch();
      return result;
    },
    [refetch],
  );

  const sync = useCallback(
    async (id: string) => {
      await apiFetch(`/api/custom-skills/${id}/sync`, { method: "POST" });
      await refetch();
    },
    [refetch],
  );

  const remove = useCallback(
    async (id: string) => {
      await apiFetch(`/api/custom-skills/${id}`, { method: "DELETE" });
      await refetch();
    },
    [refetch],
  );

  return { skills, loading, error, refetch, upload, connectGitHub, sync, remove };
}
