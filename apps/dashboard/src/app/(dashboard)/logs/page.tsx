"use client"

import { useState, useMemo } from "react"
import type { AgentLog } from "@/types/logs"
import { PageHeader } from "@/components/page-header"
import { LogTable } from "@/components/log-table"
import { Button } from "@/components/ui/button"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { SearchInput } from "@/components/ui/search-input"
import { DataPagination } from "@/components/ui/data-pagination"
import { useGlobalLogs } from "@/features/analytics"
import { DownloadIcon } from "lucide-react"

const ALL_TYPES = ["All", "tool_call", "tool_result", "channel_in", "channel_out", "message_in", "message_out", "system"] as const
const PAGE_SIZES = [10, 25, 50] as const

function LogDetailPanel({ log }: { log: AgentLog & { agentName?: string } | null }) {
  if (!log) {
    return (
      <div className="flex items-center justify-center h-full text-sm text-muted-foreground">
        Select a log entry to view details
      </div>
    )
  }

  return (
    <div className="px-4 py-3 space-y-3 text-sm">
      <div className="grid grid-cols-[100px_1fr] gap-y-2 gap-x-3">
        <span className="text-muted-foreground">Type</span>
        <span className="rounded bg-muted px-1.5 py-0.5 text-xs w-fit">
          {log.type.replace(/_/g, " ")}
        </span>

        <span className="text-muted-foreground">Time</span>
        <span className="text-xs">{new Date(log.time).toLocaleString()}</span>

        {log.agentName && (
          <>
            <span className="text-muted-foreground">Agent</span>
            <span className="text-xs">{log.agentName}</span>
          </>
        )}

        {log.tool && (
          <>
            <span className="text-muted-foreground">Tool</span>
            <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs w-fit">{log.tool}</code>
          </>
        )}

        {log.channel && (
          <>
            <span className="text-muted-foreground">Channel</span>
            <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs w-fit">{log.channel}</code>
          </>
        )}

        {log.integration && (
          <>
            <span className="text-muted-foreground">Integration</span>
            <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs w-fit">{log.integration}</code>
          </>
        )}

        {log.durationMs != null && (
          <>
            <span className="text-muted-foreground">Duration</span>
            <span className="text-xs">{log.durationMs}ms</span>
          </>
        )}

        {log.tokens && (
          <>
            <span className="text-muted-foreground">Tokens</span>
            <span className="text-xs">{log.tokens.input} in / {log.tokens.output} out</span>
          </>
        )}
      </div>

      <div className="pt-2 border-t">
        <span className="text-xs font-medium text-muted-foreground">Content</span>
        <pre className="mt-1.5 rounded bg-muted p-3 text-xs whitespace-pre-wrap break-words font-mono">
          {log.content}
        </pre>
      </div>
    </div>
  )
}

export default function LogsPage() {
  const { logs: allLogs } = useGlobalLogs()
  const ALL_AGENTS = ["All", ...Array.from(new Set(allLogs.map((l) => l.agentName)))]
  const [search, setSearch] = useState("")
  const [typeFilter, setTypeFilter] = useState("All")
  const [agentFilter, setAgentFilter] = useState("All")
  const [pageSize, setPageSize] = useState<number>(25)
  const [page, setPage] = useState(0)
  const [selectedLog, setSelectedLog] = useState<(AgentLog & { agentName?: string }) | null>(null)

  const filtered = useMemo(() => {
    return allLogs.filter((l) => {
      if (search && !l.content.toLowerCase().includes(search.toLowerCase()) && !(l.tool ?? "").includes(search)) return false
      if (typeFilter !== "All" && l.type !== typeFilter) return false
      if (agentFilter !== "All" && l.agentName !== agentFilter) return false
      return true
    })
  }, [search, typeFilter, agentFilter, allLogs])

  const paged = filtered.slice(page * pageSize, (page + 1) * pageSize)

  return (
    <>
      <PageHeader
        group="Analytics"
        page="Logs"
        action={
          <Button variant="outline" size="sm">
            <DownloadIcon />
            Export
          </Button>
        }
      />
      <div className="flex flex-1 gap-3 p-4 pt-0 min-h-0">
        {/* Left — table + toolbar + pagination */}
        <div className="flex flex-col gap-4 flex-1 min-w-0">
          {/* Toolbar */}
          <div className="flex items-center gap-2 flex-wrap">
            <SearchInput
              placeholder="Search logs..."
              value={search}
              onChange={(v) => { setSearch(v); setPage(0) }}
            />
            <Select value={agentFilter} onValueChange={(v) => { if (v) { setAgentFilter(v); setPage(0) } }}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Agent" />
              </SelectTrigger>
              <SelectContent>
                {ALL_AGENTS.map((a) => <SelectItem key={a} value={a}>{a === "All" ? "All agents" : a}</SelectItem>)}
              </SelectContent>
            </Select>
            <Select value={typeFilter} onValueChange={(v) => { if (v) { setTypeFilter(v); setPage(0) } }}>
              <SelectTrigger className="w-[150px]">
                <SelectValue placeholder="Type" />
              </SelectTrigger>
              <SelectContent>
                {ALL_TYPES.map((t) => <SelectItem key={t} value={t}>{t === "All" ? "All types" : t.replace("_", " ")}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>

          {/* Table */}
          <LogTable
            logs={paged}
            showAgent
            selectedLogId={selectedLog?.id}
            onSelectLog={(log) => setSelectedLog(selectedLog?.id === log.id ? null : log)}
          />

          <DataPagination
            page={page}
            pageSize={pageSize}
            total={filtered.length}
            pageSizes={PAGE_SIZES}
            onPageChange={setPage}
            onPageSizeChange={(s) => { setPageSize(s); setPage(0) }}
          />
        </div>

        {/* Right — always-visible detail panel */}
        <div className="w-[340px] shrink-0 border rounded-lg flex flex-col sticky top-0 self-start max-h-[calc(100vh-8rem)] overflow-auto">
          <div className="px-4 py-3 border-b sticky top-0 bg-background z-10">
            <h3 className="text-sm font-semibold">Log detail</h3>
          </div>
          <LogDetailPanel log={selectedLog} />
        </div>
      </div>
    </>
  )
}
