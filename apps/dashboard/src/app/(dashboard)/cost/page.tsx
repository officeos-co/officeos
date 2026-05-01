"use client"

import { useEffect } from "react"
import { toast } from "sonner"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Separator } from "@/components/ui/separator"
import { DownloadIcon } from "lucide-react"
import { useCost, useUsage } from "@/features/analytics"

function formatCredits(n: number) {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}k`
  return n.toLocaleString()
}

export default function CostPage() {
  const { weights, loading: weightsLoading, error: weightsError } = useCost()
  const { usage, loading: usageLoading, error: usageError } = useUsage()

  const loading = weightsLoading || usageLoading
  const error = weightsError || usageError

  useEffect(() => {
    if (error) toast.error("Failed to load cost data", { description: error.message })
  }, [error])

  if (loading && !weights.length) {
    return (
      <>
        <PageHeader
          page="Cost"
          contentClassName="max-w-4xl"
        />
        <div className="flex flex-1 flex-col gap-4 pb-4 max-w-4xl mx-auto w-full">
          <Skeleton className="h-48 w-full rounded-xl" />
          <Skeleton className="h-64 w-full rounded-xl" />
        </div>
      </>
    )
  }

  const sorted = [...weights].sort((a, b) => a.weight - b.weight)
  const baselineModel = sorted.find((w) => w.weight === 1)

  const pct = usage && usage.creditBudgetPerMonth > 0
    ? Math.min(100, (usage.creditsUsedThisMonth / usage.creditBudgetPerMonth) * 100)
    : 0

  return (
    <>
      <PageHeader
        page="Cost"
        contentClassName="max-w-4xl"
        action={
          <Button variant="outline" size="sm">
            <DownloadIcon />
            Export
          </Button>
        }
      />
      <div className="flex flex-1 flex-col gap-4 pb-4 max-w-4xl mx-auto w-full">
        {/* Credit summary */}
        {usage && (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div className="rounded-xl border border-border p-4">
              <div className="text-sm text-muted-foreground">Credits used</div>
              <div className="text-2xl font-semibold mt-1">{formatCredits(usage.creditsUsedThisMonth)}</div>
            </div>
            <div className="rounded-xl border border-border p-4">
              <div className="text-sm text-muted-foreground">Monthly budget</div>
              <div className="text-2xl font-semibold mt-1">{formatCredits(usage.creditBudgetPerMonth)}</div>
            </div>
            <div className="rounded-xl border border-border p-4">
              <div className="text-sm text-muted-foreground">Budget used</div>
              <div className="text-2xl font-semibold mt-1">{pct.toFixed(1)}%</div>
            </div>
          </div>
        )}

        <Separator />

        {/* Model cost weights */}
        <div className="rounded-xl border border-border p-4">
          <h3 className="text-sm font-medium mb-1">Model credit weights</h3>
          <p className="text-xs text-muted-foreground mb-4">
            Each model consumes credits at a different rate.
            {baselineModel && <> {baselineModel.model} is the baseline (1×).</>}
            {" "}Unknown models default to 20×.
          </p>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left">
                <th className="px-0 py-2.5 font-medium">Model</th>
                <th className="px-0 py-2.5 font-medium text-right">Weight</th>
                <th className="px-0 py-2.5 font-medium text-right">Credits per 1k tokens</th>
              </tr>
            </thead>
            <tbody>
              {sorted.map((w) => (
                <tr key={w.model} className="border-b last:border-0">
                  <td className="px-0 py-2.5 font-mono text-xs">{w.model}</td>
                  <td className="px-0 py-2.5 text-right">{w.weight}×</td>
                  <td className="px-0 py-2.5 text-right">{formatCredits(w.weight * 1000)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Visual weight comparison */}
        <div className="rounded-xl border border-border p-4">
          <h3 className="text-sm font-medium mb-4">Relative cost comparison</h3>
          <div className="space-y-3">
            {sorted.map((w) => {
              const maxWeight = sorted[sorted.length - 1]?.weight ?? 1
              const barPct = (w.weight / maxWeight) * 100
              return (
                <div key={w.model} className="flex items-center gap-3">
                  <span className="text-xs font-mono w-36 shrink-0 truncate">{w.model}</span>
                  <div className="flex-1 h-5 rounded bg-muted overflow-hidden">
                    <div
                      className="h-full rounded bg-primary transition-all"
                      style={{ width: `${barPct}%` }}
                    />
                  </div>
                  <span className="text-xs text-muted-foreground w-8 text-right">{w.weight}×</span>
                </div>
              )
            })}
          </div>
        </div>
      </div>
    </>
  )
}
