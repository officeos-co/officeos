"use client";

import { useState } from "react";
import {
  Plus,
  Trash2,
  RefreshCw,
  Play,
  Pause,
  CheckCircle2,
} from "lucide-react";
import type { Agent } from "@/hooks/useAgents";
import {
  useHeartbeatTasks,
  type TaskPriority,
  type TaskStatus,
  type HeartbeatTask,
} from "@/hooks/useHeartbeatTasks";

const PRIORITY_LABELS: Record<TaskPriority, string> = {
  high: "High",
  medium: "Medium",
  low: "Low",
};

const PRIORITY_COLORS: Record<TaskPriority, string> = {
  high: "border-red-200 text-red-700 bg-red-50",
  medium: "border-yellow-200 text-yellow-700 bg-yellow-50",
  low: "border-border text-muted-foreground bg-muted",
};

const STATUS_COLORS: Record<TaskStatus, string> = {
  active: "border-emerald-200 text-emerald-700 bg-emerald-50",
  paused: "border-orange-200 text-orange-700 bg-orange-50",
  completed: "border-border text-muted-foreground bg-muted",
};

function StatusIcon({ status }: { status: TaskStatus }) {
  switch (status) {
    case "active":
      return <Play className="h-3 w-3 text-emerald-600" />;
    case "paused":
      return <Pause className="h-3 w-3 text-orange-600" />;
    case "completed":
      return <CheckCircle2 className="h-3 w-3 text-muted-foreground" />;
  }
}

function formatDate(iso?: string | null): string {
  if (!iso) return "never";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString();
}

