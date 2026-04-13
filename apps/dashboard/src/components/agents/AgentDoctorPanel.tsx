"use client";

import { useEffect, useState } from "react";
import { CheckCircle, AlertCircle, AlertTriangle, Stethoscope, RefreshCw } from "lucide-react";
import type { Agent } from "@/types/agent";
import { agentFetch } from "@/lib/agentProxy";

type DiagResult = {
  severity: "ok" | "warn" | "error";
  category: string;
  message: string;
};

type DoctorResponse = DiagResult[] | { results: DiagResult[] };

function unwrap(data: DoctorResponse): DiagResult[] {
  if (Array.isArray(data)) return data;
  return data.results ?? [];
}

export function AgentDoctorPanel({ agent }: { agent: Agent }) {
  const [results, setResults] = useState<DiagResult[] | null>(null);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const run = async () => {
    setRunning(true);
    setError(null);
    try {
      const data = await agentFetch<DoctorResponse>(agent.id, "/api/doctor", {
        method: "POST",
        body: JSON.stringify({}),
      });
      setResults(unwrap(data));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Doctor run failed");
    } finally {
      setRunning(false);
    }
  };

  useEffect(() => {
    run();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [agent.id]);

  const okCount = results?.filter((r) => r.severity === "ok").length ?? 0;
  const warnCount = results?.filter((r) => r.severity === "warn").length ?? 0;
  const errCount = results?.filter((r) => r.severity === "error").length ?? 0;

  return (
    <div className="mx-8 my-6">
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Stethoscope className="h-4 w-4" />
          <span className="text-sm text-muted-foreground">
            {results
              ? `${okCount} ok · ${warnCount} warn · ${errCount} error`
              : "Running diagnostics…"}
          </span>
        </div>
        <button
          type="button"
          onClick={run}
          disabled={running}
          className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 text-xs hover:bg-accent disabled:opacity-50"
        >
          <RefreshCw className={`h-3 w-3 ${running ? "animate-spin" : ""}`} />
          Re-run
        </button>
      </div>

      {error && (
        <div className="mb-3 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-2 text-xs text-destructive">
          {error}
        </div>
      )}

      {!results ? (
        <div className="text-sm text-muted-foreground">Loading…</div>
      ) : results.length === 0 ? (
        <div className="rounded-xl border border-dashed border-border bg-card px-6 py-12 text-center text-sm text-muted-foreground">
          No diagnostic checks returned.
        </div>
      ) : (
        <div className="space-y-2">
          {results.map((r, idx) => {
            const style =
              r.severity === "ok"
                ? "border-emerald-200 bg-emerald-50 text-emerald-100"
                : r.severity === "warn"
                ? "border-yellow-200 bg-yellow-500/5 text-yellow-100"
                : "border-red-200 bg-red-500/5 text-red-100";
            const Icon =
              r.severity === "ok" ? CheckCircle : r.severity === "warn" ? AlertTriangle : AlertCircle;
            return (
              <div
                key={`${idx}-${r.category}`}
                className={`flex items-start gap-3 rounded-md border px-4 py-3 text-sm ${style}`}
              >
                <Icon className="mt-0.5 h-4 w-4 flex-shrink-0" />
                <div className="min-w-0 flex-1">
                  <div className="text-[11px] uppercase tracking-wider opacity-70">
                    {r.category}
                  </div>
                  <div className="mt-0.5 break-words">{r.message}</div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
