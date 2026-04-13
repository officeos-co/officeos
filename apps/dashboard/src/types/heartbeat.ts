export type TaskPriority = "high" | "medium" | "low";
export type TaskStatus = "active" | "paused" | "completed";

export type HeartbeatTask = {
  name: string | null;
  text: string;
  prompt: string | null;
  priority: TaskPriority;
  status: TaskStatus;
  interval: string | null;
  interval_secs: number | null;
  last_run_at: string | null;
  run_count: number;
};
