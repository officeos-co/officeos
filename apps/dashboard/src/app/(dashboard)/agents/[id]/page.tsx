"use client";

import { use, useState, useEffect } from "react";
import { isDevelopment } from "@/lib/env";
import Link from "next/link";
import { useSearchParams, useRouter } from "next/navigation";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { HelpTooltip, WithTooltip } from "@/components/ui/help-tooltip";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { StatusBadge } from "@/components/ui/status-badge";
import { AgentIntegrationsTab } from "@/features/agents/components/agent-integrations-tab";
import { AgentLogsTab } from "@/features/agents/components/agent-logs-tab";
import { useAgent } from "@/features/agents";
import { useModels } from "@/features/agents";
import { useSendAgentMessage } from "@/features/agents";
import { useCreateSession } from "@/features/agents";
import { useUpdateAgent } from "@/features/agents";
import { getModelTooltip } from "@/features/agents/model-tooltips";
import { SendIcon, PlusIcon } from "lucide-react";

/* ── Tabs (URL-driven) ───────────────────────────────────── */

const TABS = [
  { key: "logs", label: "Logs" },
  { key: "permissions", label: "Permissions" },
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
      <div className="sticky top-0 z-10 bg-background">
        <PageContainer width="wide" className="border-b border-border">
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
        </PageContainer>
      </div>
      <PageContainer width="wide" className="flex flex-1 flex-col pt-6 space-y-4">
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-32 w-full rounded-xl" />
        <Skeleton className="h-10 w-full" />
      </PageContainer>
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
  const [agentStatusOverride, setAgentStatusOverride] = useState<string | null>(
    initialStatus === "booting" ? "booting" : null,
  );
  const [modelOverride, setModelOverride] = useState<string | null>(null);
  const [message, setMessage] = useState("");
  const [pendingTurnStartedAt, setPendingTurnStartedAt] = useState<number | null>(
    null,
  );
  const { sendAgentMessage, loading: sendingMessage } = useSendAgentMessage();
  const createSession = useCreateSession();
  const { updateAgent } = useUpdateAgent();
  const requestedTab =
    searchParams.get("tab") === "integrations"
      ? "permissions"
      : searchParams.get("tab");
  const tab = TABS.some((candidate) => candidate.key === requestedTab)
    ? (requestedTab as TabKey)
    : "logs";
  const agentStatus = agentStatusOverride ?? agent?.status ?? "";
  const model = modelOverride ?? agent?.model ?? "";

  // Simulate boot → running transition
  useEffect(() => {
    if (agentStatus === "booting") {
      const t = setTimeout(() => {
        setAgentStatusOverride("running");
        router.replace(`/agents/${id}?tab=logs`);
      }, 5000);
      return () => clearTimeout(t);
    }
  }, [agentStatus, id, router]);

  if (agentStatus === "booting" || (agentLoading && !agent)) {
    return <AgentSkeleton />;
  }

  const displayName = agent?.name ?? "Unnamed Agent";
  const displayId = agent?.id ?? id;
  const displayStatus = agentStatus || agent?.status || "stopped";
  const createdAt = agent?.createdAt ?? null;

  const submit = () => {
    if (!message.trim()) return;
    const content = message;
    setPendingTurnStartedAt(Date.now());
    setMessage("");
    void sendAgentMessage(id, content).catch(() => {
      setPendingTurnStartedAt(null);
    });
  };

  return (
    <div
      className={
        tab === "logs"
          ? "flex h-screen flex-col overflow-hidden"
          : "flex min-h-screen flex-col"
      }
    >
      {/* Sticky agent header + tabs */}
      <div className="sticky top-0 z-10 bg-background">
        <PageContainer width="wide" className="border-b border-border">
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
              <HelpTooltip>
                The selected model controls LLM dispatch for this agent. Auto is
                only shown when Anthropic smart routing is configured.
              </HelpTooltip>
              {models.length === 0 && isDevelopment() ? (
                <Link href="/providers" className="flex items-center gap-2 rounded-md border border-dashed border-border px-3 py-1.5 text-xs text-muted-foreground hover:text-foreground hover:border-foreground transition-colors">
                  Add provider
                </Link>
              ) : (
                <Select
                  value={model}
                  onValueChange={(v) => {
                    if (!v) return;
                    const selected = models.find((m) => m.id === v);
                    setModelOverride(v);
                    void updateAgent(id, {
                      model: v,
                      provider: selected?.provider,
                    }).catch(() => setModelOverride(null));
                  }}
                >
                  <SelectTrigger className="w-[180px] h-8 text-xs">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent className="w-max min-w-(--anchor-width) max-w-[calc(100vw-2rem)]">
                    {models.map((m) => (
                      <SelectItem
                        key={m.id}
                        value={m.id}
                        title={getModelTooltip(m.id)}
                      >
                        {m.displayName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
              <WithTooltip tooltip="Start a fresh conversation session for this agent. Existing logs and memory are not deleted.">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => createSession(id)}
                >
                  <PlusIcon className="size-3.5" />
                  New Session
                </Button>
              </WithTooltip>
              <WithTooltip tooltip="Return to the agent list.">
                <Button
                  variant="outline"
                  size="sm"
                  nativeButton={false}
                  render={<Link href="/agents" />}
                >
                  All agents
                </Button>
              </WithTooltip>
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
        </PageContainer>
      </div>

      <PageContainer
        width="wide"
        className={
          tab === "logs"
            ? "flex min-h-0 flex-1 flex-col overflow-hidden"
            : "flex flex-1 flex-col"
        }
      >
        <div
          className={
            tab === "logs"
              ? "flex min-h-0 flex-1 flex-col"
              : "flex flex-1 flex-col"
          }
        >
          {tab === "permissions" && <AgentIntegrationsTab agentId={id} />}
          {tab === "logs" && (
            <AgentLogsTab
              agentId={id}
              pendingTurnStartedAt={pendingTurnStartedAt}
              composer={
                <div className="flex w-full items-center gap-2">
                  <Input
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                    placeholder="Send a message to the agent..."
                    className="flex-1"
                    onKeyDown={(e) => {
                      if (e.key === "Enter" && message.trim()) submit();
                    }}
                  />
                  <WithTooltip tooltip="Send this message into the active agent session.">
                    <Button
                      size="icon"
                      disabled={!message.trim() || sendingMessage}
                      onClick={submit}
                    >
                      <SendIcon className="size-4" />
                    </Button>
                  </WithTooltip>
                </div>
              }
            />
          )}
        </div>
      </PageContainer>

    </div>
  );
}
