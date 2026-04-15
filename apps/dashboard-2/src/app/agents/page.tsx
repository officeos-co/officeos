"use client"

import { useState } from "react"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuCheckboxItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { PlusIcon, SearchIcon, FilterIcon } from "lucide-react"

const agents = [
  { id: "agt_a1b2c3", name: "Research Assistant", status: "running", createdAt: Date.now() - 2 * 86400000, updatedAt: Date.now() - 600000 },
  { id: "agt_b2c3d4", name: "Code Reviewer", status: "running", createdAt: Date.now() - 5 * 86400000, updatedAt: Date.now() - 3600000 },
  { id: "agt_c3d4e5", name: "Customer Support Bot", status: "running", createdAt: Date.now() - 10 * 86400000, updatedAt: Date.now() - 7200000 },
  { id: "agt_d4e5f6", name: "Data Pipeline Monitor", status: "pending", createdAt: Date.now() - 86400000, updatedAt: Date.now() - 86400000 },
  { id: "agt_e5f6a7", name: "Content Writer", status: "stopped", createdAt: Date.now() - 14 * 86400000, updatedAt: Date.now() - 7 * 86400000 },
  { id: "agt_f6a7b8", name: "Security Scanner", status: "failed", createdAt: Date.now() - 3 * 86400000, updatedAt: Date.now() - 1800000 },
]

const ALL_STATUSES = ["running", "pending", "stopped", "failed"] as const

const statusStyles: Record<string, { bg: string; text: string; label: string }> = {
  running: { bg: "bg-emerald-100", text: "text-emerald-700", label: "RUNNING" },
  pending: { bg: "bg-amber-100", text: "text-amber-700", label: "PENDING" },
  stopped: { bg: "bg-zinc-100", text: "text-zinc-500", label: "STOPPED" },
  failed: { bg: "bg-red-100", text: "text-red-700", label: "FAILED" },
}

function StatusBadge({ status }: { status: string }) {
  const style = statusStyles[status] ?? statusStyles.stopped
  return (
    <span className={`inline-flex rounded-full px-2.5 py-1 text-[10px] font-semibold uppercase tracking-widest ${style.bg} ${style.text}`}>
      {style.label}
    </span>
  )
}

function timeAgo(ts: number) {
  const diff = Date.now() - ts
  const minutes = Math.floor(diff / 60000)
  if (minutes < 1) return "just now"
  if (minutes < 60) return `${minutes} minutes ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours} hours ago`
  const days = Math.floor(hours / 24)
  if (days < 30) return `${days} days ago`
  return `${Math.floor(days / 30)} months ago`
}

export default function AgentsPage() {
  const [search, setSearch] = useState("")
  const [statusFilter, setStatusFilter] = useState<Set<string>>(new Set())

  const filtered = agents.filter((a) => {
    if (search && !a.name.toLowerCase().includes(search.toLowerCase())) return false
    if (statusFilter.size > 0 && !statusFilter.has(a.status)) return false
    return true
  })

  function toggleStatus(status: string) {
    setStatusFilter((prev) => {
      const next = new Set(prev)
      if (next.has(status)) next.delete(status)
      else next.add(status)
      return next
    })
  }

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Agents"
        action={
          <Button size="sm">
            <PlusIcon />
            New agent
          </Button>
        }
      />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="flex items-center gap-2">
          <div className="relative flex-1 max-w-sm">
            <SearchIcon className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
            <Input
              placeholder="Search agents..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-8"
            />
          </div>
          <DropdownMenu>
            <DropdownMenuTrigger render={<Button variant="outline" size="sm" />}>
              <FilterIcon className="size-4" />
              Status
              {statusFilter.size > 0 && (
                <span className="ml-1 rounded-full bg-muted px-1.5 text-xs">{statusFilter.size}</span>
              )}
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start">
              {ALL_STATUSES.map((s) => (
                <DropdownMenuCheckboxItem
                  key={s}
                  checked={statusFilter.has(s)}
                  onCheckedChange={() => toggleStatus(s)}
                  className="capitalize"
                >
                  {s}
                </DropdownMenuCheckboxItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        <table className="w-full text-sm">
          <thead>
            <tr className="border-b text-left">
              <th className="px-4 py-3 font-medium">ID</th>
              <th className="px-4 py-3 font-medium">Name</th>
              <th className="px-4 py-3 font-medium text-center">Status</th>
              <th className="px-4 py-3 font-medium">Created</th>
              <th className="px-4 py-3 font-medium">Last updated</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((agent) => (
              <tr
                key={agent.id}
                className="border-b last:border-0 hover:bg-muted/50 cursor-pointer transition-colors"
              >
                <td className="px-4 py-3 font-mono text-xs">{agent.id}</td>
                <td className="px-4 py-3 font-medium">{agent.name}</td>
                <td className="px-4 py-3 text-center">
                  <StatusBadge status={agent.status} />
                </td>
                <td className="px-4 py-3">{timeAgo(agent.createdAt)}</td>
                <td className="px-4 py-3">{timeAgo(agent.updatedAt)}</td>
              </tr>
            ))}
            {filtered.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                  No agents found.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
