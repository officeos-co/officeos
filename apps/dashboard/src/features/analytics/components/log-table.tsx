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
} from "@/ui/table";
import { Skeleton } from "@/ui/skeleton";
import { HelpTooltip, WithTooltip } from "@/ui/help-tooltip";
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
  LoaderCircleIcon,
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

function typeTooltip(log: AgentLog) {
  switch (log.type) {
    case "tool_call":
      return "The agent requested a tool call. Arguments are logged separately from the result.";
    case "tool_result":
      return "A tool returned data to the agent.";
    case "message_in":
      return "A user or channel message entered the agent loop.";
    case "message_out":
      return "The agent produced an outgoing message.";
    case "channel_in":
      return "A connected channel delivered a message.";
    case "channel_out":
      return "The backend sent a response to a connected channel.";
    case "error":
      return "The run hit an error. Open the row for details where available.";
    default:
      return "Structured backend log entry from the agent run timeline.";
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
  loading = false,
  skeletonRows = 10,
  thinking = false,
}: {
  logs: (AgentLog & { agentName?: string })[];
  showAgent?: boolean;
  selectedLogId?: string | null;
  onSelectLog?: (log: AgentLog & { agentName?: string }) => void;
  showSelectionColumn?: boolean;
  className?: string;
  loading?: boolean;
  skeletonRows?: number;
  thinking?: boolean;
}) {
  const shouldShowSelectionColumn = showSelectionColumn ?? Boolean(onSelectLog);
  const columnCount =
    (showAgent ? 7 : 6) + (shouldShowSelectionColumn ? 1 : 0);

  return (
    <Table className={className}>
      <TableHeader>
        <TableRow className="sticky top-0 z-10 bg-background hover:bg-background">
          {shouldShowSelectionColumn && <TableHead className="w-10 px-3" />}
          <TableHead className="w-[32px]" />
          <TableHead>
            <span className="inline-flex items-center gap-1.5">
              Type
              <HelpTooltip>
                Logs are typed events, not raw chat transcripts. Tool calls,
                tool results, messages, and system events are recorded
                separately.
              </HelpTooltip>
            </span>
          </TableHead>
          {showAgent && <TableHead>Agent</TableHead>}
          <TableHead>Source</TableHead>
          <TableHead>Content</TableHead>
          <TableHead className="text-right">Duration</TableHead>
          <TableHead className="text-right">Time</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {loading &&
          Array.from({ length: skeletonRows }).map((_, index) => (
            <TableRow key={`log-skeleton-${index}`} aria-hidden="true">
              {shouldShowSelectionColumn && (
                <TableCell className="w-10 px-3">
                  <Skeleton className="size-4 rounded" />
                </TableCell>
              )}
              <TableCell>
                <Skeleton className="size-6 rounded-md" />
              </TableCell>
              <TableCell>
                <Skeleton className="h-5 w-20" />
              </TableCell>
              {showAgent && (
                <TableCell>
                  <Skeleton className="h-4 w-24" />
                </TableCell>
              )}
              <TableCell>
                <Skeleton className="h-5 w-20" />
              </TableCell>
              <TableCell className="max-w-[400px]">
                <Skeleton
                  className={cn(
                    "h-4",
                    index % 3 === 0
                      ? "w-full"
                      : index % 3 === 1
                        ? "w-3/4"
                        : "w-1/2",
                  )}
                />
              </TableCell>
              <TableCell>
                <Skeleton className="ml-auto h-4 w-14" />
              </TableCell>
              <TableCell>
                <Skeleton className="ml-auto h-4 w-20" />
              </TableCell>
            </TableRow>
          ))}
        {!loading &&
          logs.map((log) => (
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
                <WithTooltip tooltip={typeTooltip(log)}>
                  <span className="rounded bg-muted px-1.5 py-0.5 text-xs">
                    {typeLabel(log)}
                  </span>
                </WithTooltip>
              </TableCell>
              {showAgent && (
                <TableCell className="text-xs">
                  {log.agentName ?? "—"}
                </TableCell>
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
              <TableCell className="text-xs max-w-[400px] truncate text-foreground/70">
                {log.content}
              </TableCell>
              <TableCell className="text-right">
                {log.durationMs ? (
                  <span className="flex items-center justify-end gap-0.5 text-xs text-foreground/60">
                    <ClockIcon className="size-3" />
                    {log.durationMs}ms
                  </span>
                ) : (
                  <span className="text-xs text-muted-foreground">—</span>
                )}
              </TableCell>
              <TableCell className="text-right text-xs text-foreground/60">
                {formatTime(log.time)}
              </TableCell>
            </TableRow>
          ))}
        {!loading && thinking && (
          <TableRow aria-live="polite" className="bg-muted/20 hover:bg-muted/30">
            {shouldShowSelectionColumn && <TableCell className="w-10 px-3" />}
            <TableCell>
              <div className="flex size-6 items-center justify-center">
                <LoaderCircleIcon className="size-4 animate-spin text-muted-foreground" />
              </div>
            </TableCell>
            <TableCell>
              <span className="rounded bg-muted px-1.5 py-0.5 text-xs">
                Thinking
              </span>
            </TableCell>
            {showAgent && (
              <TableCell>
                <Skeleton className="h-4 w-24" />
              </TableCell>
            )}
            <TableCell>
              <span className="text-xs text-muted-foreground">—</span>
            </TableCell>
            <TableCell className="max-w-[400px]">
              <div className="flex flex-col gap-1.5">
                <Skeleton className="h-3.5 w-full max-w-[360px]" />
                <Skeleton className="h-3.5 w-2/3 max-w-[240px]" />
              </div>
            </TableCell>
            <TableCell>
              <Skeleton className="ml-auto h-4 w-14" />
            </TableCell>
            <TableCell className="text-right text-xs text-muted-foreground">
              now
            </TableCell>
          </TableRow>
        )}
        {!loading && logs.length === 0 && !thinking && (
          <TableRow>
            <TableCell
              colSpan={columnCount}
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
