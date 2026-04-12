"use client";

import { useCallback, useEffect, useState } from "react";
import {
  Plus,
  Trash2,
  Save,
  RefreshCw,
  Code,
  List,
  ChevronUp,
  ChevronDown,
} from "lucide-react";
import type { Agent } from "@/hooks/useAgents";
import { apiFetch } from "@/hooks/useApi";

type TaskPriority = "high" | "medium" | "low";
type TaskStatus = "active" | "paused" | "completed";

type HeartbeatTask = {
  text: string;
  priority: TaskPriority;
  status: TaskStatus;
};

const PRIORITY_LABELS: Record<TaskPriority, string> = {
  high: "High",
  medium: "Medium",
  low: "Low",
};

const STATUS_LABELS: Record<TaskStatus, string> = {
  active: "Active",
  paused: "Paused",
  completed: "Completed",
};

const PRIORITY_COLORS: Record<TaskPriority, string> = {
  high: "border-red-200 text-red-700 bg-red-50",
  medium: "border-yellow-200 text-yellow-700 bg-yellow-50",
  low: "border-border text-muted-foreground bg-muted",
};

const STATUS_COLORS: Record<TaskStatus, string> = {
  active: "border-emerald-200 text-emerald-700 bg-emerald-50",
  paused: "border-orange-200 text-orange-700 bg-orange-50",
  completed: "border-border text-muted-foreground bg-muted line-through",
};

function parseTasks(content: string): HeartbeatTask[] {
  return content
    .split("\n")
    .filter((line) => line.trim().startsWith("- "))
    .map((line) => {
      const text = line.trim().slice(2);
      if (!text) return null;

      const metaMatch = text.match(/^\[([^\]]+)\]\s*(.+)/);
      if (metaMatch) {
        const meta = metaMatch[1].toLowerCase();
        const taskText = metaMatch[2];
        let priority: TaskPriority = "medium";
        let status: TaskStatus = "active";
        for (const part of meta.split("|").map((s) => s.trim())) {
          if (part === "high") priority = "high";
          else if (part === "medium" || part === "med") priority = "medium";
          else if (part === "low") priority = "low";
          else if (part === "active") status = "active";
          else if (part === "paused" || part === "pause") status = "paused";
          else if (part === "completed" || part === "complete" || part === "done")
            status = "completed";
        }
        return { text: taskText, priority, status };
      }

      return { text, priority: "medium" as TaskPriority, status: "active" as TaskStatus };
    })
    .filter(Boolean) as HeartbeatTask[];
}

function serializeTasks(tasks: HeartbeatTask[]): string {
  const lines = [
    "# Periodic Tasks",
    "",
    "# Format: - [priority|status] Task description",
    "#   priority: high, medium (default), low",
    "#   status:   active (default), paused, completed",
    "",
  ];
  for (const t of tasks) {
    const meta =
      t.priority === "medium" && t.status === "active"
        ? ""
        : t.status === "active"
          ? `[${t.priority}] `
          : t.priority === "medium"
            ? `[${t.status}] `
            : `[${t.priority}|${t.status}] `;
    lines.push(`- ${meta}${t.text}`);
  }
  return lines.join("\n") + "\n";
}

const FILE_NAME = "HEARTBEAT.md";

