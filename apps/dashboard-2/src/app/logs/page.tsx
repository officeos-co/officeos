"use client"

import { useState, useMemo } from "react"
import { PageHeader } from "@/components/page-header"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { mockLogs } from "@/data/analytics-mock"
import {
  SearchIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  DownloadIcon,
} from "lucide-react"

function formatTime(ts: number) {
  return new Date(ts).toLocaleString(undefined, {
    month: "short", day: "numeric", hour: "2-digit", minute: "2-digit", second: "2-digit",
  })
}

function formatTokens(n: number) {
  if (n >= 1000) return `${(n / 1000).toFixed(1)}k`
  return n.toString()
}

const PAGE_SIZES = [10, 25, 50] as const
const MODELS = ["All", ...Array.from(new Set(mockLogs.map((l) => l.model)))]
const TYPES = ["All", ...Array.from(new Set(mockLogs.map((l) => l.type)))]

export default function LogsPage() {
  const [search, setSearch] = useState("")
  const [modelFilter, setModelFilter] = useState("All")
  const [typeFilter, setTypeFilter] = useState("All")
  const [pageSize, setPageSize] = useState<number>(25)
  const [page, setPage] = useState(0)

  const filtered = useMemo(() => {
    return mockLogs.filter((l) => {
      if (search && !l.request.toLowerCase().includes(search.toLowerCase()) && !l.id.includes(search)) return false
      if (modelFilter !== "All" && l.model !== modelFilter) return false
      if (typeFilter !== "All" && l.type !== typeFilter) return false
      return true
    })
  }, [search, modelFilter, typeFilter])

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
              placeholder="Search requests..."
              value={search}
              onChange={(e) => { setSearch(e.target.value); setPage(0) }}
              className="pl-8"
            />
          </div>
          <Select value={modelFilter} onValueChange={(v) => { if (v) { setModelFilter(v); setPage(0) } }}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Model" />
            </SelectTrigger>
            <SelectContent>
              {MODELS.map((m) => <SelectItem key={m} value={m}>{m === "All" ? "All models" : m}</SelectItem>)}
            </SelectContent>
          </Select>
          <Select value={typeFilter} onValueChange={(v) => { if (v) { setTypeFilter(v); setPage(0) } }}>
            <SelectTrigger className="w-[140px]">
              <SelectValue placeholder="Type" />
            </SelectTrigger>
            <SelectContent>
              {TYPES.map((t) => <SelectItem key={t} value={t}>{t === "All" ? "All types" : t}</SelectItem>)}
            </SelectContent>
          </Select>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left">
                <th className="px-3 py-3 font-medium whitespace-nowrap">Time</th>
                <th className="px-3 py-3 font-medium">ID</th>
                <th className="px-3 py-3 font-medium">Model</th>
                <th className="px-3 py-3 font-medium text-right">Input</th>
                <th className="px-3 py-3 font-medium text-right">Output</th>
                <th className="px-3 py-3 font-medium">Type</th>
                <th className="px-3 py-3 font-medium">Tier</th>
                <th className="px-3 py-3 font-medium">Request</th>
              </tr>
            </thead>
            <tbody>
              {paged.map((log) => (
                <tr key={log.id} className="border-b last:border-0 hover:bg-muted/50 transition-colors">
                  <td className="px-3 py-2.5 whitespace-nowrap text-muted-foreground text-xs">{formatTime(log.time)}</td>
                  <td className="px-3 py-2.5 font-mono text-xs">{log.id}</td>
                  <td className="px-3 py-2.5 font-mono text-xs">{log.model}</td>
                  <td className="px-3 py-2.5 text-right font-mono text-xs">{formatTokens(log.inputTokens)}</td>
                  <td className="px-3 py-2.5 text-right font-mono text-xs">{formatTokens(log.outputTokens)}</td>
                  <td className="px-3 py-2.5">
                    <span className="rounded bg-muted px-1.5 py-0.5 text-xs">{log.type}</span>
                  </td>
                  <td className="px-3 py-2.5 text-xs">{log.serviceTier}</td>
                  <td className="px-3 py-2.5 text-xs max-w-[200px] truncate text-muted-foreground">{log.request}</td>
                </tr>
              ))}
              {paged.length === 0 && (
                <tr><td colSpan={8} className="px-3 py-8 text-center text-muted-foreground">No logs found.</td></tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="flex items-center justify-between text-sm">
          <div className="flex items-center gap-2 text-muted-foreground">
            <span>Rows per page</span>
            <Select value={String(pageSize)} onValueChange={(v) => { if (v) { setPageSize(Number(v)); setPage(0) } }}>
              <SelectTrigger className="w-[70px] h-8">
                <SelectValue />
              </SelectTrigger>
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
