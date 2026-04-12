"use client";

import { useState } from "react";
import type { Agent } from "@/hooks/useAgents";
import { AgentChatPanel } from "./AgentChatPanel";
import { AgentMemoryPanel } from "./AgentMemoryPanel";
import { AgentSessionsPanel } from "./AgentSessionsPanel";
import { AgentLogsPanel } from "./AgentLogsPanel";
import { AgentSkillsListPanel } from "./AgentSkillsListPanel";
import { AgentConfigPanel } from "./AgentConfigPanel";

type Tab = "chat-tools" | "prompt" | "sessions" | "logs" | "memory";

type Props = {
  agent: Agent;
};

const tabs: { id: Tab; label: string }[] = [
  { id: "chat-tools", label: "Chat + Tools" },
  { id: "prompt", label: "Prompt" },
  { id: "sessions", label: "Sessions" },
  { id: "logs", label: "Logs" },
  { id: "memory", label: "Memory" },
];

export function AgentDetailTabs({ agent }: Props) {
  const [active, setActive] = useState<Tab>("chat-tools");

  const running = agent.status === "running";
  const alwaysOn: Tab[] = ["prompt", "memory"];
  const isAlwaysOn = (id: Tab) => alwaysOn.includes(id);

  return (
    <div>
      <div className="sticky top-[96px] z-0 flex gap-1 border-b border-border bg-background px-8">
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
                  ? "border-primary text-foreground"
                  : "border-transparent text-muted-foreground hover:text-foreground",
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

      {active === "chat-tools" && <ChatToolsPanel agent={agent} />}
      {active === "prompt" && <AgentConfigPanel agent={agent} />}
      {active === "sessions" && <AgentSessionsPanel agent={agent} />}
      {active === "logs" && <AgentLogsPanel agent={agent} />}
      {active === "memory" && <AgentMemoryPanel agent={agent} />}
    </div>
  );
}

function ChatToolsPanel({ agent }: { agent: Agent }) {
  return (
    <div className="mx-8 my-6 grid grid-cols-1 gap-6 lg:grid-cols-[1fr_340px]">
      <AgentChatPanel agent={agent} />
      <div className="flex flex-col gap-4">
        <div className="text-[11px] font-semibold uppercase tracking-widest text-muted-foreground/70 px-1">
          Assigned Tools
        </div>
        <AgentSkillsListPanel agent={agent} />
      </div>
    </div>
  );
}

