"use client"

import Image from "next/image"
import { integrations } from "@/data/integrations"
import { channels } from "@/data/channels"
import type { AgentLog } from "@/data/agent-mock"
import {
  TerminalIcon,
  ArrowDownLeftIcon,
  ArrowUpRightIcon,
  InfoIcon,
  ClockIcon,
  PlayIcon,
  SquareIcon,
  AlertTriangleIcon,
} from "lucide-react"

function logIcon(log: AgentLog) {
  if (log.type === "tool_call" || log.type === "tool_result") {
    const integ = integrations.find((i) => i.slug === log.integration)
    if (integ) return <Image src={integ.logo} alt={integ.name} width={16} height={16} className="shrink-0" />
    return <TerminalIcon className="size-4 text-muted-foreground" />
  }
  if (log.type === "channel_in" || log.type === "channel_out") {
    const ch = channels.find((c) => c.slug === log.channel)
    if (ch) return <Image src={ch.logo} alt={ch.name} width={16} height={16} className="shrink-0" />
  }
  if (log.type === "message_in") return <ArrowDownLeftIcon className="size-4 text-blue-500" />
  if (log.type === "message_out") return <ArrowUpRightIcon className="size-4 text-emerald-500" />
  if (log.type === "agent_startup") return <PlayIcon className="size-4 text-emerald-500" />
  if (log.type === "agent_shutdown") return <SquareIcon className="size-4 text-muted-foreground" />
  if (log.type === "error") return <AlertTriangleIcon className="size-4 text-red-500" />
  return <InfoIcon className="size-4 text-muted-foreground" />
}

function typeLabel(log: AgentLog) {
  switch (log.type) {
    case "tool_call": return "Tool call"
    case "tool_result": return "Tool result"
    case "channel_in": return "Channel in"
    case "channel_out": return "Channel out"
    case "message_in": return "Message in"
    case "message_out": return "Message out"
    case "system": return "System"
    case "agent_startup": return "Startup"
    case "agent_shutdown": return "Shutdown"
    case "error": return "Error"
    default: return log.type
  }
}

function formatTime(ts: number) {
  return new Date(ts).toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit", second: "2-digit" })
}

export function LogTable({
  logs,
  showAgent = false,
}: {
  logs: (AgentLog & { agentName?: string })[]
  showAgent?: boolean
}) {
  return (
    <table className="w-full text-sm">
      <thead>
        <tr className="border-b text-left">
          <th className="px-3 py-3 font-medium w-[32px]">Icon</th>
          <th className="px-3 py-3 font-medium">Type</th>
          {showAgent && <th className="px-3 py-3 font-medium">Agent</th>}
          <th className="px-3 py-3 font-medium">Source</th>
          <th className="px-3 py-3 font-medium">Content</th>
          <th className="px-3 py-3 font-medium text-right">Duration</th>
          <th className="px-3 py-3 font-medium text-right">Time</th>
        </tr>
      </thead>
      <tbody>
        {logs.map((log) => (
          <tr key={log.id} className="border-b last:border-0 hover:bg-muted/50 transition-colors">
            <td className="px-3 py-2.5">
              <div className="flex size-6 items-center justify-center">{logIcon(log)}</div>
            </td>
            <td className="px-3 py-2.5">
              <span className="rounded bg-muted px-1.5 py-0.5 text-xs">{typeLabel(log)}</span>
            </td>
            {showAgent && (
              <td className="px-3 py-2.5 text-xs">{log.agentName ?? "—"}</td>
            )}
            <td className="px-3 py-2.5">
              {(log.tool || log.channel) ? (
                <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs">{log.tool ?? log.channel}</code>
              ) : (
                <span className="text-xs text-muted-foreground">—</span>
              )}
            </td>
            <td className="px-3 py-2.5 text-xs max-w-[400px] truncate text-muted-foreground">
              {log.content}
            </td>
            <td className="px-3 py-2.5 text-right">
              {log.durationMs ? (
                <span className="flex items-center justify-end gap-0.5 text-xs text-muted-foreground">
                  <ClockIcon className="size-3" />
                  {log.durationMs}ms
                </span>
              ) : (
                <span className="text-xs text-muted-foreground">—</span>
              )}
            </td>
            <td className="px-3 py-2.5 text-right text-xs text-muted-foreground whitespace-nowrap">
              {formatTime(log.time)}
            </td>
          </tr>
        ))}
        {logs.length === 0 && (
          <tr>
            <td colSpan={showAgent ? 7 : 6} className="px-3 py-8 text-center text-muted-foreground">
              No logs yet.
            </td>
          </tr>
        )}
      </tbody>
    </table>
  )
}
