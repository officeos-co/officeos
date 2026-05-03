"use client";

import type { AgentLog } from "@/types/logs";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { XIcon } from "lucide-react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";

type LogWithAgent = AgentLog & { agentName?: string };

function typeLabel(type: AgentLog["type"]) {
  return type
    .replace(/_/g, " ")
    .replace(/\b\w/g, (char) => char.toUpperCase());
}

function formatLogContent(content: string):
  | { type: "json"; content: string }
  | { type: "markdown"; content: string } {
  const trimmed = content.trim();

  if (!trimmed) return { type: "markdown", content };

  if (!["{", "["].includes(trimmed[0])) {
    return { type: "markdown", content };
  }

  try {
    return {
      type: "json",
      content: JSON.stringify(JSON.parse(trimmed), null, 2),
    };
  } catch {
    return { type: "markdown", content };
  }
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
  const formattedContent = log ? formatLogContent(log.content) : null;

  return (
    <aside
      className={cn(
        "flex h-full min-h-0 w-[360px] shrink-0 flex-col overflow-hidden border-l border-border bg-background",
        className,
      )}
    >
      {log ? (
        <div className="flex min-h-0 flex-1 flex-col overflow-hidden">
          <div className="shrink-0 border-b border-border px-4 py-4">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
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

          <div className="min-h-0 flex-1 space-y-4 overflow-y-auto px-4 py-4 text-sm [scrollbar-gutter:stable]">
            <div className="grid grid-cols-[96px_minmax(0,1fr)] gap-x-3 gap-y-2 rounded-md border border-border bg-muted/20 p-3">
              {log.agentName && (
                <>
                  <span className="text-muted-foreground">Agent</span>
                  <span className="min-w-0 break-words text-xs">
                    {log.agentName}
                  </span>
                </>
              )}
              {log.tool && (
                <>
                  <span className="text-muted-foreground">Tool</span>
                  <code className="min-w-0 rounded bg-muted px-1.5 py-0.5 font-mono text-xs break-all">
                    {log.tool}
                  </code>
                </>
              )}
              {log.channel && (
                <>
                  <span className="text-muted-foreground">Channel</span>
                  <code className="min-w-0 rounded bg-muted px-1.5 py-0.5 font-mono text-xs break-all">
                    {log.channel}
                  </code>
                </>
              )}
              {log.integration && (
                <>
                  <span className="text-muted-foreground">Integration</span>
                  <code className="min-w-0 rounded bg-muted px-1.5 py-0.5 font-mono text-xs break-all">
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
              {formattedContent?.type === "json" ? (
                <pre
                  className={cn(
                    "overflow-x-auto rounded-md border p-3 font-mono text-xs leading-5 whitespace-pre",
                    log.type === "error"
                      ? "border-destructive/20 bg-destructive/10 text-destructive"
                      : "border-border bg-muted/50",
                  )}
                >
                  {formattedContent.content}
                </pre>
              ) : (
                <div
                  className={cn(
                    "overflow-x-auto rounded-md border p-3 text-sm leading-6",
                    log.type === "error"
                      ? "border-destructive/20 bg-destructive/10 text-destructive"
                      : "border-border bg-muted/50",
                  )}
                >
                  <ReactMarkdown
                    remarkPlugins={[remarkGfm]}
                    components={{
                      a: ({ className, ...props }) => (
                        <a
                          className={cn(
                            "font-medium text-primary underline underline-offset-4 break-all",
                            className,
                          )}
                          target="_blank"
                          rel="noreferrer"
                          {...props}
                        />
                      ),
                      h1: ({ className, ...props }) => (
                        <h1
                          className={cn(
                            "mb-3 text-lg font-semibold leading-7",
                            className,
                          )}
                          {...props}
                        />
                      ),
                      h2: ({ className, ...props }) => (
                        <h2
                          className={cn(
                            "mb-2 text-base font-semibold leading-6",
                            className,
                          )}
                          {...props}
                        />
                      ),
                      h3: ({ className, ...props }) => (
                        <h3
                          className={cn(
                            "mb-2 text-sm font-semibold leading-5",
                            className,
                          )}
                          {...props}
                        />
                      ),
                      p: ({ className, ...props }) => (
                        <p
                          className={cn(
                            "mb-3 whitespace-pre-wrap last:mb-0",
                            className,
                          )}
                          {...props}
                        />
                      ),
                      ul: ({ className, ...props }) => (
                        <ul
                          className={cn(
                            "mb-3 list-disc space-y-1 pl-5 last:mb-0",
                            className,
                          )}
                          {...props}
                        />
                      ),
                      ol: ({ className, ...props }) => (
                        <ol
                          className={cn(
                            "mb-3 list-decimal space-y-1 pl-5 last:mb-0",
                            className,
                          )}
                          {...props}
                        />
                      ),
                      code: ({ className, ...props }) => (
                        <code
                          className={cn(
                            "rounded bg-muted px-1 py-0.5 font-mono text-xs",
                            className,
                          )}
                          {...props}
                        />
                      ),
                      pre: ({ className, ...props }) => (
                        <pre
                          className={cn(
                            "mb-3 overflow-x-auto rounded border border-border bg-background p-3 font-mono text-xs leading-5 whitespace-pre last:mb-0",
                            className,
                          )}
                          {...props}
                        />
                      ),
                    }}
                  >
                    {formattedContent?.content ?? ""}
                  </ReactMarkdown>
                </div>
              )}
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
