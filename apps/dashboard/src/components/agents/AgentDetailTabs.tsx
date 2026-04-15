"use client";

import { useCallback } from "react";
import { useSearchParams, useRouter, usePathname } from "next/navigation";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import posthog from "posthog-js";
import type { Agent } from "@/types/agent";
import { AgentChatPanel } from "./AgentChatPanel";
import { AgentMemoryPanel } from "./AgentMemoryPanel";
import { AgentSessionsPanel } from "./AgentSessionsPanel";
import { AgentLogsPanel } from "./AgentLogsPanel";
import { AgentConfigPanel } from "./AgentConfigPanel";
import { AgentOverviewPanel } from "./AgentOverviewPanel";
import { AgentCronsPanel } from "./AgentCronsPanel";
import { AgentHeartbeatPanel } from "./AgentHeartbeatPanel";
import { AgentIntegrationsPanel } from "./AgentIntegrationsPanel";
import { AgentAuditPanel } from "./AgentAuditPanel";

type Tab = "agent" | "chat" | "prompt" | "sessions" | "logs" | "memory" | "cron" | "heartbeat" | "channels" | "audit";

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
  { id: "audit", label: "Audit" },
];

const ALWAYS_ON_TABS: Tab[] = ["agent", "prompt", "memory", "heartbeat", "channels", "audit"];

const VALID_TABS = new Set<string>(TABS.map((t) => t.id));

function parseTab(value: string | null): Tab {
  return value && VALID_TABS.has(value) ? (value as Tab) : "agent";
}

export function AgentDetailTabs({ agent, onAgentUpdated }: Props) {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

  const active = parseTab(searchParams.get("tab"));

  const setActive = useCallback(
    (tab: Tab) => {
      posthog.capture("tab_switched", { agent_id: agent.id, tab_name: tab });
      const params = new URLSearchParams(searchParams.toString());
      if (tab === "agent") {
        params.delete("tab");
      } else {
        params.set("tab", tab);
      }
      const qs = params.toString();
      router.replace(qs ? `${pathname}?${qs}` : pathname);
    },
    [searchParams, router, pathname, agent.id],
  );

  const running = agent.status === "running";

  const isEnabled = (id: Tab) => ALWAYS_ON_TABS.includes(id) || running;

  return (
    <Tabs value={active} onValueChange={(v) => setActive(v as Tab)} className="flex h-full flex-col">
      {/* Tab bar — sticky */}
      <div className="shrink-0 border-b border-border bg-background px-8">
        <TabsList className="h-auto gap-0 bg-transparent p-0 rounded-none">
          {TABS.map((tab) => {
            const enabled = isEnabled(tab.id);
            return (
              <TabsTrigger
                key={tab.id}
                value={tab.id}
                disabled={!enabled}
                title={!enabled ? "Available once the agent pod is running" : undefined}
                className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:shadow-none px-4 py-3 text-sm disabled:opacity-40"
              >
                {tab.label}
              </TabsTrigger>
            );
          })}
        </TabsList>
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
        {active === "audit" && <AgentAuditPanel agent={agent} />}
      </div>
    </Tabs>
  );
}
