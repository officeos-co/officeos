"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";

export type ApprovalRequest = {
  id: string;
  agentId: string;
  skillName: string;
  action: string;
  paramsJson: string;
  status: "pending" | "approved" | "rejected";
  requestedAt: string;
  decidedAt: string | null;
  resultJson: string | null;
};

let cache: ApprovalRequest[] | null = null;
const listeners = new Set<(requests: ApprovalRequest[]) => void>();

function publish(requests: ApprovalRequest[]) {
  cache = requests;
  listeners.forEach((fn) => fn(requests));
}

export function useApprovalRequests() {
  const [requests, setRequests] = useState<ApprovalRequest[]>(cache ?? []);
  const [loading, setLoading] = useState(cache === null);
  const [error, setError] = useState<string | null>(null);

  const refetch = useCallback(async () => {
    try {
      const data = await apiFetch<{ items: ApprovalRequest[]; total: number }>(
        "/api/approval-requests",
      );
      publish(data?.items ?? (Array.isArray(data) ? (data as ApprovalRequest[]) : []));
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load approval requests");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const listener = (next: ApprovalRequest[]) => setRequests(next);
    listeners.add(listener);
    if (cache === null) refetch();
    else setLoading(false);
    return () => {
      listeners.delete(listener);
    };
  }, [refetch]);

  // 10s polling with visibility guard
  useEffect(() => {
    const tick = () => {
      if (document.visibilityState === "visible") refetch();
    };
    const id = setInterval(tick, 10_000);
    return () => clearInterval(id);
  }, [refetch]);

  const approve = useCallback(
    async (id: string) => {
      await apiFetch(`/api/approval-requests/${id}/approve`, { method: "POST" });
      await refetch();
    },
    [refetch],
  );

  const reject = useCallback(
    async (id: string) => {
      await apiFetch(`/api/approval-requests/${id}/reject`, { method: "POST" });
      await refetch();
    },
    [refetch],
  );

  const pendingCount = requests.filter((r) => r.status === "pending").length;

  return { requests, pendingCount, loading, error, approve, reject, refetch };
}
