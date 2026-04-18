"use client"

import { useState, useMemo } from "react"
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from "recharts"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { useCost } from "@/features/analytics"
import { useModels } from "@/features/agents"
import {
  DownloadIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
} from "lucide-react"

const RANGES = ["Last 7 days", "Last 14 days", "Last 30 days", "Month to date"] as const
const GROUP_BY = ["Day", "Model"] as const

function rangeDays(range: string): number {
  if (range === "Last 7 days") return 7
  if (range === "Last 14 days") return 14
  if (range === "Month to date") return new Date().getDate()
  return 30
}

function usd(n: number) {
  return `$${n.toFixed(2)}`
}

function formatDate(d: string) {
  const date = new Date(d + "T00:00:00")
  return date.toLocaleDateString(undefined, { month: "short", day: "numeric" })
}

export default function CostPage() {
  const { dailyCost: mockDailyCost } = useCost()
  const { models } = useModels()
  const [range, setRange] = useState<string>("Month to date")
  const [groupBy, setGroupBy] = useState<string>("Day")
  const [model, setModel] = useState("All")

  const days = rangeDays(range)
  const data = useMemo(() => mockDailyCost.slice(-days), [days, mockDailyCost])

  const totals = useMemo(() => ({
    tokenCost: data.reduce((s, d) => s + d.tokenCost, 0),
    webSearchCost: data.reduce((s, d) => s + d.webSearchCost, 0),
    codeExecCost: data.reduce((s, d) => s + d.codeExecCost, 0),
    runtimeCost: data.reduce((s, d) => s + d.runtimeCost, 0),
  }), [data])

  const totalAll = totals.tokenCost + totals.webSearchCost + totals.codeExecCost + totals.runtimeCost

  const chartData = data.map((d) => ({
    date: formatDate(d.date),
    tokens: d.tokenCost,
    search: d.webSearchCost,
    code: d.codeExecCost,
    runtime: d.runtimeCost,
    total: d.tokenCost + d.webSearchCost + d.codeExecCost + d.runtimeCost,
  }))

  return (
    <>
      <PageHeader
        group="Analytics"
        page="Cost"
        action={
          <Button variant="outline" size="sm">
            <DownloadIcon />
            Export
          </Button>
        }
      />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0 max-w-4xl mx-auto w-full">
        {/* Filters */}
        <div className="flex items-center gap-2 flex-wrap">
          <Select value={groupBy} onValueChange={(v) => { if (v) setGroupBy(v) }}>
            <SelectTrigger className="w-[140px]">
              <span className="text-muted-foreground mr-1">Group by:</span>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {GROUP_BY.map((g) => <SelectItem key={g} value={g}>{g}</SelectItem>)}
            </SelectContent>
          </Select>
          <Select value={model} onValueChange={(v) => { if (v) setModel(v) }}>
            <SelectTrigger className="w-[160px]">
              <span className="text-muted-foreground mr-1">Model:</span>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="All">All</SelectItem>
              {models.map((m) => (
                <SelectItem key={m.id} value={m.id}>{m.displayName}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          <div className="flex items-center gap-1 ml-auto">
            <Button variant="outline" size="icon" className="h-8 w-8">
              <ChevronLeftIcon className="size-4" />
            </Button>
            <Select value={range} onValueChange={(v) => { if (v) setRange(v) }}>
              <SelectTrigger className="w-[160px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {RANGES.map((r) => <SelectItem key={r} value={r}>{r}</SelectItem>)}
              </SelectContent>
            </Select>
            <Button variant="outline" size="icon" className="h-8 w-8">
              <ChevronRightIcon className="size-4" />
            </Button>
          </div>
        </div>

        {/* Cost summary cards */}
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <div className="rounded-xl border border-border p-4">
            <div className="text-sm text-muted-foreground">Total token cost</div>
            <div className="text-2xl font-semibold mt-1">{usd(totals.tokenCost)}</div>
          </div>
          <div className="rounded-xl border border-border p-4">
            <div className="text-sm text-muted-foreground">Web search cost</div>
            <div className="text-xs text-muted-foreground mt-0.5">Broken down by agent</div>
            <div className="text-2xl font-semibold mt-1">{usd(totals.webSearchCost)}</div>
          </div>
          <div className="rounded-xl border border-border p-4">
            <div className="text-sm text-muted-foreground">Code execution cost</div>
            <div className="text-xs text-muted-foreground mt-0.5">Broken down by agent</div>
            <div className="text-2xl font-semibold mt-1">{usd(totals.codeExecCost)}</div>
          </div>
          <div className="rounded-xl border border-border p-4">
            <div className="text-sm text-muted-foreground">Session runtime cost</div>
            <div className="text-xs text-muted-foreground mt-0.5">Broken down by agent</div>
            <div className="text-2xl font-semibold mt-1">{usd(totals.runtimeCost)}</div>
          </div>
        </div>

        {/* Daily cost stacked area chart */}
        <div className="rounded-xl border border-border p-4">
          <div className="mb-1 text-sm font-medium">Daily cost breakdown</div>
          <div className="text-xs text-muted-foreground mb-4">Total: {usd(totalAll)}</div>
          <ResponsiveContainer width="100%" height={280}>
            <BarChart data={chartData} barGap={0} barCategoryGap="20%">
              <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="var(--border)" />
              <XAxis dataKey="date" tick={{ fontSize: 11 }} stroke="var(--muted-foreground)" tickLine={false} axisLine={false} />
              <YAxis tick={{ fontSize: 11 }} stroke="var(--muted-foreground)" tickLine={false} axisLine={false} tickFormatter={(v) => `$${v}`} width={40} />
              <Tooltip
                contentStyle={{ fontSize: 12, borderRadius: 8, border: "1px solid var(--border)", background: "var(--popover)" }}
                formatter={(value) => usd(Number(value))}
              />
              <Bar dataKey="tokens" name="Tokens" stackId="1" fill="var(--primary)" radius={[0, 0, 0, 0]} />
              <Bar dataKey="search" name="Web search" stackId="1" fill="var(--chart-2)" />
              <Bar dataKey="code" name="Code exec" stackId="1" fill="var(--chart-3)" />
              <Bar dataKey="runtime" name="Runtime" stackId="1" fill="var(--chart-4)" radius={[3, 3, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>
    </>
  )
}
