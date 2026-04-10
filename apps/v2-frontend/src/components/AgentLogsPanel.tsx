"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { Activity, Pause, Play, ArrowDown, Filter } from "lucide-react";
import type { Agent } from "@/hooks/useAgents";
import { agentFetch, agentProxyUrl } from "@/lib/agentProxy";

type SSEEvent = {
  type: string;
  timestamp?: string;
  [key: string]: unknown;
};

type HistoryResponse = { events?: SSEEvent[] };

type LogEntry = { id: string; event: SSEEvent };

const MAX_ENTRIES = 500;

function formatTs(ts?: string): string {
  if (!ts) return new Date().toLocaleTimeString();
  return new Date(ts).toLocaleTimeString();
}

function typeColor(type: string): string {
  switch (type.toLowerCase()) {
    case "error":
      return "border-red-500/40 bg-red-500/10 text-red-300";
    case "warn":
    case "warning":
      return "border-yellow-500/40 bg-yellow-500/10 text-yellow-300";
    case "tool_call":
    case "tool_result":
    case "tool_call_start":
      return "border-purple-500/40 bg-purple-500/10 text-purple-300";
    case "llm_request":
      return "border-sky-500/40 bg-sky-500/10 text-sky-300";
    case "agent_start":
    case "agent_end":
      return "border-emerald-500/40 bg-emerald-500/10 text-emerald-300";
    default:
      return "border-[var(--eaos-border)] bg-black/30 text-[var(--eaos-text-muted)]";
  }
}