export function AgentHeartbeatPanel({ agent }: { agent: Agent }) {
  const { tasks, loading, error, reload, addTask, updateTask, removeTask } =
    useHeartbeatTasks(agent.id);

  const [adding, setAdding] = useState(false);
  const [formName, setFormName] = useState("");
  const [formPrompt, setFormPrompt] = useState("");
  const [formInterval, setFormInterval] = useState("");
  const [formPriority, setFormPriority] = useState<TaskPriority>("medium");
  const [submitting, setSubmitting] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  // Inline editing state
  const [editingTask, setEditingTask] = useState<string | null>(null);
  const [editPrompt, setEditPrompt] = useState("");
  const [editInterval, setEditInterval] = useState("");
  const [editPriority, setEditPriority] = useState<TaskPriority>("medium");

  const handleAdd = async () => {
    if (!formName.trim() || !formPrompt.trim()) return;
    setSubmitting(true);
    setActionError(null);
    try {
      await addTask({
        name: formName.trim(),
        prompt: formPrompt.trim(),
        interval: formInterval.trim() || undefined,
        priority: formPriority,
      });
      setFormName("");
      setFormPrompt("");
      setFormInterval("");
      setFormPriority("medium");
      setAdding(false);
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Failed to add task");
    } finally {
      setSubmitting(false);
    }
  };

  const handleToggleStatus = async (task: HeartbeatTask) => {
    const taskName = task.name ?? task.text;
    const newStatus: TaskStatus = task.status === "active" ? "paused" : "active";
    setActionError(null);
    try {
      await updateTask(taskName, { status: newStatus });
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Failed to update task");
    }
  };

  const handleRemove = async (task: HeartbeatTask) => {
    const taskName = task.name ?? task.text;
    if (!confirm(`Delete heartbeat task "${taskName}"?`)) return;
    setActionError(null);
    try {
      await removeTask(taskName);
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Failed to remove task");
    }
  };

  const startEditing = (task: HeartbeatTask) => {
    const taskName = task.name ?? task.text;
    setEditingTask(taskName);
    setEditPrompt(task.prompt ?? task.text);
    setEditInterval(task.interval ?? "");
    setEditPriority(task.priority);
  };

  const handleSaveEdit = async () => {
    if (!editingTask) return;
    setActionError(null);
    try {
      await updateTask(editingTask, {
        prompt: editPrompt.trim() || undefined,
        interval: editInterval.trim() || undefined,
        priority: editPriority,
      });
      setEditingTask(null);
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Failed to update task");
    }
  };

  if (loading) {
    return (
      <div className="mx-8 my-6 text-sm text-muted-foreground">
        Loading heartbeat tasks...
      </div>
    );
  }

  return (
    <div className="mx-8 my-6">
      <div className="mb-4 flex items-center justify-between">
        <div className="text-sm text-muted-foreground">
          {tasks.length} task{tasks.length === 1 ? "" : "s"}
        </div>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={reload}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 text-xs hover:bg-accent"
          >
            <RefreshCw className="h-3 w-3" />
            Refresh
          </button>
          <button
            type="button"
            onClick={() => setAdding((a) => !a)}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 text-xs hover:bg-primary hover:text-primary-foreground"
          >
            <Plus className="h-3 w-3" />
            Add task
          </button>
        </div>
      </div>

      {(error || actionError) && (
        <div className="mb-3 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-2 text-xs text-destructive">
          {error || actionError}
        </div>
      )}

      {adding && (
        <div className="mb-4 rounded-xl border border-border bg-card p-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <input
              value={formName}
              onChange={(e) => setFormName(e.target.value)}
              placeholder="Task name (e.g. inbox-triage)"
              className="rounded-md border border-border bg-accent px-3 py-2 text-sm outline-none focus:border-primary"
              autoFocus
            />
            <div className="flex gap-2">
              <input
                value={formInterval}
                onChange={(e) => setFormInterval(e.target.value)}
                placeholder="Interval (e.g. 30m, 6h)"
                className="w-1/2 rounded-md border border-border bg-accent px-3 py-2 font-mono text-sm outline-none focus:border-primary"
              />
              <select
                value={formPriority}
                onChange={(e) => setFormPriority(e.target.value as TaskPriority)}
                className="w-1/2 rounded-md border border-border bg-accent px-3 py-2 text-sm outline-none"
              >
                <option value="high">High</option>
                <option value="medium">Medium</option>
                <option value="low">Low</option>
              </select>
            </div>
          </div>
          <textarea
            value={formPrompt}
            onChange={(e) => setFormPrompt(e.target.value)}
            placeholder="Task prompt / instruction"
            rows={3}
            className="mt-3 w-full rounded-md border border-border bg-accent px-3 py-2 text-sm outline-none focus:border-primary"
          />
          <div className="mt-3 flex justify-end gap-2">
            <button
              type="button"
              onClick={() => setAdding(false)}
              className="rounded-md border border-border px-3 py-1.5 text-xs hover:bg-accent"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={handleAdd}
              disabled={submitting || !formName.trim() || !formPrompt.trim()}
              className="rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-primary-foreground disabled:opacity-50"
            >
              {submitting ? "Adding..." : "Add"}
            </button>
          </div>
        </div>
      )}

      {tasks.length === 0 ? (
        <div className="rounded-xl border border-dashed border-border bg-card px-6 py-12 text-center text-sm text-muted-foreground">
          No heartbeat tasks defined. Add a task for the agent to check periodically.
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-3">
          {tasks.map((task) => {
            const taskName = task.name ?? task.text;
            const isEditing = editingTask === taskName;

            return (
              <div
                key={taskName}
                className="rounded-xl border border-border bg-card p-4"
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-3 min-w-0 flex-1">
                    <button
                      type="button"
                      onClick={() => handleToggleStatus(task)}
                      className="mt-0.5 flex-shrink-0"
                      title={
                        task.status === "active" ? "Pause task" : "Activate task"
                      }
                    >
                      <StatusIcon status={task.status} />
                    </button>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium text-foreground truncate">
                          {task.name ?? "(unnamed)"}
                        </span>
                        <span
                          className={`rounded border px-1.5 py-0.5 text-[10px] font-medium ${PRIORITY_COLORS[task.priority]}`}
                        >
                          {PRIORITY_LABELS[task.priority]}
                        </span>
                        <span
                          className={`rounded border px-1.5 py-0.5 text-[10px] font-medium ${STATUS_COLORS[task.status]}`}
                        >
                          {task.status}
                        </span>
                      </div>
                      {isEditing ? (
                        <div className="mt-2 space-y-2">
                          <textarea
                            value={editPrompt}
                            onChange={(e) => setEditPrompt(e.target.value)}
                            rows={2}
                            className="w-full rounded-md border border-border bg-accent px-3 py-2 text-xs outline-none focus:border-primary"
                          />
                          <div className="flex gap-2">
                            <input
                              value={editInterval}
                              onChange={(e) => setEditInterval(e.target.value)}
                              placeholder="Interval (e.g. 30m)"
                              className="w-32 rounded-md border border-border bg-accent px-2 py-1 font-mono text-xs outline-none focus:border-primary"
                            />
                            <select
                              value={editPriority}
                              onChange={(e) =>
                                setEditPriority(e.target.value as TaskPriority)
                              }
                              className="rounded-md border border-border bg-accent px-2 py-1 text-xs outline-none"
                            >
                              <option value="high">High</option>
                              <option value="medium">Medium</option>
                              <option value="low">Low</option>
                            </select>
                            <button
                              type="button"
                              onClick={handleSaveEdit}
                              className="rounded-md bg-primary px-3 py-1 text-xs font-medium text-primary-foreground"
                            >
                              Save
                            </button>
                            <button
                              type="button"
                              onClick={() => setEditingTask(null)}
                              className="rounded-md border border-border px-3 py-1 text-xs hover:bg-accent"
                            >
                              Cancel
                            </button>
                          </div>
                        </div>
                      ) : (
                        <>
                          <p
                            className="mt-1 text-xs text-muted-foreground cursor-pointer hover:text-foreground"
                            onClick={() => startEditing(task)}
                            title="Click to edit"
                          >
                            {task.prompt ?? task.text}
                          </p>
                          <div className="mt-2 flex items-center gap-4 text-[10px] text-muted-foreground">
                            {task.interval && (
                              <span>
                                Interval: <span className="font-mono">{task.interval}</span>
                              </span>
                            )}
                            <span>Last run: {formatDate(task.last_run_at)}</span>
                            <span>Runs: {task.run_count}</span>
                          </div>
                        </>
                      )}
                    </div>
                  </div>
                  <div className="flex gap-1 flex-shrink-0">
                    <button
                      type="button"
                      onClick={() => handleRemove(task)}
                      className="rounded border border-red-200 p-1 text-destructive hover:bg-red-100"
                      aria-label="Delete"
                    >
                      <Trash2 className="h-3 w-3" />
                    </button>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
