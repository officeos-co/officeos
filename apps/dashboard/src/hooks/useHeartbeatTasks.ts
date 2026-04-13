"use client";

import { useCallback, useEffect, useState } from "react";
import { agentFetch } from "@/lib/agentProxy";
import type { HeartbeatTask, TaskPriority, TaskStatus } from "@/types/heartbeat";

export type { HeartbeatTask, TaskPriority, TaskStatus } from "@/types/heartbeat";

type TasksResponse = { tasks: HeartbeatTask[] };
type AddBody = {
  name: string;
  prompt: string;
  interval?: string;
  priority?: TaskPriority;
};
type PatchBody = {
  prompt?: string;
  interval?: string;
  priority?: TaskPriority;
  status?: TaskStatus;
};

export function useHeartbeatTasks(agentId: string) {
  const [tasks, setTasks] = useState<HeartbeatTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setError(null);
    setLoading(true);
    try {
      const data = await agentFetch<TasksResponse>(agentId, "/api/heartbeat/tasks");
      setTasks(data.tasks ?? []);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load heartbeat tasks");
      setTasks([]);
    } finally {
      setLoading(false);
    }
  }, [agentId]);

  useEffect(() => {
    load();
  }, [load]);

  const addTask = useCallback(
    async (body: AddBody) => {
      await agentFetch(agentId, "/api/heartbeat/tasks", {
        method: "POST",
        body: JSON.stringify(body),
      });
      await load();
    },
    [agentId, load],
  );

  const updateTask = useCallback(
    async (name: string, body: PatchBody) => {
      await agentFetch(agentId, `/api/heartbeat/tasks/${encodeURIComponent(name)}`, {
        method: "PATCH",
        body: JSON.stringify(body),
      });
      await load();
    },
    [agentId, load],
  );

  const removeTask = useCallback(
    async (name: string) => {
      await agentFetch(agentId, `/api/heartbeat/tasks/${encodeURIComponent(name)}`, {
        method: "DELETE",
      });
      await load();
    },
    [agentId, load],
  );

  return { tasks, loading, error, reload: load, addTask, updateTask, removeTask };
}
