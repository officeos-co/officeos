"use client";

import { useCallback, useEffect, useState } from "react";
import { Plus, Trash2, Play, Pause, RefreshCw, Clock } from "lucide-react";
import type { Agent } from "@/types/agent";
import { agentFetch } from "@/lib/agentProxy";

type CronJob = {
  id: string;
  name: string | null;
  expression: string;
  command: string;
  prompt: string | null;
  job_type: string;
  enabled: boolean;
  created_at: string;
  next_run: string;
  last_run: string | null;
  last_status: string | null;
  last_output: string | null;
};

type CronRun = {
  id: number;
  job_id: string;
  started_at: string;
  finished_at: string;
  status: string;
  output: string | null;
  duration_ms: number | null;
};

type JobListResponse = CronJob[] | { jobs: CronJob[] };
type RunListResponse = CronRun[] | { runs: CronRun[] };

function unwrap<T>(data: T[] | { [k: string]: T[] }, key: string): T[] {
  if (Array.isArray(data)) return data;
  return (data as Record<string, T[]>)[key] ?? [];
}

function formatDate(iso?: string | null): string {
  if (!iso) return "—";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString();
}

export function AgentCronsPanel({ agent }: { agent: Agent }) {
  const [jobs, setJobs] = useState<CronJob[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [selected, setSelected] = useState<string | null>(null);
  const [runs, setRuns] = useState<CronRun[]>([]);
  const [loadingRuns, setLoadingRuns] = useState(false);

  const [formName, setFormName] = useState("");
  const [formSchedule, setFormSchedule] = useState("");
  const [formCommand, setFormCommand] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const load = useCallback(async () => {
    setError(null);
    try {
      const data = await agentFetch<JobListResponse>(agent.id, "/api/cron");
      setJobs(unwrap(data, "jobs"));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load cron jobs");
      setJobs([]);
    }
  }, [agent.id]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (!selected) {
      setRuns([]);
      return;
    }
    let cancelled = false;
    setLoadingRuns(true);
    agentFetch<RunListResponse>(
      agent.id,
      `/api/cron/${encodeURIComponent(selected)}/runs?limit=20`,
    )
      .then((data) => {
        if (!cancelled) setRuns(unwrap(data, "runs"));
      })
      .catch(() => {
        if (!cancelled) setRuns([]);
      })
      .finally(() => {
        if (!cancelled) setLoadingRuns(false);
      });
    return () => {
      cancelled = true;
    };
  }, [agent.id, selected]);

  const create = async () => {
    if (!formSchedule.trim() || !formCommand.trim()) return;
    setSubmitting(true);
    setError(null);
    try {
      await agentFetch(agent.id, "/api/cron", {
        method: "POST",
        body: JSON.stringify({
          name: formName.trim() || undefined,
          schedule: formSchedule.trim(),
          command: formCommand.trim(),
          enabled: true,
        }),
      });
      setFormName("");
      setFormSchedule("");
      setFormCommand("");
      setCreating(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create job");
    } finally {
      setSubmitting(false);
    }
  };

  const remove = async (id: string) => {
    if (!confirm("Delete this cron job?")) return;
    setError(null);
    try {
      await agentFetch(agent.id, `/api/cron/${encodeURIComponent(id)}`, { method: "DELETE" });
      setJobs((prev) => (prev ? prev.filter((j) => j.id !== id) : prev));
      if (selected === id) setSelected(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete job");
    }
  };

  if (jobs === null) {
    return <div className="mx-8 my-6 text-sm text-muted-foreground">Loading cron jobs…</div>;
  }

  return (
    <div className="mx-8 my-6">
      <div className="mb-4 flex items-center justify-between">
        <div className="text-sm text-muted-foreground">
          {jobs.length} job{jobs.length === 1 ? "" : "s"}
        </div>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={load}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 text-xs hover:bg-accent"
          >
            <RefreshCw className="h-3 w-3" />
            Refresh
          </button>
          <button
            type="button"
            onClick={() => setCreating((c) => !c)}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 text-xs hover:bg-primary hover:text-primary-foreground"
          >
            <Plus className="h-3 w-3" />
            New job
          </button>
        </div>
      </div>

      {error && (
        <div className="mb-3 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-2 text-xs text-destructive">
          {error}
        </div>
      )}

      {creating && (
        <div className="mb-4 rounded-xl border border-border bg-card p-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
            <input
              value={formName}
              onChange={(e) => setFormName(e.target.value)}
              placeholder="Name (optional)"
              className="rounded-md border border-border bg-accent px-3 py-2 text-sm outline-none focus:border-primary"
            />
            <input
              value={formSchedule}
              onChange={(e) => setFormSchedule(e.target.value)}
              placeholder="Schedule (cron expr)"
              className="rounded-md border border-border bg-accent px-3 py-2 font-mono text-sm outline-none focus:border-primary"
            />
            <input
              value={formCommand}
              onChange={(e) => setFormCommand(e.target.value)}
              placeholder="Command or prompt"
              className="rounded-md border border-border bg-accent px-3 py-2 text-sm outline-none focus:border-primary"
            />
          </div>
          <div className="mt-3 flex justify-end gap-2">
            <button
              type="button"
              onClick={() => setCreating(false)}
              className="rounded-md border border-border px-3 py-1.5 text-xs hover:bg-accent"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={create}
              disabled={submitting || !formSchedule.trim() || !formCommand.trim()}
              className="rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-primary-foreground disabled:opacity-50"
            >
              {submitting ? "Creating…" : "Create"}
            </button>
          </div>
        </div>
      )}

      {jobs.length === 0 ? (
        <div className="rounded-xl border border-dashed border-border bg-card px-6 py-12 text-center text-sm text-muted-foreground">
          No cron jobs scheduled.
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-[2fr_1fr]">
          <div className="overflow-hidden rounded-xl border border-border bg-card">
            <table className="w-full text-sm">
              <thead className="text-left text-muted-foreground">
                <tr className="border-b border-border">
                  <th className="px-4 py-3 font-normal">Name</th>
                  <th className="px-4 py-3 font-normal">Schedule</th>
                  <th className="px-4 py-3 font-normal">Next run</th>
                  <th className="px-4 py-3 font-normal">Last</th>
                  <th className="px-4 py-3 text-right font-normal">Actions</th>
                </tr>
              </thead>
              <tbody>
                {jobs.map((j) => {
                  const isActive = j.id === selected;
                  return (
                    <tr
                      key={j.id}
                      onClick={() => setSelected(j.id)}
                      className={`cursor-pointer border-b border-border last:border-b-0 hover:bg-muted ${
                        isActive ? "bg-accent" : ""
                      }`}
                    >
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          {j.enabled ? (
                            <Play className="h-3 w-3 text-emerald-600" />
                          ) : (
                            <Pause className="h-3 w-3 text-muted-foreground" />
                          )}
                          <span>{j.name ?? <em className="text-muted-foreground">unnamed</em>}</span>
                        </div>
                      </td>
                      <td className="px-4 py-3 font-mono text-xs text-muted-foreground">
                        {j.expression}
                      </td>
                      <td className="px-4 py-3 text-muted-foreground">
                        {formatDate(j.next_run)}
                      </td>
                      <td className="px-4 py-3 text-muted-foreground">
                        {j.last_status ?? "—"}
                      </td>
                      <td className="px-4 py-3 text-right">
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            remove(j.id);
                          }}
                          className="rounded border border-red-200 p-1 text-destructive hover:bg-red-100"
                          aria-label="Delete"
                        >
                          <Trash2 className="h-3 w-3" />
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          <div className="rounded-xl border border-border bg-card p-4">
            <div className="mb-2 flex items-center gap-2 text-[11px] uppercase tracking-wider text-muted-foreground">
              <Clock className="h-3 w-3" />
              Recent runs
            </div>
            {!selected ? (
              <div className="text-xs text-muted-foreground">Select a job to view its runs.</div>
            ) : loadingRuns ? (
              <div className="text-xs text-muted-foreground">Loading…</div>
            ) : runs.length === 0 ? (
              <div className="text-xs text-muted-foreground">No runs yet.</div>
            ) : (
              <ul className="space-y-2">
                {runs.map((r) => (
                  <li
                    key={r.id}
                    className="rounded border border-border bg-muted p-2 text-xs"
                  >
                    <div className="flex items-center justify-between">
                      <span
                        className={`rounded border px-1.5 py-0.5 text-[10px] capitalize ${
                          r.status === "ok" || r.status === "success"
                            ? "border-emerald-200 text-emerald-700"
                            : r.status === "error" || r.status === "failed"
                            ? "border-red-200 text-destructive"
                            : "border-border text-muted-foreground"
                        }`}
                      >
                        {r.status}
                      </span>
                      <span className="text-[10px] text-muted-foreground">
                        {formatDate(r.started_at)}
                      </span>
                    </div>
                    {r.output && (
                      <pre className="mt-1 max-h-24 overflow-auto whitespace-pre-wrap break-words text-[10px] text-muted-foreground">
                        {r.output}
                      </pre>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
