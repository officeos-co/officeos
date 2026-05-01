"use client";

import type { AgentLog } from "@/types/logs";
import { cn } from "@/lib/utils";

type LogWithAgent = AgentLog & { agentName?: string };

function typeLabel(type: AgentLog["type"]) {
  return type.replace(/_/g, " ");
}

function sourceLabel(log: LogWithAgent) {
  return (
    log.tool ?? log.channel ?? log.integration ?? log.agentName ?? "system"
  );
}

export function LogDetailPanel({
  log,
  className,
}: {
  log: LogWithAgent | null;
  className?: string;
}) {
  return (
    <aside
      className={cn(
        "flex h-full min-h-0 w-[360px] shrink-0 flex-col overflow-hidden border-l border-border bg-background",
        className,
      )}
    >
      <div className="flex min-h-14 shrink-0 items-center justify-between border-b border-border px-4 py-2">
        <div>
          <h3 className="text-sm font-semibold">Log detail</h3>
          {log && (
            <p className="mt-0.5 text-xs text-muted-foreground">
              {sourceLabel(log)} · {new Date(log.time).toLocaleTimeString()}
            </p>
          )}
        </div>
      </div>

      {log ? (
        <div className="min-h-0 flex-1 overflow-auto">
          <div className="border-b border-border px-4 py-4">
            <div className="mb-3 flex items-center gap-2">
              <span
                className={cn(
                  "rounded px-2 py-1 text-xs font-medium capitalize",
                  log.type === "error"
                    ? "bg-destructive/10 text-destructive"
                    : "bg-muted text-foreground",
                )}
              >
                {typeLabel(log.type)}
              </span>
              <span className="font-mono text-xs text-muted-foreground">
                {log.id.slice(0, 12)}
              </span>
            </div>
            <h4 className="text-base font-semibold leading-6">
              {typeLabel(log.type)}
            </h4>
            <p className="mt-1 text-xs text-muted-foreground">
              {new Date(log.time).toLocaleString()}
            </p>
          </div>

          <div className="space-y-4 px-4 py-4 text-sm">
            <div className="grid grid-cols-[96px_1fr] gap-x-3 gap-y-2">
              {log.agentName && (
                <>
                  <span className="text-muted-foreground">Agent</span>
                  <span className="text-xs">{log.agentName}</span>
                </>
              )}
              {log.tool && (
                <>
                  <span className="text-muted-foreground">Tool</span>
                  <code className="w-fit rounded bg-muted px-1.5 py-0.5 font-mono text-xs">
                    {log.tool}
                  </code>
                </>
              )}
              {log.channel && (
                <>
                  <span className="text-muted-foreground">Channel</span>
                  <code className="w-fit rounded bg-muted px-1.5 py-0.5 font-mono text-xs">
                    {log.channel}
                  </code>
                </>
              )}
              {log.integration && (
                <>
                  <span className="text-muted-foreground">Integration</span>
                  <code className="w-fit rounded bg-muted px-1.5 py-0.5 font-mono text-xs">
                    {log.integration}
                  </code>
                </>
              )}
              {log.durationMs != null && (
                <>
                  <span className="text-muted-foreground">Duration</span>
                  <span className="text-xs">{log.durationMs}ms</span>
                </>
              )}
              {log.tokens && (
                <>
                  <span className="text-muted-foreground">Tokens</span>
                  <span className="text-xs">
                    {log.tokens.input} in / {log.tokens.output} out
                  </span>
                </>
              )}
            </div>

            <div>
              <div className="mb-2 text-xs font-medium text-muted-foreground">
                Content
              </div>
              <pre
                className={cn(
                  "rounded-md border p-3 font-mono text-xs leading-5 whitespace-pre-wrap break-words",
                  log.type === "error"
                    ? "border-destructive/20 bg-destructive/10 text-destructive"
                    : "border-border bg-muted/50",
                )}
              >
                {log.content}
              </pre>
            </div>
          </div>
        </div>
      ) : (
        <div className="flex flex-1 items-center justify-center px-4 text-center text-sm text-muted-foreground">
          Select a log entry to view details
        </div>
      )}
    </aside>
  );
}
