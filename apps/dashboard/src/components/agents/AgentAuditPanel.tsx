"use client";

import { useState } from "react";
import { ClipboardList, ChevronLeft, ChevronRight } from "lucide-react";
import type { Agent } from "@/types/agent";
import { useAgentAuditLog } from "@/hooks/useAgentAuditLog";
import { Button } from "@/components/ui/button";

function relativeTime(timestamp: string): string {
  const diff = Date.now() - new Date(timestamp).getTime();
  const secs = Math.floor(diff / 1000);
  if (secs < 60) return `${secs}s ago`;
  const mins = Math.floor(secs / 60);
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.floor(hrs / 24)}d ago`;
}

const PAGE_SIZE = 20;

export function AgentAuditPanel({ agent }: { agent: Agent }) {
  const { entries, total, loading, error, page, setPage } = useAgentAuditLog(agent.id);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  function toggleRow(id: string) {
    setExpandedId((prev) => (prev === id ? null : id));
  }

  if (loading) {
    return (
      <div className="flex h-40 items-center justify-center text-sm text-muted-foreground">
        Loading…
      </div>
    );
  }

  if (error) {
    return (
      <div className="px-6 py-4 text-sm text-destructive">{error}</div>
    );
  }

  if (entries.length === 0) {
    return (
      <div className="flex h-40 flex-col items-center justify-center gap-2 text-sm text-muted-foreground">
        <ClipboardList className="h-8 w-8" />
        No tool calls recorded yet
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4 px-6 py-4">
      <div className="overflow-hidden rounded-md border border-border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border bg-muted/50 text-left text-xs text-muted-foreground">
              <th className="px-4 py-2 font-medium">Time</th>
              <th className="px-4 py-2 font-medium">Skill</th>
              <th className="px-4 py-2 font-medium">Action</th>
              <th className="px-4 py-2 font-medium">Result</th>
              <th className="px-4 py-2 font-medium">Duration</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((entry) => {
              const isExpanded = expandedId === entry.id;
              const isOk = entry.resultSummary !== "error";
              return (
                <>
                  <tr
                    key={entry.id}
                    onClick={() => toggleRow(entry.id)}
                    className="cursor-pointer border-b border-border last:border-0 hover:bg-muted/40 transition-colors"
                  >
                    <td className="px-4 py-2 font-mono text-xs text-muted-foreground">
                      {relativeTime(entry.timestamp)}
                    </td>
                    <td className="px-4 py-2">{entry.skillName}</td>
                    <td className="px-4 py-2 text-muted-foreground">{entry.action}</td>
                    <td className="px-4 py-2">
                      {entry.resultSummary === null ? (
                        <span className="text-muted-foreground">—</span>
                      ) : entry.resultSummary === "error" ? (
                        <span className="rounded border border-red-200 bg-destructive/10 px-1.5 py-0.5 text-xs text-destructive">
                          error
                        </span>
                      ) : (
                        <span className="rounded border border-emerald-200 bg-emerald-50 px-1.5 py-0.5 text-xs text-emerald-700">
                          ok
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-2 font-mono text-xs text-muted-foreground">
                      {entry.durationMs}ms
                    </td>
                  </tr>
                  {isExpanded && (
                    <tr key={`${entry.id}-expanded`} className="border-b border-border last:border-0 bg-muted/20">
                      <td colSpan={5} className="px-4 py-3">
                        <div className="text-xs text-muted-foreground mb-1 font-medium">Params</div>
                        <pre className="rounded bg-muted px-3 py-2 text-xs overflow-x-auto whitespace-pre-wrap break-words">
                          {(() => {
                            try {
                              return JSON.stringify(JSON.parse(entry.paramsJson), null, 2);
                            } catch {
                              return entry.paramsJson;
                            }
                          })()}
                        </pre>
                      </td>
                    </tr>
                  )}
                </>
              );
            })}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <span>
            Page {page + 1} of {totalPages} ({total} total)
          </span>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={page === 0}
              onClick={() => setPage((p) => p - 1)}
            >
              <ChevronLeft className="h-4 w-4" />
              Prev
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={page >= totalPages - 1}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
