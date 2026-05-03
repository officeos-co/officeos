"use client";

import { useMemo, useState, type ReactNode } from "react";
import { LogDetailPanel } from "@/components/log-detail-panel";
import { LogTable } from "@/components/log-table";
import { DataPagination } from "@/components/ui/data-pagination";
import { SearchInput } from "@/components/ui/search-input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useAgentLogs } from "@/features/analytics/api/useAgentLogs";

const ALL_TYPES = [
  "All",
  "tool_call",
  "tool_result",
  "channel_in",
  "channel_out",
  "message_in",
  "message_out",
  "system",
] as const;
const PAGE_SIZES = [10, 25, 50] as const;

export function AgentLogsTab({
  agentId,
  composer,
}: {
  agentId: string;
  composer?: ReactNode;
}) {
  const { logs, loading } = useAgentLogs(agentId);
  const [search, setSearch] = useState("");
  const [typeFilter, setTypeFilter] = useState("All");
  const [pageSize, setPageSize] = useState<number>(25);
  const [page, setPage] = useState(0);
  const [selectedLogId, setSelectedLogId] = useState<string | null>(null);
  const [detailOpen, setDetailOpen] = useState(true);

  const filtered = useMemo(() => {
    return logs.filter((log) => {
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
  }, [logs, search, typeFilter]);

  const paged = filtered.slice(page * pageSize, (page + 1) * pageSize);
  const latestLogId = logs[logs.length - 1]?.id ?? null;
  const effectiveSelectedLogId = detailOpen
    ? (selectedLogId ?? latestLogId)
    : null;
  const selectedLog = effectiveSelectedLogId
    ? (logs.find((log) => log.id === effectiveSelectedLogId) ?? null)
    : null;

  return (
    <div
      className={
        selectedLog
          ? "grid h-full min-h-0 flex-1 overflow-hidden grid-cols-[minmax(0,1fr)_clamp(360px,42vw,560px)]"
          : "flex h-full min-h-0 flex-1 flex-col overflow-hidden"
      }
    >
      <div className="flex min-h-0 min-w-0 flex-col overflow-hidden">
        <section className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden bg-background pt-4">
          <div className="flex min-h-14 shrink-0 items-center justify-between gap-3 py-2">
            <div className="flex items-center gap-2">
              <SearchInput
                placeholder="Search logs..."
                value={search}
                onChange={(value) => {
                  setSearch(value);
                  setPage(0);
                }}
              />
              <Select
                value={typeFilter}
                onValueChange={(value) => {
                  if (value) {
                    setTypeFilter(value);
                    setPage(0);
                  }
                }}
              >
                <SelectTrigger className="w-[150px]">
                  <SelectValue placeholder="Type" />
                </SelectTrigger>
                <SelectContent>
                  {ALL_TYPES.map((type) => (
                    <SelectItem key={type} value={type}>
                      {type === "All" ? "All types" : type.replace("_", " ")}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="min-h-0 flex-1 overflow-y-scroll pr-4 [scrollbar-gutter:stable]">
            <LogTable
              logs={paged}
              selectedLogId={effectiveSelectedLogId}
              showSelectionColumn={false}
              loading={loading && logs.length === 0}
              skeletonRows={10}
              className="[&_tr]:border-0"
              onSelectLog={(log) => {
                setSelectedLogId(log.id);
                setDetailOpen(true);
              }}
            />
            <div className="py-2">
              <DataPagination
                page={page}
                pageSize={pageSize}
                total={filtered.length}
                pageSizes={PAGE_SIZES}
                onPageChange={setPage}
                onPageSizeChange={(size) => {
                  setPageSize(size);
                  setPage(0);
                }}
              />
            </div>
          </div>
        </section>

        {composer && (
          <div className="shrink-0 border-t border-border bg-background/80 p-3 backdrop-blur-sm">
            {composer}
          </div>
        )}
      </div>

      {selectedLog && (
        <div className="h-full min-h-0 overflow-hidden border-l border-border">
          <LogDetailPanel
            log={selectedLog}
            onClose={() => setDetailOpen(false)}
            className="w-full border-l-0"
          />
        </div>
      )}
    </div>
  );
}