export function AgentHeartbeatPanel({ agent }: { agent: Agent }) {
  const [tasks, setTasks] = useState<HeartbeatTask[]>([]);
  const [rawContent, setRawContent] = useState<string>("");
  const [rawMode, setRawMode] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [dirty, setDirty] = useState(false);
  const [adding, setAdding] = useState(false);
  const [newText, setNewText] = useState("");
  const [newPriority, setNewPriority] = useState<TaskPriority>("medium");
  const [exists, setExists] = useState(true);

  const load = useCallback(async () => {
    setError(null);
    setLoading(true);
    try {
      const res = await fetch(
        `/api/agents/${agent.id}/memory/${encodeURIComponent(FILE_NAME)}`,
      );
      if (res.status === 404) {
        setExists(false);
        setTasks([]);
        setRawContent("");
        setDirty(false);
        return;
      }
      if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
      const text = await res.text();
      setExists(true);
      setRawContent(text);
      setTasks(parseTasks(text));
      setDirty(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load heartbeat file");
    } finally {
      setLoading(false);
    }
  }, [agent.id]);

  useEffect(() => {
    load();
  }, [load]);

  const save = async () => {
    setSaving(true);
    setError(null);
    try {
      const content = rawMode ? rawContent : serializeTasks(tasks);
      await apiFetch(`/api/agents/${agent.id}/memory/${encodeURIComponent(FILE_NAME)}`, {
        method: "PUT",
        body: JSON.stringify({ content }),
      });
      setExists(true);
      if (!rawMode) {
        setRawContent(content);
      } else {
        setTasks(parseTasks(rawContent));
      }
      setDirty(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setSaving(false);
    }
  };

  const createFile = async () => {
    setSaving(true);
    setError(null);
    try {
      const content = serializeTasks([]);
      await apiFetch(`/api/agents/${agent.id}/memory/${encodeURIComponent(FILE_NAME)}`, {
        method: "PUT",
        body: JSON.stringify({ content }),
      });
      setExists(true);
      setRawContent(content);
      setTasks([]);
      setDirty(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create file");
    } finally {
      setSaving(false);
    }
  };

  const updateTask = (index: number, updates: Partial<HeartbeatTask>) => {
    setTasks((prev) => prev.map((t, i) => (i === index ? { ...t, ...updates } : t)));
    setDirty(true);
  };

  const removeTask = (index: number) => {
    setTasks((prev) => prev.filter((_, i) => i !== index));
    setDirty(true);
  };

  const moveTask = (index: number, direction: -1 | 1) => {
    setTasks((prev) => {
      const next = [...prev];
      const target = index + direction;
      if (target < 0 || target >= next.length) return prev;
      [next[index], next[target]] = [next[target], next[index]];
      return next;
    });
    setDirty(true);
  };

  const addTask = () => {
    if (!newText.trim()) return;
    setTasks((prev) => [
      ...prev,
      { text: newText.trim(), priority: newPriority, status: "active" },
    ]);
    setNewText("");
    setNewPriority("medium");
    setAdding(false);
    setDirty(true);
  };

  const switchToRaw = () => {
    if (!rawMode) {
      setRawContent(serializeTasks(tasks));
    } else {
      setTasks(parseTasks(rawContent));
    }
    setRawMode(!rawMode);
  };

  if (loading) {
    return <div className="mx-8 my-6 text-sm text-muted-foreground">Loading heartbeat…</div>;
  }

  if (!exists) {
    return (
      <div className="mx-8 my-6">
        {error && (
          <div className="mb-3 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-2 text-xs text-destructive">
            {error}
          </div>
        )}
        <div className="rounded-xl border border-dashed border-border bg-card px-6 py-12 text-center">
          <div className="mb-2 text-sm font-medium text-foreground">No HEARTBEAT.md found</div>
          <div className="mb-4 text-xs text-muted-foreground">
            Create a heartbeat file to define periodic tasks the agent will check on each tick.
          </div>
          <button
            type="button"
            onClick={createFile}
            disabled={saving}
            className="rounded-md bg-primary px-4 py-2 text-xs font-medium text-primary-foreground disabled:opacity-50"
          >
            {saving ? "Creating…" : "Create HEARTBEAT.md"}
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-8 my-6">
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="text-sm text-muted-foreground">
            {tasks.length} task{tasks.length === 1 ? "" : "s"}
          </div>
          {dirty && (
            <span className="rounded-full bg-yellow-100 px-2 py-0.5 text-[10px] font-medium text-yellow-700">
              Unsaved
            </span>
          )}
        </div>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={switchToRaw}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 text-xs hover:bg-accent"
            title={rawMode ? "Switch to structured view" : "Switch to raw markdown"}
          >
            {rawMode ? <List className="h-3 w-3" /> : <Code className="h-3 w-3" />}
            {rawMode ? "Structured" : "Raw"}
          </button>
          <button
            type="button"
            onClick={load}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 text-xs hover:bg-accent"
          >
            <RefreshCw className="h-3 w-3" />
            Refresh
          </button>
          {!rawMode && (
            <button
              type="button"
              onClick={() => setAdding(true)}
              className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 text-xs hover:bg-primary hover:text-primary-foreground"
            >
              <Plus className="h-3 w-3" />
              Add task
            </button>
          )}
          <button
            type="button"
            onClick={save}
            disabled={saving || !dirty}
            className="flex items-center gap-1 rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-primary-foreground disabled:opacity-50"
          >
            <Save className="h-3 w-3" />
            {saving ? "Saving…" : "Save"}
          </button>
        </div>
      </div>

      {error && (
        <div className="mb-3 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-2 text-xs text-destructive">
          {error}
        </div>
      )}

      {rawMode ? (
        <textarea
          value={rawContent}
          onChange={(e) => {
            setRawContent(e.target.value);
            setDirty(true);
          }}
          spellCheck={false}
          className="min-h-[400px] w-full resize-none rounded-xl border border-border bg-card p-4 font-mono text-xs outline-none focus:border-primary"
        />
      ) : (
        <>
          {adding && (
            <div className="mb-4 rounded-xl border border-border bg-card p-4">
              <div className="grid grid-cols-1 gap-3 md:grid-cols-[1fr_auto]">
                <input
                  value={newText}
                  onChange={(e) => setNewText(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && addTask()}
                  placeholder="Task description"
                  className="rounded-md border border-border bg-accent px-3 py-2 text-sm outline-none focus:border-primary"
                  autoFocus
                />
                <select
                  value={newPriority}
                  onChange={(e) => setNewPriority(e.target.value as TaskPriority)}
                  className="rounded-md border border-border bg-accent px-3 py-2 text-sm outline-none"
                >
                  <option value="high">High</option>
                  <option value="medium">Medium</option>
                  <option value="low">Low</option>
                </select>
              </div>
              <div className="mt-3 flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setAdding(false);
                    setNewText("");
                  }}
                  className="rounded-md border border-border px-3 py-1.5 text-xs hover:bg-accent"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={addTask}
                  disabled={!newText.trim()}
                  className="rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-primary-foreground disabled:opacity-50"
                >
                  Add
                </button>
              </div>
            </div>
          )}

          {tasks.length === 0 ? (
            <div className="rounded-xl border border-dashed border-border bg-card px-6 py-12 text-center text-sm text-muted-foreground">
              No tasks defined. Add a task for the agent to check on each heartbeat tick.
            </div>
          ) : (
            <div className="overflow-hidden rounded-xl border border-border bg-card">
              <table className="w-full text-sm">
                <thead className="text-left text-muted-foreground">
                  <tr className="border-b border-border">
                    <th className="w-8 px-2 py-3" />
                    <th className="px-4 py-3 font-normal">Task</th>
                    <th className="px-4 py-3 font-normal">Priority</th>
                    <th className="px-4 py-3 font-normal">Status</th>
                    <th className="px-4 py-3 text-right font-normal">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {tasks.map((task, i) => (
                    <tr
                      key={`${i}-${task.text}`}
                      className="border-b border-border last:border-b-0 hover:bg-muted"
                    >
                      <td className="px-2 py-3">
                        <div className="flex flex-col gap-0.5">
                          <button
                            type="button"
                            onClick={() => moveTask(i, -1)}
                            disabled={i === 0}
                            className="rounded p-0.5 text-muted-foreground hover:text-foreground disabled:opacity-20"
                          >
                            <ChevronUp className="h-3 w-3" />
                          </button>
                          <button
                            type="button"
                            onClick={() => moveTask(i, 1)}
                            disabled={i === tasks.length - 1}
                            className="rounded p-0.5 text-muted-foreground hover:text-foreground disabled:opacity-20"
                          >
                            <ChevronDown className="h-3 w-3" />
                          </button>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <input
                          value={task.text}
                          onChange={(e) => updateTask(i, { text: e.target.value })}
                          className="w-full bg-transparent text-sm outline-none"
                        />
                      </td>
                      <td className="px-4 py-3">
                        <select
                          value={task.priority}
                          onChange={(e) =>
                            updateTask(i, { priority: e.target.value as TaskPriority })
                          }
                          className={`rounded border px-2 py-1 text-[11px] font-medium ${PRIORITY_COLORS[task.priority]}`}
                        >
                          {(Object.entries(PRIORITY_LABELS) as [TaskPriority, string][]).map(
                            ([k, v]) => (
                              <option key={k} value={k}>
                                {v}
                              </option>
                            ),
                          )}
                        </select>
                      </td>
                      <td className="px-4 py-3">
                        <select
                          value={task.status}
                          onChange={(e) =>
                            updateTask(i, { status: e.target.value as TaskStatus })
                          }
                          className={`rounded border px-2 py-1 text-[11px] font-medium ${STATUS_COLORS[task.status]}`}
                        >
                          {(Object.entries(STATUS_LABELS) as [TaskStatus, string][]).map(
                            ([k, v]) => (
                              <option key={k} value={k}>
                                {v}
                              </option>
                            ),
                          )}
                        </select>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <button
                          type="button"
                          onClick={() => removeTask(i)}
                          className="rounded border border-red-200 p-1 text-destructive hover:bg-red-100"
                          aria-label="Remove task"
                        >
                          <Trash2 className="h-3 w-3" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}
    </div>
  );
}
