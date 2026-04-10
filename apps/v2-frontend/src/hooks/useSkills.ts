"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";

export type CredentialField = {
  key: string;
  label: string;
  kind: "password" | "text" | "textarea";
  required: boolean;
  placeholder: string | null;
  help: string | null;
};

export type LlmTool = {
  name: string;
  description: string;
  parameters: Record<string, unknown>;
};

export type Skill = {
  name: string;
  title: string;
  description: string;
  emoji: string;
  installed: boolean;
  configured: boolean;
  credentialFields: CredentialField[];
  llmTools: LlmTool[];
};

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

  return { skills, loading, error, refresh, install, uninstall, putCredentials };
}
