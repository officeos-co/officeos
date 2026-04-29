"use client";

import { useState, useRef, useEffect, useCallback } from "react";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from "@/components/ui/table";
import { useAgentLogs } from "@/features/analytics/api/useAgentLogs";
import {
  ArrowDownLeftIcon,
  ArrowUpRightIcon,
  TerminalIcon,
  PlayIcon,
  SquareIcon,
  AlertTriangleIcon,
  InfoIcon,
} from "lucide-react";

function LogSkeletonRows() {
  return (
    <>
      {Array.from({ length: 6 }).map((_, i) => (
        <TableRow key={i}>
          <TableCell>
            <Skeleton className="size-6 rounded" />
          </TableCell>
          <TableCell>
            <Skeleton className="h-5 w-16 rounded" />
          </TableCell>
          <TableCell>
            <Skeleton className="h-5 w-20 rounded" />
          </TableCell>
          <TableCell>
            <Skeleton className="h-4 w-48 rounded" />
          </TableCell>
          <TableCell className="text-right">
            <Skeleton className="h-4 w-16 rounded ml-auto" />
          </TableCell>
        </TableRow>
      ))}
    </>
  );
}

export function AgentLogsTab({ agentId }: { agentId: string }) {
  const { logs, loading } = useAgentLogs(agentId);
  const [selectedLogId, setSelectedLogId] = useState<string | null>(null);
  const selectedLog = logs.find((l) => l.id === selectedLogId) ?? null;
  const scrollRef = useRef<HTMLDivElement>(null);
  const isNearBottom = useRef(true);

  const handleScroll = useCallback(() => {
    const el = scrollRef.current;
    if (!el) return;
    isNearBottom.current = el.scrollHeight - el.scrollTop - el.clientHeight < 80;
  }, []);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [loading]);

  useEffect(() => {
    if (isNearBottom.current && scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [logs.length]);

  return (
    <div className="flex flex-1 pt-4 gap-3 min-h-0">
      {/* Log list — left side */}
      <div ref={scrollRef} onScroll={handleScroll} className="flex-1 overflow-auto">
        <Table>
          <TableHeader>
            <TableRow className="sticky top-0 bg-background">
              <TableHead className="w-[32px]" />
              <TableHead>Type</TableHead>
              <TableHead>Source</TableHead>
              <TableHead>Content</TableHead>
              <TableHead className="text-right">Time</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {logs.map((log) => (
              <TableRow
                key={log.id}
                onClick={() =>
                  setSelectedLogId(selectedLogId === log.id ? null : log.id)
                }
                className={`cursor-pointer ${
                  selectedLogId === log.id ? "bg-muted" : ""
                }`}
              >
                <TableCell>
                  <div className="flex size-6 items-center justify-center">
                    {log.type === "message_in" && (
                      <ArrowDownLeftIcon className="size-4 text-blue-500" />
                    )}
                    {log.type === "message_out" && (
                      <ArrowUpRightIcon className="size-4 text-emerald-500" />
                    )}
                    {(log.type === "tool_call" ||
                      log.type === "tool_result") && (
                      <TerminalIcon className="size-4 text-muted-foreground" />
                    )}
                    {log.type === "agent_startup" && (
                      <PlayIcon className="size-4 text-emerald-500" />
                    )}
                    {log.type === "agent_shutdown" && (
                      <SquareIcon className="size-4 text-muted-foreground" />
                    )}
                    {log.type === "error" && (
                      <AlertTriangleIcon className="size-4 text-red-500" />
                    )}
                    {![
                      "message_in",
                      "message_out",
                      "tool_call",
                      "tool_result",
                      "agent_startup",
                      "agent_shutdown",
                      "error",
                    ].includes(log.type) && (
                      <InfoIcon className="size-4 text-muted-foreground" />
                    )}
                  </div>
                </TableCell>
                <TableCell>
                  <span className="rounded bg-muted px-1.5 py-0.5 text-xs">
                    {log.type.replace(/_/g, " ")}
                  </span>
                </TableCell>
                <TableCell>
                  {log.tool || log.channel ? (
                    <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs">
                      {log.tool ?? log.channel}
                    </code>
                  ) : (
                    <span className="text-xs text-muted-foreground">—</span>
                  )}
                </TableCell>
                <TableCell className="text-xs max-w-[300px] truncate text-muted-foreground">
                  {log.content}
                </TableCell>
                <TableCell className="text-right text-xs text-muted-foreground">
                  {new Date(log.time).toLocaleTimeString(undefined, {
                    hour: "2-digit",
                    minute: "2-digit",
                    second: "2-digit",
                  })}
                </TableCell>
              </TableRow>
            ))}
            {logs.length === 0 && loading && <LogSkeletonRows />}
            {logs.length === 0 && !loading && (
              <TableRow>
                <TableCell
                  colSpan={5}
                  className="py-8 text-center text-muted-foreground"
                >
                  No logs yet.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      {/* Detail panel — right side, always visible */}
      <div className="w-[340px] shrink-0 border rounded-lg flex flex-col sticky top-0 self-start max-h-[calc(100vh-12rem)] overflow-auto">
        <div className="px-4 py-3 border-b sticky top-0 bg-background z-10">
          <h3 className="text-sm font-semibold">Log detail</h3>
        </div>
        {selectedLog ? (
          <div className="px-4 py-3 space-y-3 text-sm">
            <div className="grid grid-cols-[100px_1fr] gap-y-2 gap-x-3">
              <span className="text-muted-foreground">Type</span>
              <span className="rounded bg-muted px-1.5 py-0.5 text-xs w-fit">
                {selectedLog.type.replace(/_/g, " ")}
              </span>

              <span className="text-muted-foreground">Time</span>
              <span className="text-xs">
                {new Date(selectedLog.time).toLocaleString()}
              </span>

              {selectedLog.tool && (
                <>
                  <span className="text-muted-foreground">Tool</span>
                  <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs w-fit">
                    {selectedLog.tool}
                  </code>
                </>
              )}

              {selectedLog.channel && (
                <>
                  <span className="text-muted-foreground">Channel</span>
                  <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs w-fit">
                    {selectedLog.channel}
                  </code>
                </>
              )}

              {selectedLog.integration && (
                <>
                  <span className="text-muted-foreground">Integration</span>
                  <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs w-fit">
                    {selectedLog.integration}
                  </code>
                </>
              )}

              {selectedLog.durationMs != null && (
                <>
                  <span className="text-muted-foreground">Duration</span>
                  <span className="text-xs">{selectedLog.durationMs}ms</span>
                </>
              )}

              {selectedLog.tokens && (
                <>
                  <span className="text-muted-foreground">Tokens</span>
                  <span className="text-xs">
                    {selectedLog.tokens.input} in / {selectedLog.tokens.output}{" "}
                    out
                  </span>
                </>
              )}
            </div>

            <div className="pt-2 border-t">
              <span className="text-xs font-medium text-muted-foreground">
                Content
              </span>
              <pre className="mt-1.5 rounded bg-muted p-3 text-xs whitespace-pre-wrap break-words font-mono">
                {selectedLog.content}
              </pre>
            </div>
          </div>
        ) : (
          <div className="flex items-center justify-center flex-1 text-sm text-muted-foreground py-8">
            Select a log entry to view details
          </div>
        )}
      </div>
    </div>
  );
}
