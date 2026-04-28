"use client";

import { useEffect } from "react";
import { toast } from "sonner";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Separator } from "@/components/ui/separator";
import { DownloadIcon } from "lucide-react";
import { useUsage } from "@/features/analytics";

function formatCredits(n: number) {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}k`;
  return n.toLocaleString();
}

function fmtDate(iso: string | null | undefined): string {
  if (!iso) return "";
  const t = Date.parse(iso);
  if (Number.isNaN(t)) return iso ?? "";
  return new Date(t).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

export default function UsagePage() {
  const { usage, loading, error } = useUsage();

  useEffect(() => {
    if (error)
      toast.error("Failed to load usage", { description: error.message });
  }, [error]);

  if (loading && !usage) {
    return (
      <>
        <PageHeader group="Analytics" page="Usage" />
        <div className="flex flex-1 flex-col gap-4 p-4 pt-0 max-w-4xl mx-auto w-full">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="rounded-xl border border-border p-4">
                <Skeleton className="h-4 w-24 mb-2" />
                <Skeleton className="h-8 w-32" />
              </div>
            ))}
          </div>
          <Skeleton className="h-6 w-full rounded-md" />
          <Skeleton className="h-48 w-full rounded-xl" />
        </div>
      </>
    );
  }

  if (!usage) {
    return (
      <>
        <PageHeader group="Analytics" page="Usage" />
        <div className="flex items-center justify-center py-20">
          <p className="text-sm text-muted-foreground">
            Unable to load usage information.
          </p>
        </div>
      </>
    );
  }

  const pct =
    usage.creditBudgetPerMonth > 0
      ? Math.min(
          100,
          (usage.creditsUsedThisMonth / usage.creditBudgetPerMonth) * 100,
        )
      : 0;

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
        {/* Stat cards */}
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <div className="rounded-xl border border-border p-4">
            <div className="text-sm text-muted-foreground">Credits used</div>
            <div className="text-2xl font-semibold mt-1">
              {formatCredits(usage.creditsUsedThisMonth)}
            </div>
          </div>
          <div className="rounded-xl border border-border p-4">
            <div className="text-sm text-muted-foreground">Monthly budget</div>
            <div className="text-2xl font-semibold mt-1">
              {formatCredits(usage.creditBudgetPerMonth)}
            </div>
          </div>
          <div className="rounded-xl border border-border p-4">
            <div className="text-sm text-muted-foreground">
              Credits remaining
            </div>
            <div className="text-2xl font-semibold mt-1">
              {formatCredits(usage.creditsRemaining)}
            </div>
          </div>
        </div>

        {/* Progress bar */}
        <div className="rounded-xl border border-border p-4">
          <div className="flex items-center justify-between mb-2">
            <div className="text-sm font-medium">Credit usage this period</div>
            <span className="text-sm text-muted-foreground">
              {pct.toFixed(1)}%
            </span>
          </div>
          <div className="h-3 rounded-full bg-muted overflow-hidden">
            <div
              className={`h-full rounded-full transition-all ${pct >= 100 ? "bg-destructive" : pct >= 80 ? "bg-yellow-500" : "bg-primary"}`}
              style={{ width: `${pct}%` }}
            />
          </div>
          <div className="flex items-center justify-between mt-2">
            <span className="text-xs text-muted-foreground">
              {formatCredits(usage.creditsUsedThisMonth)} /{" "}
              {formatCredits(usage.creditBudgetPerMonth)} credits
            </span>
            {usage.overBudget && (
              <span className="rounded bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive">
                Over budget
              </span>
            )}
          </div>
        </div>

        <Separator />

        {/* Period info */}
        <div className="rounded-xl border border-border p-4">
          <h3 className="text-sm font-medium mb-3">Billing period</h3>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-muted-foreground">Plan</span>
              <p className="font-medium capitalize mt-0.5">{usage.plan}</p>
            </div>
            <div>
              <span className="text-muted-foreground">Cycle</span>
              <p className="font-medium capitalize mt-0.5">
                {usage.billingCycle}
              </p>
            </div>
            <div>
              <span className="text-muted-foreground">Period start</span>
              <p className="font-medium mt-0.5">{fmtDate(usage.periodStart)}</p>
            </div>
            <div>
              <span className="text-muted-foreground">Period end</span>
              <p className="font-medium mt-0.5">{fmtDate(usage.periodEnd)}</p>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
