"use client";

import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useAgentBrowser } from "@/features/agents/api/useAgentBrowser";
import {
  ExternalLinkIcon,
  MonitorIcon,
  PlayIcon,
  RefreshCwIcon,
  SquareIcon,
} from "lucide-react";

function normalizeStatus(status: string | null | undefined) {
  if (!status || status === "not_started") return "Not started";
  return status.replace(/_/g, " ");
}

export function AgentBrowserTab({ agentId }: { agentId: string }) {
  const {
    browser,
    viewUrl,
    loading,
    error,
    busy,
    start,
    restart,
    stop,
  } = useAgentBrowser(agentId);

  const hasSession = Boolean(browser?.runtimeSessionId);
  const canView = Boolean(viewUrl);

  return (
    <div className="flex flex-col gap-4 pt-4 min-h-0">
      <div className="flex items-center justify-between gap-3 rounded-lg border border-border bg-card px-4 py-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <MonitorIcon className="size-4 text-muted-foreground" />
            <h2 className="text-sm font-medium">Browser</h2>
            <span className="rounded-full bg-muted px-2 py-0.5 text-[10px] font-medium uppercase text-muted-foreground">
              {normalizeStatus(browser?.status)}
            </span>
          </div>
          <div className="mt-1 truncate text-xs text-muted-foreground">
            {browser?.currentUrl || browser?.title || browser?.runtimeSessionId || "No browser session"}
          </div>
        </div>
        <div className="flex shrink-0 items-center gap-2">
          {canView && (
            <Button variant="outline" size="sm" nativeButton={false} render={<a href={viewUrl ?? "#"} target="_blank" rel="noreferrer" />}>
              <ExternalLinkIcon className="size-3.5" />
              Open
            </Button>
          )}
          <Button variant="outline" size="sm" disabled={busy} onClick={start}>
            <PlayIcon className="size-3.5" />
            Start
          </Button>
          <Button variant="outline" size="sm" disabled={busy || !hasSession} onClick={restart}>
            <RefreshCwIcon className="size-3.5" />
            Restart
          </Button>
          <Button variant="outline" size="sm" disabled={busy || !hasSession} onClick={stop}>
            <SquareIcon className="size-3.5" />
            Stop
          </Button>
        </div>
      </div>

      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {error.message}
        </div>
      )}

      <div className="min-h-[520px] overflow-hidden rounded-lg border border-border bg-card">
        {loading && !canView ? (
          <div className="space-y-3 p-4">
            <Skeleton className="h-8 w-52" />
            <Skeleton className="h-[460px] w-full" />
          </div>
        ) : canView ? (
          <iframe
            src={viewUrl ?? undefined}
            title="Agent browser"
            className="h-[70vh] min-h-[520px] w-full bg-background"
            referrerPolicy="no-referrer"
          />
        ) : (
          <div className="flex min-h-[520px] items-center justify-center px-4 text-sm text-muted-foreground">
            Start the browser to open a live session.
          </div>
        )}
      </div>
    </div>
  );
}
