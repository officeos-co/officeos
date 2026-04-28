"use client";

import { use, useState, useEffect } from "react";
import Link from "next/link";
import { useSearchParams, useRouter } from "next/navigation";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { StatusBadge } from "@/features/agents/components/status-badge";
import { AgentIntegrationsTab } from "@/features/agents/components/agent-integrations-tab";
import { AgentLogsTab } from "@/features/agents/components/agent-logs-tab";
import { AgentMemoryTab } from "@/features/agents/components/agent-memory-tab";
import { AgentCronTab } from "@/features/agents/components/agent-cron-tab";
import { useAgent } from "@/features/agents";
import { useModels } from "@/features/agents";
import { useSendAgentMessage } from "@/features/agents";
import { useCreateSession } from "@/features/agents";
import { SendIcon, PlusIcon } from "lucide-react";

/* ── Tabs (URL-driven) ───────────────────────────────────── */

const TABS = [
  { key: "integrations", label: "Integrations" },
  { key: "logs", label: "Logs" },
  { key: "memory", label: "Memory" },
  { key: "cron", label: "Cron" },
] as const;
type TabKey = (typeof TABS)[number]["key"];

/* ── Helpers ─────────────────────────────────────────────── */

function humanAgo(ts: number | string | null | undefined): string {
  if (!ts) return "";
  const then = typeof ts === "number" ? ts : Date.parse(ts);
  if (Number.isNaN(then)) return "";
  const diffMs = Date.now() - then;
  const m = Math.floor(diffMs / 60000);
  if (m < 1) return "just now";
  if (m < 60) return `${m} minute${m === 1 ? "" : "s"} ago`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h} hour${h === 1 ? "" : "s"} ago`;
  const d = Math.floor(h / 24);
  return `${d} day${d === 1 ? "" : "s"} ago`;
}

/* ── Skeleton loading state ───────────────────────────────── */

function AgentSkeleton() {
  return (
    <>
      <div className="sticky top-0 z-10 bg-background border-b border-border">
        <div className="max-w-6xl mx-auto w-full px-4">
          <div className="flex items-start justify-between py-4">
            <div className="space-y-2">
              <div className="flex items-center gap-2.5">
                <Skeleton className="h-6 w-48" />
                <Skeleton className="h-6 w-20 rounded-full" />
              </div>
              <Skeleton className="h-3 w-72" />
            </div>
            <Skeleton className="h-8 w-24 rounded-md" />
          </div>
          <div className="flex gap-1 -mb-px">
            <Skeleton className="h-8 w-16" />
            <Skeleton className="h-8 w-12" />
            <Skeleton className="h-8 w-18" />
            <Skeleton className="h-8 w-12" />
          </div>
        </div>
      </div>
      <div className="flex flex-1 flex-col px-4 max-w-6xl mx-auto w-full pt-6 space-y-4">
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-32 w-full rounded-xl" />
        <Skeleton className="h-10 w-full" />
      </div>
    </>
  );
}

/* ── Page ─────────────────────────────────────────────────── */

export default function AgentDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const searchParams = useSearchParams();
  const router = useRouter();
  const initialStatus = searchParams.get("status");
  const { agent, loading: agentLoading } = useAgent(id);
  const { models } = useModels();
  const [agentStatus, setAgentStatus] = useState(
    initialStatus === "booting" ? "booting" : "",
  );
  const [model, setModel] = useState("");
  const [message, setMessage] = useState("");
  const { sendAgentMessage } = useSendAgentMessage();
  const createSession = useCreateSession();
  const activeSession = agent?.activeSession ?? null;
  const tab = (searchParams.get("tab") as TabKey) ?? "integrations";

  // Sync state from loaded agent
  useEffect(() => {
    if (agent) {
      if (!agentStatus || agentStatus === "") setAgentStatus(agent.status);
      if (!model) setModel(agent.model);
    }
  }, [agent, agentStatus, model]);

  // Simulate boot → running transition
  useEffect(() => {
    if (agentStatus === "booting") {
      const t = setTimeout(() => {
        setAgentStatus("running");
        router.replace(`/agents/${id}?tab=integrations`);
      }, 5000);
      return () => clearTimeout(t);
    }
  }, [agentStatus, id, router]);

  if (agentStatus === "booting" || (agentLoading && !agent)) {
    return (
      <>
        <PageHeader group="Agents" page="Loading..." />
        <AgentSkeleton />
      </>
    );
  }

  const displayName = agent?.name ?? "Unnamed Agent";
  const displayId = agent?.id ?? id;
  const displayStatus = agentStatus || agent?.status || "stopped";
  const createdAt = agent?.createdAt ?? null;

  const submit = () => {
    if (!message.trim()) return;
    const content = message;
    setMessage("");
    // Fire-and-forget — the optimistic log entry appears immediately
    sendAgentMessage(id, content);
  };

  return (
    <div className="flex flex-col min-h-screen">
      <PageHeader group="Agents" page={displayName} />

      {/* Sticky agent header + tabs */}
      <div className="sticky top-0 z-10 bg-background border-b border-border">
        <div className="max-w-6xl mx-auto w-full px-4">
          <div className="flex items-start justify-between py-4">
            <div>
              <div className="flex items-center gap-2.5">
                <h1 className="text-lg font-semibold">{displayName}</h1>
                <StatusBadge status={displayStatus} />
              </div>
              <div className="mt-1 text-xs text-muted-foreground">
                <span className="font-mono">{displayId}</span>
                {createdAt && (
                  <>
                    <span className="mx-1.5">·</span>
                    <span>Created {humanAgo(createdAt)}</span>
                  </>
                )}
              </div>
            </div>
            <div className="flex items-center gap-2 shrink-0">
              <Select
                value={model}
                onValueChange={(v) => {
                  if (v) setModel(v);
                }}
              >
                <SelectTrigger className="w-[180px] h-8 text-xs">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {models.map((m) => (
                    <SelectItem key={m.id} value={m.id}>
                      {m.displayName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <Button
                variant="outline"
                size="sm"
                onClick={() => createSession(id)}
              >
                <PlusIcon className="size-3.5" />
                New Session
              </Button>
              <Button
                variant="outline"
                size="sm"
                nativeButton={false}
                render={<Link href="/agents" />}
              >
                All agents
              </Button>
            </div>
          </div>

          <div className="flex -mb-px">
            {TABS.map((t) => (
              <Link
                key={t.key}
                href={`/agents/${id}?tab=${t.key}`}
                className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
                  tab === t.key
                    ? "border-foreground text-foreground"
                    : "border-transparent text-muted-foreground hover:text-foreground"
                }`}
              >
                {t.label}
              </Link>
            ))}
          </div>
        </div>
      </div>

      <div className="flex flex-1 flex-col px-4 max-w-6xl mx-auto w-full pb-20">
        <div className="flex-1 flex flex-col">
          {tab === "integrations" && <AgentIntegrationsTab agentId={id} />}
          {tab === "logs" && <AgentLogsTab agentId={id} />}
          {tab === "memory" && <AgentMemoryTab agentId={id} />}
          {tab === "cron" && <AgentCronTab agentId={id} />}
        </div>
      </div>

      {/* Sticky chat bar — only on logs tab */}
      {tab === "logs" && <div className="sticky bottom-0 z-10 border-t border-border bg-background/80 backdrop-blur-sm p-3 mt-auto">
        {activeSession && (
          <div className="flex items-center justify-center gap-2 mb-1.5 text-[10px] text-muted-foreground">
            <span className="inline-block size-1.5 rounded-full bg-emerald-500" />
            Session · {activeSession.messageCount} messages
          </div>
        )}
        <div className="flex items-center gap-2 max-w-3xl mx-auto">
          <Input
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            placeholder="Send a message to the agent..."
            className="flex-1"
            onKeyDown={(e) => {
              if (e.key === "Enter" && message.trim()) submit();
            }}
          />
          <Button size="icon" disabled={!message.trim()} onClick={submit}>
            <SendIcon className="size-4" />
          </Button>
        </div>
      </div>}
    </div>
  );
}
