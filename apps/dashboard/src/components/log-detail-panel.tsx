"use client";

import type { AgentLog } from "@/types/logs";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { XIcon } from "lucide-react";

type LogWithAgent = AgentLog & { agentName?: string };

function typeLabel(type: AgentLog["type"]) {
  return type.replace(/_/g, " ");
}

export function LogDetailPanel({
  log,
  onClose,
  className,
}: {
  log: LogWithAgent | null;
  onClose?: () => void;
  className?: string;
}) {
  return (
    <aside
      className={cn(
        "flex h-full min-h-0 w-[360px] shrink-0 flex-col overflow-hidden border-l border-border bg-background",
        className,
      )}
    >
      {log ? (
        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="border-b border-border px-4 py-4">
            <div className="flex items-start justify-between gap-3">
              <div>
                <h4 className="text-base font-semibold leading-6">
                  {typeLabel(log.type)}
                </h4>
                <p className="mt-1 text-xs text-muted-foreground">
                  {new Date(log.time).toLocaleString()}
                </p>
              </div>
              {onClose && (
                <Button
                  variant="ghost"
                  size="icon"
                  className="size-8 shrink-0"
                  onClick={onClose}
                >
                  <XIcon className="size-4" />
                  <span className="sr-only">Close log detail</span>
                </Button>
              )}
            </div>
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
