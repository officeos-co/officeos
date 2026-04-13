"use client";

import { useState } from "react";
import type { Agent } from "@/hooks/useAgents";
import { AgentChatPanel } from "./AgentChatPanel";
import { AgentMemoryPanel } from "./AgentMemoryPanel";
import { AgentSessionsPanel } from "./AgentSessionsPanel";
import { AgentLogsPanel } from "./AgentLogsPanel";
import { AgentConfigPanel } from "./AgentConfigPanel";
import { AgentOverviewPanel } from "./AgentOverviewPanel";
import { AgentCronsPanel } from "./AgentCronsPanel";
import { AgentHeartbeatPanel } from "./AgentHeartbeatPanel";
import { AgentIntegrationsPanel } from "./AgentIntegrationsPanel";

type Tab = "agent" | "chat" | "prompt" | "sessions" | "logs" | "memory" | "cron" | "heartbeat" | "channels";

type Props = {
  agent: Agent;
  onAgentUpdated?: () => void;
};

const TABS: { id: Tab; label: string }[] = [
  { id: "agent", label: "Agent" },
  { id: "chat", label: "Chat" },
  { id: "prompt", label: "Prompt" },
  { id: "sessions", label: "Sessions" },
  { id: "cron", label: "Cron" },
  { id: "heartbeat", label: "Heartbeat" },
  { id: "channels", label: "Channels" },
  { id: "logs", label: "Logs" },
  { id: "memory", label: "Memory" },
];

const ALWAYS_ON_TABS: Tab[] = ["agent", "prompt", "memory", "heartbeat", "channels"];

export function AgentDetailTabs({ agent, onAgentUpdated }: Props) {
  const [active, setActive] = useState<Tab>("agent");

  const running = agent.status === "running";

  const isEnabled = (id: Tab) => ALWAYS_ON_TABS.includes(id) || running;

  return (
    <div className="flex h-full flex-col">
      {/* Tab bar — sticky */}
      <div className="shrink-0 flex gap-1 border-b border-border bg-background px-8">
        {TABS.map((tab) => {
          const enabled = isEnabled(tab.id);
          const isActive = tab.id === active;
          return (
            <button
              key={tab.id}
              type="button"
              disabled={!enabled}
              onClick={() => setActive(tab.id)}
              title={!enabled ? "Available once the agent pod is running" : undefined}
              className={[
                "-mb-px border-b-2 px-4 py-3 text-sm transition-colors",
                isActive
                  ? "border-primary text-foreground"
                  : "border-transparent text-muted-foreground hover:text-foreground",
                !enabled
                  ? "cursor-not-allowed opacity-40 hover:text-muted-foreground"
                  : "",
              ].join(" ")}
            >
              {tab.label}
            </button>
          );
        })}
      </div>

      {/* Tab content — scrollable */}
      <div className="flex-1 overflow-y-auto">
        {active === "agent" && <AgentOverviewPanel agent={agent} onAgentUpdated={onAgentUpdated} />}
        {active === "chat" && <AgentChatPanel agent={agent} />}
        {active === "prompt" && <AgentConfigPanel agent={agent} />}
        {active === "sessions" && <AgentSessionsPanel agent={agent} />}
        {active === "cron" && <AgentCronsPanel agent={agent} />}
        {active === "heartbeat" && <AgentHeartbeatPanel agent={agent} />}
        {active === "channels" && <AgentIntegrationsPanel agent={agent} />}
        {active === "logs" && <AgentLogsPanel agent={agent} />}
        {active === "memory" && <AgentMemoryPanel agent={agent} />}
      </div>
    </div>
  );
}
