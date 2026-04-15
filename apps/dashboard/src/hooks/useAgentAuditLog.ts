"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";

export type AuditEntry = {
  id: string;
  agentId: string;
  skillName: string;
  action: string;
  paramsJson: string;
  resultSummary: string | null;
  durationMs: number;
  timestamp: string;
};

type AuditLogResponse = {
  entries: AuditEntry[];
  total: number;
};

const PAGE_SIZE = 20;

export function useAgentAuditLog(agentId: string) {
  const [entries, setEntries] = useState<AuditEntry[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(0);

  const refresh = useCallback(async (p: number) => {
    setLoading(true);
    setError(null);
    try {
      const data = await apiFetch<AuditLogResponse>(
        `/api/agents/${agentId}/audit-log?limit=${PAGE_SIZE}&offset=${p * PAGE_SIZE}`,
      );
      setEntries(data.entries);
      setTotal(data.total);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load audit log");
    } finally {
      setLoading(false);
    }
  }, [agentId]);

  useEffect(() => {
    refresh(page);
  }, [refresh, page]);

  return { entries, total, loading, error, page, setPage };
}
