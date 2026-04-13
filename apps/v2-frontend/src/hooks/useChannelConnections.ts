"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";
import type { ChannelConnection, ChannelType } from "@/types/channel";

export type { ChannelConfigField, ChannelType, ChannelConnection } from "@/types/channel";

export function useChannelConnections() {
  const [connections, setConnections] = useState<ChannelConnection[]>([]);
  const [channelTypes, setChannelTypes] = useState<ChannelType[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setError(null);
    try {
      const [conns, types] = await Promise.all([
        apiFetch<ChannelConnection[]>("/api/channels"),
        apiFetch<ChannelType[]>("/api/channels/types"),
      ]);
      setConnections(conns);
      setChannelTypes(types);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load channels");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const create = useCallback(
    async (channelType: string, displayName: string, config?: Record<string, string>) => {
      const created = await apiFetch<ChannelConnection>("/api/channels", {
        method: "POST",
        body: JSON.stringify({ channelType, displayName, config }),
      });
      setConnections((prev) => [...prev, created]);
      return created;
    },
    [],
  );

  const update = useCallback(
    async (
      id: string,
      data: { displayName?: string; enabled?: boolean; config?: Record<string, string> },
    ) => {
      const updated = await apiFetch<ChannelConnection>(`/api/channels/${id}`, {
        method: "PUT",
        body: JSON.stringify(data),
      });
      setConnections((prev) => prev.map((c) => (c.id === updated.id ? updated : c)));
      return updated;
    },
    [],
  );

  const remove = useCallback(async (id: string) => {
    await apiFetch(`/api/channels/${id}`, { method: "DELETE" });
    setConnections((prev) => prev.filter((c) => c.id !== id));
  }, []);

  return { connections, channelTypes, loading, error, refresh, create, update, remove };
}
