"use client";

import { useState } from "react";
import type { Agent } from "@/hooks/useAgents";
import { AgentChatPanel } from "./AgentChatPanel";
import { AgentMemoryPanel } from "./AgentMemoryPanel";
import { AgentSessionsPanel } from "./AgentSessionsPanel";
import { AgentLogsPanel } from "./AgentLogsPanel";
import { AgentConfigPanel } from "./AgentConfigPanel";
import { AgentOverviewPanel } from "./AgentOverviewPanel";

type Tab = "agent" | "chat" | "prompt" | "sessions" | "logs" | "memory";

type Props = {
  agent: Agent;
};

const TABS: { id: Tab; label: string }[] = [
  { id: "agent", label: "Agent" },
  { id: "chat", label: "Chat" },
  { id: "prompt", label: "Prompt" },
  { id: "sessions", label: "Sessions" },
  { id: "logs", label: "Logs" },
  { id: "memory", label: "Memory" },
];

const ALWAYS_ON_TABS: Tab[] = ["agent", "prompt", "memory"];

export function AgentDetailTabs({ agent }: Props) {
  const [active, setActive] = useState<Tab>("agent");

  const running = agent.status === "running";

  const isEnabled = (id: Tab) => ALWAYS_ON_TABS.includes(id) || running;

  return (
    <div>
      <div className="sticky top-0 z-10 flex gap-1 border-b border-border bg-background px-8">
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

      {active === "agent" && <AgentOverviewPanel agent={agent} />}
      {active === "chat" && <AgentChatPanel agent={agent} />}
      {active === "prompt" && <AgentConfigPanel agent={agent} />}
      {active === "sessions" && <AgentSessionsPanel agent={agent} />}
      {active === "logs" && <AgentLogsPanel agent={agent} />}
      {active === "memory" && <AgentMemoryPanel agent={agent} />}
    </div>
  );
}
