"use client";

import { useState } from "react";
import type { Agent } from "@/hooks/useAgents";
import { formatDate, shortId } from "@/utils/format";

type Tab = "overview" | "chat" | "sessions" | "memory" | "crons" | "logs";

type Props = {
  agent: Agent;
};

const tabs: { id: Tab; label: string }[] = [
  { id: "overview", label: "Overview" },
  { id: "chat", label: "Chat" },
  { id: "sessions", label: "Sessions" },
  { id: "memory", label: "Memory" },
  { id: "crons", label: "Crons" },
  { id: "logs", label: "Logs" },
];

export function AgentDetailTabs({ agent }: Props) {
  const [active, setActive] = useState<Tab>("overview");

  return (
    <div>
      <div className="sticky top-[96px] z-0 flex gap-1 border-b border-[var(--eaos-border)] bg-[var(--eaos-bg)] px-8">
        {tabs.map((t) => {
          const isActive = t.id === active;
          const disabled = t.id !== "overview" && agent.status !== "running";
          return (
            <button
              key={t.id}
              disabled={disabled}
              onClick={() => setActive(t.id)}
              className={[
                "-mb-px border-b-2 px-4 py-3 text-sm transition-colors",
                isActive
                  ? "border-white text-white"
                  : "border-transparent text-[var(--eaos-text-muted)] hover:text-white",
                disabled ? "opacity-40 cursor-not-allowed hover:text-[var(--eaos-text-muted)]" : "",
              ].join(" ")}
              title={disabled ? "Available once the agent pod is running" : undefined}
            >
              {t.label}
            </button>
          );
        })}
      </div>

      {active === "overview" && <OverviewPanel agent={agent} />}
      {active !== "overview" && <PlaceholderPanel label={active} />}
    </div>
  );
}

function OverviewPanel({ agent }: { agent: Agent }) {
  return (
    <div className="mx-8 my-6 grid grid-cols-1 gap-4 md:grid-cols-2">
      <InfoCard label="ID" value={<span className="font-mono text-xs">{shortId(agent.id)}</span>} />
      <InfoCard label="Name" value={agent.name} />
      <InfoCard label="Model" value={agent.model ?? "—"} />
      <InfoCard
        label="Status"
        value={
          <span className="rounded-full border border-[var(--eaos-border)] px-2 py-0.5 text-xs">
            {agent.status}
          </span>
        }
      />
      <InfoCard label="Created" value={formatDate(agent.createdAt)} />
    </div>
  );
}

function InfoCard({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)] px-4 py-3">
      <div className="text-[11px] uppercase tracking-wider text-[var(--eaos-text-muted)]">
        {label}
      </div>
      <div className="mt-1 text-sm">{value}</div>
    </div>
  );
}

function PlaceholderPanel({ label }: { label: string }) {
  return (
    <div className="mx-8 my-16 grid place-items-center rounded-xl border border-dashed border-[var(--eaos-border)] bg-[var(--eaos-panel)] px-6 py-20 text-center">
      <div className="text-base font-medium capitalize">{label}</div>
      <div className="mt-2 max-w-sm text-sm text-[var(--eaos-text-muted)]">
        Available once the agent pod is running. Pod provisioning is wired in the
        next stage.
      </div>
    </div>
  );
}
