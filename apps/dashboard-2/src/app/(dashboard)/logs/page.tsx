"use client"

import { useState, useMemo } from "react"
import { PageHeader } from "@/components/page-header"
import { LogTable } from "@/components/log-table"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { mockAgentLogs } from "@/data/agent-mock"
import type { AgentLog } from "@/data/agent-mock"
import {
  SearchIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  DownloadIcon,
} from "lucide-react"

// Aggregate logs from all agents (mock: duplicate with different agent names)
const allLogs: (AgentLog & { agentName: string })[] = [
  ...mockAgentLogs.map((l) => ({ ...l, agentName: "Research Assistant" })),
  ...mockAgentLogs.slice(0, 5).map((l, i) => ({
    ...l,
    id: `log_code_${i}`,
    time: l.time - 300000,
    agentName: "Code Reviewer",
  })),
  ...mockAgentLogs.slice(0, 3).map((l, i) => ({
    ...l,
    id: `log_support_${i}`,
    time: l.time - 600000,
    agentName: "Customer Support Bot",
  })),
].sort((a, b) => b.time - a.time)

const ALL_TYPES = ["All", "tool_call", "tool_result", "channel_in", "channel_out", "message_in", "message_out", "system"] as const
const ALL_AGENTS = ["All", ...Array.from(new Set(allLogs.map((l) => l.agentName)))]
const PAGE_SIZES = [10, 25, 50] as const

export default function LogsPage() {
  const [search, setSearch] = useState("")
  const [typeFilter, setTypeFilter] = useState("All")
  const [agentFilter, setAgentFilter] = useState("All")
  const [pageSize, setPageSize] = useState<number>(25)
  const [page, setPage] = useState(0)

  const filtered = useMemo(() => {
    return allLogs.filter((l) => {
      if (search && !l.content.toLowerCase().includes(search.toLowerCase()) && !(l.tool ?? "").includes(search)) return false
      if (typeFilter !== "All" && l.type !== typeFilter) return false
      if (agentFilter !== "All" && l.agentName !== agentFilter) return false
      return true
    })
  }, [search, typeFilter, agentFilter])

  const totalPages = Math.ceil(filtered.length / pageSize)
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
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        {/* Toolbar */}
        <div className="flex items-center gap-2 flex-wrap">
          <div className="relative flex-1 max-w-sm">
            <SearchIcon className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
            <Input
              placeholder="Search logs..."
              value={search}
              onChange={(e) => { setSearch(e.target.value); setPage(0) }}
              className="pl-8"
            />
          </div>
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
        <div className="overflow-x-auto">
          <LogTable logs={paged} showAgent />
        </div>

        {/* Pagination */}
        <div className="flex items-center justify-between text-sm">
          <div className="flex items-center gap-2 text-muted-foreground">
            <span>Rows per page</span>
            <Select value={String(pageSize)} onValueChange={(v) => { if (v) { setPageSize(Number(v)); setPage(0) } }}>
              <SelectTrigger className="w-[70px] h-8"><SelectValue /></SelectTrigger>
              <SelectContent>
                {PAGE_SIZES.map((s) => <SelectItem key={s} value={String(s)}>{s}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-muted-foreground text-xs">
              {filtered.length > 0 ? `${page * pageSize + 1}–${Math.min((page + 1) * pageSize, filtered.length)} of ${filtered.length}` : "0 results"}
            </span>
            <Button variant="outline" size="icon" className="h-8 w-8" disabled={page === 0} onClick={() => setPage(page - 1)}>
              <ChevronLeftIcon className="size-4" />
            </Button>
            <Button variant="outline" size="icon" className="h-8 w-8" disabled={page >= totalPages - 1} onClick={() => setPage(page + 1)}>
              <ChevronRightIcon className="size-4" />
            </Button>
          </div>
        </div>
      </div>
    </>
  )
}
