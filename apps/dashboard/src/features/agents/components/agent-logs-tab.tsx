"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { useAgentLogs } from "@/features/analytics/api/useAgentLogs";
import {
  ArrowDownLeftIcon,
  ArrowUpRightIcon,
  TerminalIcon,
  PlayIcon,
  SquareIcon,
  AlertTriangleIcon,
  InfoIcon,
  XIcon,
} from "lucide-react";

export function AgentLogsTab({ agentId }: { agentId: string }) {
  const { logs } = useAgentLogs(agentId);
  const [selectedLogId, setSelectedLogId] = useState<string | null>(null);
  const selectedLog = logs.find((l) => l.id === selectedLogId) ?? null;

  return (
    <div className="flex flex-1 pt-4 gap-0 min-h-0">
      {/* Log list — left side */}
      <div
        className={`flex-1 overflow-auto border rounded-lg ${selectedLog ? "max-w-[60%]" : ""}`}
      >
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b text-left sticky top-0 bg-background">
              <th className="px-3 py-3 font-medium w-[32px]" />
              <th className="px-3 py-3 font-medium">Type</th>
              <th className="px-3 py-3 font-medium">Source</th>
              <th className="px-3 py-3 font-medium">Content</th>
              <th className="px-3 py-3 font-medium text-right">Time</th>
            </tr>
          </thead>
          <tbody>
            {logs.map((log) => (
              <tr
                key={log.id}
                onClick={() =>
                  setSelectedLogId(selectedLogId === log.id ? null : log.id)
                }
                className={`border-b last:border-0 cursor-pointer transition-colors ${
                  selectedLogId === log.id ? "bg-muted" : "hover:bg-muted/50"
                }`}
              >
                <td className="px-3 py-2.5">
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
                </td>
                <td className="px-3 py-2.5">
                  <span className="rounded bg-muted px-1.5 py-0.5 text-xs">
                    {log.type.replace(/_/g, " ")}
                  </span>
                </td>
                <td className="px-3 py-2.5">
                  {log.tool || log.channel ? (
                    <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs">
                      {log.tool ?? log.channel}
                    </code>
                  ) : (
                    <span className="text-xs text-muted-foreground">—</span>
                  )}
                </td>
                <td className="px-3 py-2.5 text-xs max-w-[300px] truncate text-muted-foreground">
                  {log.content}
                </td>
                <td className="px-3 py-2.5 text-right text-xs text-muted-foreground whitespace-nowrap">
                  {new Date(log.time).toLocaleTimeString(undefined, {
                    hour: "2-digit",
                    minute: "2-digit",
                    second: "2-digit",
                  })}
                </td>
              </tr>
            ))}
            {logs.length === 0 && (
              <tr>
                <td
                  colSpan={5}
                  className="px-3 py-8 text-center text-muted-foreground"
                >
                  No logs yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Detail panel — right side */}
      {selectedLog && (
        <div className="w-[40%] border rounded-lg ml-3 flex flex-col overflow-auto">
          <div className="flex items-center justify-between px-4 py-3 border-b sticky top-0 bg-background">
            <h3 className="text-sm font-semibold">Log detail</h3>
            <Button
              variant="ghost"
              size="icon"
              className="size-7"
              onClick={() => setSelectedLogId(null)}
            >
              <XIcon className="size-4" />
            </Button>
          </div>
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
        </div>
      )}
    </div>
  );
}