export function AgentLogsPanel({ agent }: { agent: Agent }) {
  const [entries, setEntries] = useState<LogEntry[]>([]);
  const [paused, setPaused] = useState(false);
  const [connected, setConnected] = useState(false);
  const [autoScroll, setAutoScroll] = useState(true);
  const [typeFilters, setTypeFilters] = useState<Set<string>>(new Set());
  const [error, setError] = useState<string | null>(null);

  const [reconnecting, setReconnecting] = useState(false);

  const containerRef = useRef<HTMLDivElement | null>(null);
  const pausedRef = useRef(false);
  const entryIdRef = useRef(0);
  const esRef = useRef<EventSource | null>(null);
  const reconnectTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const reconnectDelayRef = useRef(1000);
  const retriesRef = useRef(0);
  const MAX_RECONNECT_DELAY = 30_000;
  const MAX_RETRIES = 10;

  useEffect(() => {
    pausedRef.current = paused;
  }, [paused]);

  useEffect(() => {
    let cancelled = false;

    // Best-effort history load.
    agentFetch<HistoryResponse>(agent.id, "/api/events/history")
      .then((res) => {
        if (cancelled) return;
        const events = res.events ?? [];
        if (events.length === 0) return;
        setEntries((prev) => {
          const fresh: LogEntry[] = events.map((evt, i) => ({
            id: `hist-${i}`,
            event: { ...evt, type: evt.type ?? "unknown" },
          }));
          entryIdRef.current += events.length;
          const merged = [...fresh, ...prev];
          return merged.length > MAX_ENTRIES ? merged.slice(-MAX_ENTRIES) : merged;
        });
      })
      .catch(() => {
        /* history is best-effort */
      });

    // Live stream via SSE through the backend proxy, with reconnect.
    reconnectDelayRef.current = 1000;
    retriesRef.current = 0;

    function connectSSE() {
      const url = agentProxyUrl(agent.id, "/api/events");
      const es = new EventSource(url);
      esRef.current = es;

      es.onopen = () => {
        setConnected(true);
        setReconnecting(false);
        setError(null);
        reconnectDelayRef.current = 1000;
        retriesRef.current = 0;
      };
      es.onerror = () => {
        setConnected(false);
        es.close();
        esRef.current = null;
        if (cancelled) return;
        if (retriesRef.current < MAX_RETRIES) {
          setReconnecting(true);
          const delay = reconnectDelayRef.current;
          reconnectTimerRef.current = setTimeout(() => {
            reconnectDelayRef.current = Math.min(delay * 2, MAX_RECONNECT_DELAY);
            retriesRef.current += 1;
            connectSSE();
          }, delay);
        } else {
          setReconnecting(false);
          setError("Log stream lost. Refresh to reconnect.");
        }
      };
      es.onmessage = (ev) => {
        if (pausedRef.current) return;
        try {
          const parsed = JSON.parse(ev.data) as SSEEvent;
          entryIdRef.current += 1;
          const entry: LogEntry = {
            id: `log-${entryIdRef.current}`,
            event: { ...parsed, type: parsed.type ?? "unknown" },
          };
          setEntries((prev) => {
            const next = [...prev, entry];
            return next.length > MAX_ENTRIES ? next.slice(-MAX_ENTRIES) : next;
          });
        } catch {
          /* non-JSON frame */
        }
      };
    }

    connectSSE();

    return () => {
      cancelled = true;
      if (reconnectTimerRef.current) clearTimeout(reconnectTimerRef.current);
      if (esRef.current) {
        esRef.current.close();
        esRef.current = null;
      }
    };
  }, [agent.id]);

  useEffect(() => {
    if (autoScroll && containerRef.current) {
      containerRef.current.scrollTop = containerRef.current.scrollHeight;
    }
  }, [entries, autoScroll]);

  const handleScroll = useCallback(() => {
    if (!containerRef.current) return;
    const { scrollTop, scrollHeight, clientHeight } = containerRef.current;
    setAutoScroll(scrollHeight - scrollTop - clientHeight < 50);
  }, []);

  const allTypes = Array.from(new Set(entries.map((e) => e.event.type))).sort();
  const filtered =
    typeFilters.size === 0 ? entries : entries.filter((e) => typeFilters.has(e.event.type));

  const toggleType = (type: string) => {
    setTypeFilters((prev) => {
      const next = new Set(prev);
      if (next.has(type)) next.delete(type);
      else next.add(type);
      return next;
    });
  };

  const jumpToBottom = () => {
    if (containerRef.current) {
      containerRef.current.scrollTop = containerRef.current.scrollHeight;
    }
    setAutoScroll(true);
  };

  return (
    <div className="mx-8 my-6 flex h-[calc(100vh-260px)] flex-col rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)]">
      <div className="flex items-center justify-between border-b border-[var(--eaos-border)] px-4 py-2 text-xs">
        <div className="flex items-center gap-2">
          <Activity className="h-4 w-4" />
          <span className="font-medium uppercase tracking-wider">Live logs</span>
          <span className="font-mono text-[10px] text-[var(--eaos-text-muted)]">
            {filtered.length} events
          </span>
        </div>
        <div className="flex items-center gap-2">
          <span
            className={
              connected
                ? "rounded-full border border-emerald-500/40 px-2 py-0.5 text-emerald-300"
                : reconnecting
                  ? "rounded-full border border-yellow-500/40 px-2 py-0.5 text-yellow-300"
                  : "rounded-full border border-[var(--eaos-border)] px-2 py-0.5 text-[var(--eaos-text-muted)]"
            }
          >
            {connected ? "streaming" : reconnecting ? "reconnecting…" : "disconnected"}
          </span>
          <button
            type="button"
            onClick={() => setPaused((p) => !p)}
            className="flex items-center gap-1 rounded-md border border-[var(--eaos-border)] px-2 py-1 hover:bg-white hover:text-black"
          >
            {paused ? <Play className="h-3 w-3" /> : <Pause className="h-3 w-3" />}
            {paused ? "Resume" : "Pause"}
          </button>
          {!autoScroll && (
            <button
              type="button"
              onClick={jumpToBottom}
              className="flex items-center gap-1 rounded-md border border-[var(--eaos-border)] px-2 py-1 hover:bg-white hover:text-black"
            >
              <ArrowDown className="h-3 w-3" />
              Jump
            </button>
          )}
        </div>
      </div>

      {allTypes.length > 0 && (
        <div className="flex items-center gap-2 overflow-x-auto border-b border-[var(--eaos-border)] px-4 py-2 text-[11px]">
          <Filter className="h-3 w-3 flex-shrink-0 text-[var(--eaos-text-muted)]" />
          <span className="flex-shrink-0 uppercase tracking-wider text-[var(--eaos-text-muted)]">
            Filter:
          </span>
          {allTypes.map((type) => {
            const active = typeFilters.has(type);
            return (
              <button
                key={type}
                type="button"
                onClick={() => toggleType(type)}
                className={`flex-shrink-0 rounded border px-2 py-0.5 transition-colors ${
                  active
                    ? "border-white bg-white text-black"
                    : "border-[var(--eaos-border)] text-[var(--eaos-text-muted)] hover:text-white"
                }`}
              >
                {type}
              </button>
            );
          })}
          {typeFilters.size > 0 && (
            <button
              type="button"
              onClick={() => setTypeFilters(new Set())}
              className="flex-shrink-0 text-[var(--eaos-text-muted)] hover:text-white"
            >
              clear
            </button>
          )}
        </div>
      )}

      {error && (
        <div className="border-b border-red-500/30 bg-red-500/10 px-4 py-2 text-xs text-red-300">
          {error}
        </div>
      )}

      <div
        ref={containerRef}
        onScroll={handleScroll}
        className="min-h-0 flex-1 space-y-1.5 overflow-y-auto p-3"
      >
        {filtered.length === 0 ? (
          <div className="flex h-full flex-col items-center justify-center text-sm text-[var(--eaos-text-muted)]">
            <Activity className="mb-2 h-8 w-8" />
            {paused ? "Stream paused." : "Waiting for events…"}
          </div>
        ) : (
          filtered.map((entry) => {
            const { event } = entry;
            const detail =
              (event as { message?: string }).message ??
              (event as { content?: string }).content ??
              (event as { data?: unknown }).data ??
              JSON.stringify(
                Object.fromEntries(
                  Object.entries(event).filter(
                    ([k]) => k !== "type" && k !== "timestamp",
                  ),
                ),
              );
            return (
              <div
                key={entry.id}
                className="flex items-start gap-3 rounded-md border border-[var(--eaos-border)] bg-black/20 px-3 py-2 text-xs"
              >
                <span className="flex-shrink-0 font-mono text-[10px] text-[var(--eaos-text-muted)]">
                  {formatTs(event.timestamp)}
                </span>
                <span
                  className={`flex-shrink-0 rounded border px-1.5 py-0.5 font-mono text-[10px] capitalize ${typeColor(event.type)}`}
                >
                  {event.type}
                </span>
                <p className="min-w-0 break-words text-[var(--eaos-text-muted)]">
                  {typeof detail === "string" ? detail : JSON.stringify(detail)}
                </p>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}
