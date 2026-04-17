"use client"

import { gql, useQuery, useSubscription, useApolloClient } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"
import { mockAgentLogs } from "../data/analytics-mock"
import type { AgentLog } from "@/types/logs"

const AGENT_LOGS_QUERY = gql`
  query AgentLogs($agentId: UUID!, $limit: Int!) {
    agentLogs(agentId: $agentId, limit: $limit) {
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
    }
  }
`

const AGENT_LOG_SUBSCRIPTION = gql`
  subscription AgentLogAppended($agentId: UUID!) {
    agentLogAppended(agentId: $agentId) {
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
    }
  }
`

/** HotChocolate defaults to PascalCase enum names. The page logic uses the
 *  existing snake_case AgentLog.type union, so we normalise on the hook edge. */
function normaliseType(raw: string | null | undefined): AgentLog["type"] {
  if (!raw) return "system"
  const v = raw.toString()
  // Accept either already-snake_case or PascalCase.
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

function toAgentLog(raw: {
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
}): AgentLog {
  const time =
    typeof raw.time === "number"
      ? raw.time
      : Date.parse(raw.time) || Date.now()
  return {
    id: raw.id,
    time,
    type: normaliseType(raw.type),
    tool: raw.tool ?? undefined,
    integration: raw.integration ?? undefined,
    channel: raw.channel ?? undefined,
    content: raw.content,
    durationMs: raw.durationMs ?? undefined,
    tokens:
      raw.inputTokens != null || raw.outputTokens != null
        ? { input: raw.inputTokens ?? 0, output: raw.outputTokens ?? 0 }
        : undefined,
  }
}

export function useAgentLogs(
  agentId: string,
  limit = 200,
): { logs: AgentLog[]; loading: boolean; error?: Error } {
  const client = useApolloClient()
  const { data, loading, error } = useQuery(AGENT_LOGS_QUERY, {
    variables: { agentId, limit },
    skip: USE_MOCKS || !agentId,
    pollInterval: USE_MOCKS ? 0 : 5000,
  })

  useSubscription(AGENT_LOG_SUBSCRIPTION, {
    variables: { agentId },
    skip: USE_MOCKS || !agentId,
    onData: ({ data: sub }) => {
      const incoming = sub.data?.agentLogAppended
      if (!incoming) return
      client.cache.updateQuery(
        { query: AGENT_LOGS_QUERY, variables: { agentId, limit } },
        (old: { agentLogs?: unknown[] } | null) => ({
          agentLogs: [...((old?.agentLogs as unknown[]) ?? []), incoming],
        }),
      )
    },
  })

  if (USE_MOCKS) return { logs: mockAgentLogs, loading: false }
  const raw: Array<Parameters<typeof toAgentLog>[0]> = data?.agentLogs ?? []
  const logs: AgentLog[] = raw.map(toAgentLog)
  return { logs, loading, error: error ?? undefined }
}
