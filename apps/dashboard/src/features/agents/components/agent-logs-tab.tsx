"use client";

import { useState, useRef, useEffect, useCallback } from "react";
import { LogDetailPanel } from "@/components/log-detail-panel";
import { LogTable } from "@/components/log-table";
import { useAgentLogs } from "@/features/analytics/api/useAgentLogs";

export function AgentLogsTab({ agentId }: { agentId: string }) {
  const { logs, loading } = useAgentLogs(agentId);
  const [selectedLogId, setSelectedLogId] = useState<string | null>(null);
  const selectedLog = logs.find((log) => log.id === selectedLogId) ?? null;
  const scrollRef = useRef<HTMLDivElement>(null);
  const isNearBottom = useRef(true);

  const handleScroll = useCallback(() => {
    const el = scrollRef.current;
    if (!el) return;
    isNearBottom.current =
      el.scrollHeight - el.scrollTop - el.clientHeight < 80;
  }, []);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [loading]);

  useEffect(() => {
    if (isNearBottom.current && scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [logs.length]);

  return (
    <div className="grid min-h-0 flex-1 grid-cols-[minmax(0,1fr)_360px] pt-4">
      <section className="flex min-h-0 min-w-0 flex-col overflow-hidden border border-border bg-background">
        <div className="flex min-h-14 shrink-0 items-center justify-between border-b border-border px-4 py-2">
          <div>
            <h2 className="text-sm font-semibold">Logs</h2>
            <p className="mt-0.5 text-xs text-muted-foreground">
              {loading ? "Loading" : `${logs.length} events`}
            </p>
          </div>
        </div>
        <div
          ref={scrollRef}
          onScroll={handleScroll}
          className="min-h-0 flex-1 overflow-auto"
        >
          <LogTable
            logs={logs}
            selectedLogId={selectedLogId}
            showSelectionColumn={false}
            onSelectLog={(log) =>
              setSelectedLogId(selectedLogId === log.id ? null : log.id)
            }
          />
        </div>
      </section>
      <LogDetailPanel log={selectedLog} />
    </div>
  );
}
