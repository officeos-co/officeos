"use client";

import type { AgentLog } from "@/types/logs";
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
  TableSelectionCell,
} from "@/components/ui/table";
import { cn } from "@/lib/utils";
import {
  TerminalIcon,
  ArrowDownLeftIcon,
  ArrowUpRightIcon,
  InfoIcon,
  ClockIcon,
  PlayIcon,
  SquareIcon,
  AlertTriangleIcon,
} from "lucide-react";

function logIcon(log: AgentLog) {
  if (log.type === "tool_call" || log.type === "tool_result") {
    return <TerminalIcon className="size-4 text-muted-foreground" />;
  }
  if (log.type === "channel_in" || log.type === "channel_out") {
    // channel logos are SVG from backend — not available at log render time
  }
  if (log.type === "message_in")
    return <ArrowDownLeftIcon className="size-4 text-blue-500" />;
  if (log.type === "message_out")
    return <ArrowUpRightIcon className="size-4 text-emerald-500" />;
  if (log.type === "agent_startup")
    return <PlayIcon className="size-4 text-emerald-500" />;
  if (log.type === "agent_shutdown")
    return <SquareIcon className="size-4 text-muted-foreground" />;
  if (log.type === "error")
    return <AlertTriangleIcon className="size-4 text-red-500" />;
  return <InfoIcon className="size-4 text-muted-foreground" />;
}

function typeLabel(log: AgentLog) {
  switch (log.type) {
    case "tool_call":
      return "Tool call";
    case "tool_result":
      return "Tool result";
    case "channel_in":
      return "Channel in";
    case "channel_out":
      return "Channel out";
    case "message_in":
      return "Message in";
    case "message_out":
      return "Message out";
    case "system":
      return "System";
    case "agent_startup":
      return "Startup";
    case "agent_shutdown":
      return "Shutdown";
    case "error":
      return "Error";
    default:
      return log.type;
  }
}

function formatTime(ts: number) {
  return new Date(ts).toLocaleTimeString(undefined, {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

export function LogTable({
  logs,
  showAgent = false,
  selectedLogId,
  onSelectLog,
  showSelectionColumn,
  className,
}: {
  logs: (AgentLog & { agentName?: string })[];
  showAgent?: boolean;
  selectedLogId?: string | null;
  onSelectLog?: (log: AgentLog & { agentName?: string }) => void;
  showSelectionColumn?: boolean;
  className?: string;
}) {
  const shouldShowSelectionColumn = showSelectionColumn ?? Boolean(onSelectLog);

  return (
    <Table className={className}>
      <TableHeader>
        <TableRow className="sticky top-0 z-10 bg-background hover:bg-background">
          {shouldShowSelectionColumn && <TableHead className="w-10 px-3" />}
          <TableHead className="w-[32px]" />
          <TableHead>Type</TableHead>
          {showAgent && <TableHead>Agent</TableHead>}
          <TableHead>Source</TableHead>
          <TableHead>Content</TableHead>
          <TableHead className="text-right">Duration</TableHead>
          <TableHead className="text-right">Time</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {logs.map((log) => (
          <TableRow
            key={log.id}
            onClick={() => onSelectLog?.(log)}
            data-state={selectedLogId === log.id ? "selected" : undefined}
            className={cn(onSelectLog && "cursor-pointer")}
          >
            {shouldShowSelectionColumn && (
              <TableSelectionCell
                checked={selectedLogId === log.id}
                aria-label={`Select ${typeLabel(log)} log`}
                onCheckedChange={() => onSelectLog?.(log)}
              />
            )}
            <TableCell>
              <div className="flex size-6 items-center justify-center">
                {logIcon(log)}
              </div>
            </TableCell>
            <TableCell>
              <span className="rounded bg-muted px-1.5 py-0.5 text-xs">
                {typeLabel(log)}
              </span>
            </TableCell>
            {showAgent && (
              <TableCell className="text-xs">{log.agentName ?? "—"}</TableCell>
            )}
            <TableCell>
              {log.tool || log.channel ? (
                <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs">
                  {log.tool ?? log.channel}
                </code>
              ) : (
                <span className="text-xs text-muted-foreground">—</span>
              )}
            </TableCell>
            <TableCell className="text-xs max-w-[400px] truncate text-muted-foreground">
              {log.content}
            </TableCell>
            <TableCell className="text-right">
              {log.durationMs ? (
                <span className="flex items-center justify-end gap-0.5 text-xs text-muted-foreground">
                  <ClockIcon className="size-3" />
                  {log.durationMs}ms
                </span>
              ) : (
                <span className="text-xs text-muted-foreground">—</span>
              )}
            </TableCell>
            <TableCell className="text-right text-xs text-muted-foreground">
              {formatTime(log.time)}
            </TableCell>
          </TableRow>
        ))}
        {logs.length === 0 && (
          <TableRow>
            <TableCell
              colSpan={(showAgent ? 7 : 6) + (shouldShowSelectionColumn ? 1 : 0)}
              className="py-8 text-center text-muted-foreground"
            >
              No logs yet.
            </TableCell>
          </TableRow>
        )}
      </TableBody>
    </Table>
  );
}
