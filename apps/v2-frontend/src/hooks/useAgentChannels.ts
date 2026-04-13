"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";
import type { AgentChannelBinding, AgentChannelConfig } from "@/types/channel";

export type { AgentChannelBinding, AgentChannelConfig } from "@/types/channel";

export function useAgentChannels(agentId: string) {
  const [bindings, setBindings] = useState<AgentChannelBinding[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setError(null);
    try {
      const list = await apiFetch<AgentChannelBinding[]>(
        `/api/agents/${agentId}/channels`,
      );
      setBindings(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load agent channels");
    } finally {
      setLoading(false);
    }
  }, [agentId]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const bind = useCallback(
    async (channelConnectionId: string, config?: AgentChannelConfig) => {
      const created = await apiFetch<AgentChannelBinding>(
        `/api/agents/${agentId}/channels`,
        {
          method: "POST",
          body: JSON.stringify({ channelConnectionId, config }),
        },
      );
      setBindings((prev) => [...prev, created]);
      return created;
    },
    [agentId],
  );

  const update = useCallback(
    async (bindingId: string, data: { enabled?: boolean; config?: AgentChannelConfig }) => {
      const updated = await apiFetch<AgentChannelBinding>(
        `/api/agents/${agentId}/channels/${bindingId}`,
        {
          method: "PUT",
          body: JSON.stringify(data),
        },
      );
      setBindings((prev) => prev.map((b) => (b.id === updated.id ? updated : b)));
      return updated;
    },
    [agentId],
  );

  const unbind = useCallback(
    async (bindingId: string) => {
      await apiFetch(`/api/agents/${agentId}/channels/${bindingId}`, {
        method: "DELETE",
      });
      setBindings((prev) => prev.filter((b) => b.id !== bindingId));
    },
    [agentId],
  );

  return { bindings, loading, error, refresh, bind, update, unbind };
}
