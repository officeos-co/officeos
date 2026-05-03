"use client";

import { useState, useMemo } from "react";
import { PageHeader } from "@/components/page-header";
import { LogTable } from "@/components/log-table";
import { Button } from "@/components/ui/button";
import { WithTooltip } from "@/components/ui/help-tooltip";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { SearchInput } from "@/components/ui/search-input";
import { DataPagination } from "@/components/ui/data-pagination";
import { useGlobalLogs } from "@/features/analytics";
import { DownloadIcon } from "lucide-react";

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

export default function LogsPage() {
  const { logs: allLogs } = useGlobalLogs();
  const ALL_AGENTS = [
    "All",
    ...Array.from(new Set(allLogs.map((l) => l.agentName))),
  ];
  const [search, setSearch] = useState("");
  const [typeFilter, setTypeFilter] = useState("All");
  const [agentFilter, setAgentFilter] = useState("All");
  const [pageSize, setPageSize] = useState<number>(25);
  const [page, setPage] = useState(0);

  const filtered = useMemo(() => {
    return allLogs.filter((l) => {
      if (
        search &&
        !l.content.toLowerCase().includes(search.toLowerCase()) &&
        !(l.tool ?? "").includes(search)
      )
        return false;
      if (typeFilter !== "All" && l.type !== typeFilter) return false;
      if (agentFilter !== "All" && l.agentName !== agentFilter) return false;
      return true;
    });
  }, [search, typeFilter, agentFilter, allLogs]);

  const paged = filtered.slice(page * pageSize, (page + 1) * pageSize);

  return (
    <>
      <PageHeader
        page="Logs"
        contentClassName="max-w-4xl"
        action={
          <WithTooltip tooltip="Export the currently available log data for offline review.">
            <Button variant="outline" size="sm">
              <DownloadIcon />
              Export
            </Button>
          </WithTooltip>
        }
      />
      <div className="flex flex-1 flex-col gap-4 pb-4 max-w-4xl mx-auto w-full">
        <section className="flex min-h-0 min-w-0 flex-col overflow-hidden">
          <div className="flex min-h-14 shrink-0 items-center justify-between gap-3 py-2">
            <div className="flex items-center gap-2">
              <SearchInput
                placeholder="Search logs..."
                value={search}
                onChange={(v) => {
                  setSearch(v);
                  setPage(0);
                }}
              />
              <Select
                value={agentFilter}
                onValueChange={(v) => {
                  if (v) {
                    setAgentFilter(v);
                    setPage(0);
                  }
                }}
              >
                <SelectTrigger className="w-[180px]">
                  <SelectValue placeholder="Agent" />
                </SelectTrigger>
                <SelectContent>
                  {ALL_AGENTS.map((a) => (
                    <SelectItem key={a} value={a}>
                      {a === "All" ? "All agents" : a}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <Select
                value={typeFilter}
                onValueChange={(v) => {
                  if (v) {
                    setTypeFilter(v);
                    setPage(0);
                  }
                }}
              >
                <SelectTrigger className="w-[150px]">
                  <SelectValue placeholder="Type" />
                </SelectTrigger>
                <SelectContent>
                  {ALL_TYPES.map((t) => (
                    <SelectItem key={t} value={t}>
                      {t === "All" ? "All types" : t.replace("_", " ")}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="min-h-0 flex-1 overflow-auto">
            <LogTable logs={paged} showAgent />
          </div>

          <div className="shrink-0 py-2">
            <DataPagination
              page={page}
              pageSize={pageSize}
              total={filtered.length}
              pageSizes={PAGE_SIZES}
              onPageChange={setPage}
              onPageSizeChange={(s) => {
                setPageSize(s);
                setPage(0);
              }}
            />
          </div>
        </section>
      </div>
    </>
  );
}
