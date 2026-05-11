"use client"

import { gql, useQuery, useSubscription } from "@apollo/client"
import type { AgentLog } from "@/features/analytics"

const AGENT_LOGS_QUERY = gql`
  query AgentLogs($agentId: UUID!, $last: Int!) {
    agentLogs(agentId: $agentId, last: $last) {
      nodes {
        id
        time
        type
        tool
        integration
        channel
        channelConnectionId
        content
        durationMs
        inputTokens
        outputTokens
        correlationId
      }
    }
  }
`

const AGENT_LOG_APPENDED_SUBSCRIPTION = gql`
  subscription AgentLogAppended($agentId: UUID!) {
    agentLogAppended(agentId: $agentId) {
      id
      time
      type
      tool
      integration
      channel
      channelConnectionId
      content
      durationMs
      inputTokens
      outputTokens
      correlationId
    }
  }
`

type RawAgentLog = {
  id: string
  time: string | number
  type: string
  tool?: string | null
  integration?: string | null
  channel?: string | null
  channelConnectionId?: string | null
  content: string
  durationMs?: number | null
  inputTokens?: number | null
  outputTokens?: number | null
  correlationId?: string | null
}

function normaliseType(raw: string | null | undefined): AgentLog["type"] {
  if (!raw) return "system"
  const v = raw.toString()
  if (v.includes("_")) return v as AgentLog["type"]
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
  return map[v] ?? "system"
}

function toMillis(time: string | number) {
  if (typeof time === "number") return time
  const parsed = Date.parse(time)
  return Number.isFinite(parsed) ? parsed : 0
}

function toAgentLog(raw: RawAgentLog): AgentLog {
  const time = toMillis(raw.time)
  return {
    id: raw.id,
    time,
    type: normaliseType(raw.type),
    tool: raw.tool ?? undefined,
    integration: raw.integration ?? undefined,
    channel: raw.channel ?? undefined,
    channelConnectionId: raw.channelConnectionId ?? undefined,
    content: raw.content,
    durationMs: raw.durationMs ?? undefined,
    tokens:
      raw.inputTokens != null || raw.outputTokens != null
        ? { input: raw.inputTokens ?? 0, output: raw.outputTokens ?? 0 }
        : undefined,
    correlationId: raw.correlationId ?? undefined,
  }
}

export function useAgentLogs(
  agentId: string,
  limit = 200,
): { logs: AgentLog[]; loading: boolean; error?: Error } {
  const { data, loading, error } = useQuery(AGENT_LOGS_QUERY, {
    variables: { agentId, last: limit },
    skip: !agentId,
    fetchPolicy: "network-only",
    pollInterval: 5000,
  })

  useSubscription(AGENT_LOG_APPENDED_SUBSCRIPTION, {
    variables: { agentId },
    skip: !agentId,
    onData: ({ client, data: subscriptionData }) => {
      const appended = subscriptionData.data?.agentLogAppended as
        | RawAgentLog
        | undefined
      if (!appended) return

      client.cache.updateQuery(
        { query: AGENT_LOGS_QUERY, variables: { agentId, last: limit } },
        (old: { agentLogs?: { nodes?: RawAgentLog[] } } | null) => {
          const existing = old?.agentLogs?.nodes ?? []
          if (existing.some((log) => log.id === appended.id)) return old

          const next = [...existing, appended]
            .sort((a, b) => toMillis(a.time) - toMillis(b.time))
            .slice(-limit)

          return { agentLogs: { ...old?.agentLogs, nodes: next } }
        },
      )
    },
  })

  const raw: RawAgentLog[] = data?.agentLogs?.nodes ?? []
  const logs: AgentLog[] = raw.map(toAgentLog)
  return { logs, loading, error: error ?? undefined }
}
