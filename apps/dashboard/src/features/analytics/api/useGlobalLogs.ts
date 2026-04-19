"use client"

import { gql, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"
import { mockAgentLogs } from "../data/analytics-mock"
import type { AgentLog } from "@/types/logs"

export type GlobalLogFilters = {
  search?: string
  agentName?: string
  type?: string
  skip?: number
  limit?: number
}

export type GlobalLog = AgentLog & { agentName: string }

const GLOBAL_LOGS_QUERY = gql`
  query GlobalLogs(
    $search: String
    $agentName: String
    $type: AgentLogType
    $skip: Int
    $limit: Int
  ) {
    globalLogs(
      filters: {
        search: $search
        agentName: $agentName
        type: $type
        skip: $skip
        limit: $limit
      }
    ) {
      items {
        id
        time
        type
        tool
        integration
        channel
        content
        durationMs
        inputTokens
        outputTokens
        agentName
      }
      total
    }
  }
`

function normaliseType(raw: string | null | undefined): AgentLog["type"] {
  if (!raw) return "system"
  if (raw.includes("_")) return raw as AgentLog["type"]
  const map: Record<string, AgentLog["type"]> = {
    ToolCall: "tool_call",
    ToolResult: "tool_result",
    MessageIn: "message_in",
    MessageOut: "message_out",
    ChannelIn: "channel_in",
    ChannelOut: "channel_out",
    System: "system",
    AgentStartup: "agent_startup",
    AgentShutdown: "agent_shutdown",
    Error: "error",
  }
  return map[raw] ?? "system"
}

// Same mock aggregation that used to live inline in logs/page.tsx.
const mockGlobal: GlobalLog[] = [
  ...mockAgentLogs.map((l) => ({ ...l, agentName: "Research Assistant" })),
  ...mockAgentLogs.slice(0, 5).map((l, i) => ({
    ...l,
    id: `log_code_${i}`,
    time: l.time - 300000,
    agentName: "Code Reviewer",
  })),
  ...mockAgentLogs.slice(0, 3).map((l, i) => ({
    ...l,
    id: `log_support_${i}`,
    time: l.time - 600000,
    agentName: "Customer Support Bot",
  })),
].sort((a, b) => b.time - a.time)

export function useGlobalLogs(filters: GlobalLogFilters = {}): {
  logs: GlobalLog[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(GLOBAL_LOGS_QUERY, {
    variables: filters,
    skip: USE_MOCKS,
    pollInterval: USE_MOCKS ? 0 : 5000,
    fetchPolicy: "network-only",
  })
  if (USE_MOCKS) return { logs: mockGlobal, loading: false }
  const raw: Array<{
    id: string
    time: string | number
    type: string
    tool?: string | null
    integration?: string | null
    channel?: string | null
    content: string
    durationMs?: number | null
    inputTokens?: number | null
    outputTokens?: number | null
    agentName: string
  }> = data?.globalLogs?.items ?? []
  const logs: GlobalLog[] = raw.map((r) => ({
    id: r.id,
    time:
      typeof r.time === "number" ? r.time : Date.parse(r.time) || Date.now(),
    type: normaliseType(r.type),
    tool: r.tool ?? undefined,
    integration: r.integration ?? undefined,
    channel: r.channel ?? undefined,
    content: r.content,
    durationMs: r.durationMs ?? undefined,
    tokens:
      r.inputTokens != null || r.outputTokens != null
        ? { input: r.inputTokens ?? 0, output: r.outputTokens ?? 0 }
        : undefined,
    agentName: r.agentName,
  }))
  return { logs, loading, error: error ?? undefined }
}
