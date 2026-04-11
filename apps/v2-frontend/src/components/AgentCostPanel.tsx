"use client";

import { useEffect, useState } from "react";
import { DollarSign, Activity, Cpu, RefreshCw } from "lucide-react";
import type { Agent } from "@/hooks/useAgents";
import { agentFetch } from "@/lib/agentProxy";

type ModelStats = {
  model: string;
  cost_usd: number;
  total_tokens: number;
  request_count: number;
};

type CostSummary = {
  session_cost_usd: number;
  daily_cost_usd: number;
  monthly_cost_usd: number;
  total_tokens: number;
  request_count: number;
  by_model: Record<string, ModelStats>;
};

type CostResponse = CostSummary | { cost: CostSummary };

function fmtUsd(v: number): string {
  return `$${v.toFixed(v < 1 ? 4 : 2)}`;
}

function fmtNum(v: number): string {
  return v.toLocaleString();
}

export function AgentCostPanel({ agent }: { agent: Agent }) {
  const [cost, setCost] = useState<CostSummary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await agentFetch<CostResponse>(agent.id, "/api/cost");
      const summary = "cost" in (data as object) ? (data as { cost: CostSummary }).cost : (data as CostSummary);
      setCost(summary);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load cost");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [agent.id]);

  if (loading && !cost) {
    return <div className="mx-8 my-6 text-sm text-muted-foreground">Loading cost…</div>;
  }
  if (error && !cost) {
    return (
      <div className="mx-8 my-6 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
        {error}
      </div>
    );
  }
  if (!cost) return null;

  const byModel = Object.values(cost.by_model ?? {});

  return (
    <div className="mx-8 my-6">
      <div className="mb-4 flex items-center justify-between">
        <div className="text-sm text-muted-foreground">Usage and spend for this agent</div>
        <button
          type="button"
          onClick={load}
          className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 text-xs hover:bg-accent"
        >
          <RefreshCw className="h-3 w-3" />
          Refresh
        </button>
      </div>

      <div className="mb-4 grid grid-cols-1 gap-4 md:grid-cols-3">
        <StatCard label="Session" value={fmtUsd(cost.session_cost_usd)} icon={<DollarSign className="h-4 w-4" />} />
        <StatCard label="Today" value={fmtUsd(cost.daily_cost_usd)} icon={<DollarSign className="h-4 w-4" />} />
        <StatCard label="This month" value={fmtUsd(cost.monthly_cost_usd)} icon={<DollarSign className="h-4 w-4" />} />
      </div>

      <div className="mb-4 grid grid-cols-1 gap-4 md:grid-cols-2">
        <StatCard
          label="Total tokens"
          value={fmtNum(cost.total_tokens)}
          icon={<Cpu className="h-4 w-4" />}
        />
        <StatCard
          label="Requests"
          value={fmtNum(cost.request_count)}
          icon={<Activity className="h-4 w-4" />}
        />
      </div>

      <div className="overflow-hidden rounded-xl border border-border bg-card">
        <div className="border-b border-border px-4 py-3 text-[11px] uppercase tracking-wider text-muted-foreground">
          Breakdown by model
        </div>
        {byModel.length === 0 ? (
          <div className="px-4 py-8 text-center text-sm text-muted-foreground">
            No usage recorded yet.
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="text-left text-muted-foreground">
              <tr className="border-b border-border">
                <th className="px-4 py-3 font-normal">Model</th>
                <th className="px-4 py-3 font-normal">Requests</th>
                <th className="px-4 py-3 font-normal">Tokens</th>
                <th className="px-4 py-3 font-normal">Cost</th>
              </tr>
            </thead>
            <tbody>
              {byModel.map((m) => (
                <tr
                  key={m.model}
                  className="border-b border-border last:border-b-0"
                >
                  <td className="px-4 py-3 font-mono text-xs">{m.model}</td>
                  <td className="px-4 py-3 text-muted-foreground">{fmtNum(m.request_count)}</td>
                  <td className="px-4 py-3 text-muted-foreground">{fmtNum(m.total_tokens)}</td>
                  <td className="px-4 py-3">{fmtUsd(m.cost_usd)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

function StatCard({
  label,
  value,
  icon,
}: {
  label: string;
  value: string;
  icon: React.ReactNode;
}) {
  return (
    <div className="rounded-xl border border-border bg-card p-4">
      <div className="flex items-center gap-2 text-[11px] uppercase tracking-wider text-muted-foreground">
        {icon}
        {label}
      </div>
      <div className="mt-2 text-2xl font-semibold">{value}</div>
    </div>
  );
}
