"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import { apiFetch } from "./useApi";

export type SystemEvent = {
  id: string;
  severity: "error" | "warning" | "info";
  category: string;
  message: string;
  skillName: string | null;
  agentId: string | null;
  correlationId: string | null;
  acknowledged: boolean;
  createdAt: string;
};

export function useSystemEvents(limit = 50) {
  const [events, setEvents] = useState<SystemEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const eventsRef = useRef(events);
  eventsRef.current = events;

  // Initial fetch
  useEffect(() => {
    apiFetch<SystemEvent[]>(`/api/system-events?limit=${limit}`)
      .then(setEvents)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [limit]);

  // SSE stream for real-time updates
  useEffect(() => {
    const es = new EventSource("/api/system-events/stream");

    es.onmessage = (e) => {
      try {
        const ev: SystemEvent = JSON.parse(e.data);
        setEvents((prev) => [ev, ...prev].slice(0, limit));
      } catch {}
    };

    es.onerror = () => {
      // EventSource auto-reconnects
    };

    return () => es.close();
  }, [limit]);

  const unacknowledgedCount = events.filter(
    (e) => !e.acknowledged && e.severity === "error"
  ).length;

  const acknowledge = useCallback(async (id: string) => {
    await apiFetch(`/api/system-events/${id}/acknowledge`, {
      method: "POST",
    });
    setEvents((prev) =>
      prev.map((e) => (e.id === id ? { ...e, acknowledged: true } : e))
    );
  }, []);

  const refresh = useCallback(async () => {
    const data = await apiFetch<SystemEvent[]>(
      `/api/system-events?limit=${limit}`
    );
    setEvents(data);
  }, [limit]);

  return { events, loading, unacknowledgedCount, acknowledge, refresh };
}
