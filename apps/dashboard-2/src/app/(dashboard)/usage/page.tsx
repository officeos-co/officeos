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
import { useUsage } from "@/hooks/useBilling"
import {
  DownloadIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
} from "lucide-react"

const RANGES = ["Last 7 days", "Last 14 days", "Last 30 days", "Month to date"] as const
const GROUP_BY = ["Day", "Week", "Model"] as const

function rangeDays(range: string): number {
  if (range === "Last 7 days") return 7
  if (range === "Last 14 days") return 14
  if (range === "Month to date") return new Date().getDate()
  return 30
}

function formatNumber(n: number) {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}k`
  return n.toString()
}

function formatDate(d: string) {
  const date = new Date(d + "T00:00:00")
  return date.toLocaleDateString(undefined, { month: "short", day: "numeric" })
}

export default function UsagePage() {
  const { dailyUsage: mockDailyUsage } = useUsage()
  const [range, setRange] = useState<string>("Month to date")
  const [groupBy, setGroupBy] = useState<string>("Day")
  const [model, setModel] = useState("All")

  const days = rangeDays(range)
  const data = useMemo(() => mockDailyUsage.slice(-days), [days, mockDailyUsage])

  const totals = useMemo(() => ({
    inputTokens: data.reduce((s, d) => s + d.inputTokens, 0),
    outputTokens: data.reduce((s, d) => s + d.outputTokens, 0),
    webSearches: data.reduce((s, d) => s + d.webSearches, 0),
  }), [data])

  const tokenChartData = data.map((d) => ({
    date: formatDate(d.date),
    input: d.inputTokens,
    output: d.outputTokens,
  }))

  const rateLimitChartData = data.map((d) => ({
    date: formatDate(d.date),
    blocked: d.rateLimited,
  }))

  return (
    <>
      <PageHeader
        group="Analytics"
        page="Usage"
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
          <Select value={model} onValueChange={(v) => { if (v) setModel(v) }}>
            <SelectTrigger className="w-[160px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="All">All models</SelectItem>
              <SelectItem value="claude-sonnet-4-6">Claude Sonnet 4.6</SelectItem>
              <SelectItem value="claude-opus-4-6">Claude Opus 4.6</SelectItem>
            </SelectContent>
          </Select>
          <Select value={groupBy} onValueChange={(v) => { if (v) setGroupBy(v) }}>
            <SelectTrigger className="w-[120px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {GROUP_BY.map((g) => <SelectItem key={g} value={g}>{g}</SelectItem>)}
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

        {/* Stat cards */}
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <div className="rounded-xl border border-border p-4">
            <div className="text-sm text-muted-foreground">Total tokens in</div>
            <div className="text-2xl font-semibold mt-1">{formatNumber(totals.inputTokens)}</div>
          </div>
          <div className="rounded-xl border border-border p-4">
            <div className="text-sm text-muted-foreground">Total tokens out</div>
            <div className="text-2xl font-semibold mt-1">{formatNumber(totals.outputTokens)}</div>
          </div>
          <div className="rounded-xl border border-border p-4">
            <div className="text-sm text-muted-foreground">Total web searches</div>
            <div className="text-2xl font-semibold mt-1">{formatNumber(totals.webSearches)}</div>
          </div>
        </div>

        {/* Token usage chart */}
        <div className="rounded-xl border border-border p-4">
          <div className="mb-1 text-sm font-medium">Token usage</div>
          <div className="text-xs text-muted-foreground mb-4">Includes usage from all agents</div>
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={tokenChartData} barGap={0} barCategoryGap="20%">
              <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="var(--border)" />
              <XAxis dataKey="date" tick={{ fontSize: 11 }} stroke="var(--muted-foreground)" tickLine={false} axisLine={false} />
              <YAxis tick={{ fontSize: 11 }} stroke="var(--muted-foreground)" tickLine={false} axisLine={false} tickFormatter={formatNumber} width={45} />
              <Tooltip
                contentStyle={{ fontSize: 12, borderRadius: 8, border: "1px solid var(--border)", background: "var(--popover)" }}
                formatter={(value) => formatNumber(Number(value))}
              />
              <Bar dataKey="input" name="Input" fill="var(--primary)" radius={[3, 3, 0, 0]} />
              <Bar dataKey="output" name="Output" fill="var(--primary)" opacity={0.4} radius={[3, 3, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Rate limited chart */}
        <div className="rounded-xl border border-border p-4">
          <div className="mb-1 text-sm font-medium">Rate-limited requests</div>
          <div className="text-xs text-muted-foreground mb-4">Requests blocked due to rate limits</div>
          <ResponsiveContainer width="100%" height={180}>
            <BarChart data={rateLimitChartData} barCategoryGap="20%">
              <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="var(--border)" />
              <XAxis dataKey="date" tick={{ fontSize: 11 }} stroke="var(--muted-foreground)" tickLine={false} axisLine={false} />
              <YAxis tick={{ fontSize: 11 }} stroke="var(--muted-foreground)" tickLine={false} axisLine={false} allowDecimals={false} width={30} />
              <Tooltip
                contentStyle={{ fontSize: 12, borderRadius: 8, border: "1px solid var(--border)", background: "var(--popover)" }}
              />
              <Bar dataKey="blocked" name="Blocked" fill="var(--destructive)" opacity={0.7} radius={[3, 3, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>
    </>
  )
}
