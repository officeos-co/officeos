"use client";

import { useState } from "react";
import type { Agent } from "@/hooks/useAgents";
import { AgentChatPanel } from "./AgentChatPanel";
import { AgentMemoryPanel } from "./AgentMemoryPanel";
import { AgentSessionsPanel } from "./AgentSessionsPanel";
import { AgentLogsPanel } from "./AgentLogsPanel";
import { AgentCronsPanel } from "./AgentCronsPanel";
import { AgentCostPanel } from "./AgentCostPanel";
import { AgentDoctorPanel } from "./AgentDoctorPanel";
import { AgentToolsPanel } from "./AgentToolsPanel";
import { AgentSkillsListPanel } from "./AgentSkillsListPanel";
import { AgentConfigPanel } from "./AgentConfigPanel";
import { StatusBadge } from "./StatusBadge";
import { formatDate, shortId } from "@/utils/format";

type Tab =
  | "overview"
  | "chat"
  | "sessions"
  | "memory"
  | "crons"
  | "cost"
  | "tools"
  | "skills"
  | "doctor"
  | "config"
  | "logs";

type Props = {
  agent: Agent;
};

const tabs: { id: Tab; label: string }[] = [
  { id: "overview", label: "Overview" },
  { id: "chat", label: "Chat" },
  { id: "sessions", label: "Sessions" },
  { id: "memory", label: "Memory" },
  { id: "crons", label: "Crons" },
  { id: "cost", label: "Cost" },
  { id: "tools", label: "Tools" },
  { id: "skills", label: "Skills" },
  { id: "doctor", label: "Doctor" },
  { id: "config", label: "Config" },
  { id: "logs", label: "Logs" },
];

export function AgentDetailTabs({ agent }: Props) {
  const [active, setActive] = useState<Tab>("overview");

  const running = agent.status === "running";
  const alwaysOn: Tab[] = ["overview", "memory", "skills"];
  const isAlwaysOn = (id: Tab) => alwaysOn.includes(id);

  return (
    <div>
      <div className="sticky top-[96px] z-0 flex flex-wrap gap-1 border-b border-border bg-background px-8">
        {tabs.map((t) => {
          const isActive = t.id === active;
          const disabled = !isAlwaysOn(t.id) && !running;
          return (
            <button
              key={t.id}
              disabled={disabled}
              onClick={() => setActive(t.id)}
              className={[
                "-mb-px border-b-2 px-4 py-3 text-sm transition-colors",
                isActive
                  ? "border-white text-white"
                  : "border-transparent text-muted-foreground hover:text-white",
                disabled
                  ? "cursor-not-allowed opacity-40 hover:text-muted-foreground"
                  : "",
              ].join(" ")}
              title={disabled ? "Available once the agent pod is running" : undefined}
            >
              {t.label}
            </button>
          );
        })}
      </div>

      {active === "overview" && <OverviewPanel agent={agent} />}
      {active === "chat" && <AgentChatPanel agent={agent} />}
      {active === "sessions" && <AgentSessionsPanel agent={agent} />}
      {active === "memory" && <AgentMemoryPanel agent={agent} />}
      {active === "crons" && <AgentCronsPanel agent={agent} />}
      {active === "cost" && <AgentCostPanel agent={agent} />}
      {active === "tools" && <AgentToolsPanel agent={agent} />}
      {active === "skills" && <AgentSkillsListPanel agent={agent} />}
      {active === "doctor" && <AgentDoctorPanel agent={agent} />}
      {active === "config" && <AgentConfigPanel agent={agent} />}
      {active === "logs" && <AgentLogsPanel agent={agent} />}
    </div>
  );
}

function OverviewPanel({ agent }: { agent: Agent }) {
  return (
    <div className="mx-8 my-6 grid grid-cols-1 gap-4 md:grid-cols-2">
      <InfoCard label="ID" value={<span className="font-mono text-xs">{shortId(agent.id)}</span>} />
      <InfoCard label="Name" value={agent.name} />
      <InfoCard label="Provider" value={agent.provider} />
      <InfoCard label="Model" value={agent.model ?? "—"} />
      <InfoCard
        label="Status"
        value={<StatusBadge status={agent.status} />}
      />
      <InfoCard label="Created" value={formatDate(agent.createdAt)} />
    </div>
  );
}

function InfoCard({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-border bg-card px-4 py-3">
      <div className="text-[11px] uppercase tracking-wider text-muted-foreground">
        {label}
      </div>
      <div className="mt-1 text-sm">{value}</div>
    </div>
  );
}
