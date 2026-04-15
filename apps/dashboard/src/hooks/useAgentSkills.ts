"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";
import type { AgentSkillAssignment } from "@/types/agent";

export type { AgentSkillAssignment } from "@/types/agent";

export function useAgentSkills(agentId: string) {
  const [assignments, setAssignments] = useState<AgentSkillAssignment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setError(null);
    try {
      const data = await apiFetch<AgentSkillAssignment[]>(
        `/api/agents/${agentId}/skills`,
      );
      setAssignments(data ?? []);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load agent skills");
    } finally {
      setLoading(false);
    }
  }, [agentId]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const assign = useCallback(
    async (skillNames: string[]) => {
      const data = await apiFetch<AgentSkillAssignment[]>(
        `/api/agents/${agentId}/skills`,
        {
          method: "POST",
          body: JSON.stringify({ skillNames }),
        },
      );
      setAssignments(data ?? []);
      return data ?? [];
    },
    [agentId],
  );

  const remove = useCallback(
    async (skillName: string) => {
      await apiFetch(`/api/agents/${agentId}/skills/${encodeURIComponent(skillName)}`, {
        method: "DELETE",
      });
      setAssignments((prev) => prev.filter((a) => a.skillName !== skillName));
    },
    [agentId],
  );

  return { assignments, loading, error, refresh, assign, remove };
}
