"use client";

import { gql, useQuery } from "@apollo/client";
import type { AgentLog } from "@/types/logs";

export type GlobalLogFilters = {
  search?: string;
  agentName?: string;
  type?: string;
  skip?: number;
  limit?: number;
};

export type GlobalLog = AgentLog & { agentName: string };

const GLOBAL_LOGS_QUERY = gql`
  query GlobalLogs(
    $search: String
    $agentName: String
    $type: AgentLogType
    $first: Int
  ) {
    globalLogs(
      first: $first
      filters: {
        search: $search
        agentName: $agentName
        type: $type
      }
    ) {
      nodes {
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
      totalCount
    }
  }
`;

function normaliseType(raw: string | null | undefined): AgentLog["type"] {
  if (!raw) return "system";
  if (raw.includes("_")) return raw as AgentLog["type"];
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
  };
  return map[raw] ?? "system";
}

export function useGlobalLogs(filters: GlobalLogFilters = {}): {
  logs: GlobalLog[];
  loading: boolean;
  error?: Error;
} {
  const filterVariables = {
    search: filters.search,
    agentName: filters.agentName,
    type: filters.type,
  };
  const { data, loading, error } = useQuery(GLOBAL_LOGS_QUERY, {
    variables: { ...filterVariables, first: filters.limit ?? 50 },
    pollInterval: 5000,
    fetchPolicy: "network-only",
  });
  const raw: Array<{
    id: string;
    time: string | number;
    type: string;
    tool?: string | null;
    integration?: string | null;
    channel?: string | null;
    content: string;
    durationMs?: number | null;
    inputTokens?: number | null;
    outputTokens?: number | null;
    agentName: string;
  }> = data?.globalLogs?.nodes ?? [];
  const logs: GlobalLog[] = raw.map((r) => ({
    id: r.id,
    time:
      typeof r.time === "number" ? r.time : Date.parse(r.time) || 0,
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
  }));
  return { logs, loading, error: error ?? undefined };
}
