import { useMemo, useState } from "react";
import { useAgentLogs } from "@/features/analytics/api/useAgentLogs";
import type { AgentLog } from "@/types/logs";

export const AGENT_LOG_TYPES = [
  "All",
  "tool_call",
  "tool_result",
  "channel_in",
  "channel_out",
  "message_in",
  "message_out",
  "system",
] as const;

export function useAgentLogTimeline({
  agentId,
  pendingTurnStartedAt = null,
}: {
  agentId: string;
  pendingTurnStartedAt?: number | null;
}) {
  const { logs, loading } = useAgentLogs(agentId);
  const [search, setSearchValue] = useState("");
  const [typeFilter, setTypeFilterValue] = useState("All");
  const [selectedLogId, setSelectedLogId] = useState<string | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);
  const [debugLogs, setDebugLogsValue] = useState(false);

  const filtered = useMemo(() => {
    return logs.filter((log) => {
      if (!debugLogs && isDebugLog(log)) return false;
      const query = search.toLowerCase();
      if (
        query &&
        !log.content.toLowerCase().includes(query) &&
        !(log.tool ?? "").toLowerCase().includes(query) &&
        !(log.channel ?? "").toLowerCase().includes(query) &&
        !(log.integration ?? "").toLowerCase().includes(query)
      ) {
        return false;
      }
      if (typeFilter !== "All" && log.type !== typeFilter) return false;
      return true;
    });
  }, [logs, search, typeFilter, debugLogs]);

  const agentThinking = useMemo(
    () => isAgentThinking(logs, pendingTurnStartedAt),
    [logs, pendingTurnStartedAt],
  );
  const showThinkingRow =
    agentThinking && search.trim() === "" && typeFilter === "All";
  const effectiveSelectedLogId = detailOpen ? selectedLogId : null;
  const selectedLog = effectiveSelectedLogId
    ? (filtered.find((log) => log.id === effectiveSelectedLogId) ?? null)
    : null;

  const resetLiveState = () => {
    setSelectedLogId(null);
    setDetailOpen(false);
  };

  return {
    logs,
    loading,
    search,
    typeFilter,
    debugLogs,
    selectedLog,
    selectedLogId: effectiveSelectedLogId,
    visibleLogs: filtered,
    showThinkingRow,
    setSearch(value: string) {
      setSearchValue(value);
      resetLiveState();
    },
    setTypeFilter(value: string) {
      setTypeFilterValue(value);
      resetLiveState();
    },
    setDebugLogs(checked: boolean) {
      setDebugLogsValue(checked);
      resetLiveState();
    },
    selectLog(log: AgentLog) {
      setSelectedLogId(log.id);
      setDetailOpen(true);
    },
    closeDetail() {
      setDetailOpen(false);
    },
  };
}

function isAgentThinking(logs: AgentLog[], pendingTurnStartedAt: number | null) {
  const latestStart = logs.reduce<number | null>((latest, log) => {
    if (!isTurnStart(log)) return latest;
    return latest === null || log.time > latest ? log.time : latest;
  }, pendingTurnStartedAt);

  if (latestStart === null) return false;

  return !logs.some((log) => log.time >= latestStart && isTurnTerminal(log));
}

function isTurnStart(log: AgentLog) {
  return (
    log.type === "message_in" ||
    log.type === "channel_in" ||
    (log.type === "system" && log.content.startsWith("Turn started:"))
  );
}

function isTurnTerminal(log: AgentLog) {
  return (
    log.type === "message_out" ||
    log.type === "error" ||
    (log.type === "system" && log.content.startsWith("Turn complete:"))
  );
}

function isDebugLog(log: AgentLog) {
  if (log.type !== "system") return false;
  return (
    log.content.startsWith("Turn setup:") ||
    log.content.startsWith("Tool setup:") ||
    log.content.startsWith("LLM iteration ") ||
    (log.content.startsWith("Iteration ") &&
      (log.content.includes(": billing check complete") ||
        log.content.includes(": billing record complete")))
  );
}
