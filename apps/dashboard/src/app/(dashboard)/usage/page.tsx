"use client";

import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { PageHeader } from "@/shell/page-header";
import { PageContainer } from "@/shell/page-container";
import { Button } from "@/ui/button";
import { Input } from "@/ui/input";
import { Skeleton } from "@/ui/skeleton";
import { DownloadIcon } from "lucide-react";
import { useUsageAnalytics } from "@/features/analytics";
import { cn } from "@/lib/utils";

function formatCompact(n: number) {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}k`;
  return n.toLocaleString();
}

function formatMoney(cents: number, currency: string) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency,
  }).format(cents / 100);
}

function toInputDate(date: Date) {
  return date.toISOString().slice(0, 10);
}

function defaultRange() {
  const to = new Date();
  const from = new Date();
  from.setDate(to.getDate() - 6);
  return { from: toInputDate(from), to: toInputDate(to), preset: "7d" as const };
}

function rangeForPreset(preset: "1d" | "7d" | "30d" | "last-month") {
  const today = new Date();
  if (preset === "last-month") {
    const from = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth() - 1, 1));
    const to = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), 0));
    return { from: toInputDate(from), to: toInputDate(to), preset };
  }

  const days = preset === "1d" ? 1 : preset === "7d" ? 7 : 30;
  const from = new Date();
  from.setDate(today.getDate() - (days - 1));
  return { from: toInputDate(from), to: toInputDate(today), preset };
}

function chartDateLabel(iso: string) {
  const t = Date.parse(iso);
  if (Number.isNaN(t)) return iso;
  return new Date(t).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
  });
}

export default function UsagePage() {
  const [range, setRange] = useState<{
    from: string;
    to: string;
    preset?: "1d" | "7d" | "30d" | "last-month";
  }>(defaultRange);
  const { usage, loading, error } = useUsageAnalytics(range.from, range.to);

  useEffect(() => {
    if (error)
      toast.error("Failed to load usage", { description: error.message });
  }, [error]);

  const points = useMemo(
    () =>
      (usage?.points ?? []).map((point) => ({
        date: chartDateLabel(point.date),
        tokens: point.tokens,
      })),
    [usage],
  );

  const currency = usage?.cost.currency ?? "USD";
  const estimatedLabel = usage?.cost.estimated ? "Estimated" : "Actual";

  const rangeControls = (
    <div className="flex flex-wrap items-center justify-end gap-2">
      <div className="flex items-center gap-2">
        <Input
          type="date"
          value={range.from}
          onChange={(e) =>
            setRange((current) => ({
              ...current,
              from: e.target.value,
              preset: undefined,
            }))
          }
          className="h-8 w-[138px] text-xs"
        />
        <span className="text-xs text-muted-foreground">to</span>
        <Input
          type="date"
          value={range.to}
          onChange={(e) =>
            setRange((current) => ({
              ...current,
              to: e.target.value,
              preset: undefined,
            }))
          }
          className="h-8 w-[138px] text-xs"
        />
      </div>
      <div className="flex items-center rounded-md border border-border p-0.5">
        {[
          ["1d", "1d"],
          ["7d", "7d"],
          ["30d", "30d"],
          ["last-month", "Last month"],
        ].map(([value, label]) => (
          <Button
            key={value}
            type="button"
            variant="ghost"
            size="sm"
            onClick={() =>
              setRange(rangeForPreset(value as "1d" | "7d" | "30d" | "last-month"))
            }
            className={cn(
              "h-7 px-2.5 text-xs",
              range.preset === value && "bg-accent text-accent-foreground",
            )}
          >
            {label}
          </Button>
        ))}
      </div>
      <Button variant="outline" size="sm">
        <DownloadIcon />
        Export
      </Button>
    </div>
  );

  if (loading && !usage) {
    return (
      <>
        <PageHeader page="Usage" width="thin" action={rangeControls} />
        <PageContainer width="thin" className="flex flex-1 flex-col gap-4 pb-4">
          <Skeleton className="h-[440px] w-full rounded-xl" />
        </PageContainer>
      </>
    );
  }

  return (
    <>
      <PageHeader page="Usage" width="thin" action={rangeControls} />
      <PageContainer width="thin" className="flex flex-1 flex-col gap-4 pb-4">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <div className="rounded-xl border border-border p-4">
            <div className="text-sm text-muted-foreground">{estimatedLabel} total</div>
            <div className="mt-1 text-2xl font-semibold">
              {formatMoney(usage?.cost.totalCents ?? 0, currency)}
            </div>
          </div>
          <div className="rounded-xl border border-border p-4">
            <div className="text-sm text-muted-foreground">Included</div>
            <div className="mt-1 text-2xl font-semibold">
              {formatMoney(usage?.cost.includedCents ?? 0, currency)}
            </div>
          </div>
          <div className="rounded-xl border border-border p-4">
            <div className="text-sm text-muted-foreground">On-demand</div>
            <div className="mt-1 text-2xl font-semibold">
              {formatMoney(usage?.cost.onDemandCents ?? 0, currency)}
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-border p-4">
          <div className="mb-4 flex flex-wrap items-start justify-between gap-4">
            <div>
              <h3 className="text-sm font-medium">Token usage</h3>
              <p className="text-xs text-muted-foreground">
                {formatCompact(usage?.totalTokens ?? 0)} tokens over selected range
              </p>
            </div>
          </div>
          <div className="h-[360px]">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={points} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
                <CartesianGrid vertical={false} stroke="var(--border)" />
                <XAxis
                  dataKey="date"
                  tickLine={false}
                  axisLine={false}
                  fontSize={12}
                />
                <YAxis
                  tickLine={false}
                  axisLine={false}
                  fontSize={12}
                  tickFormatter={formatCompact}
                />
                <Tooltip
                  cursor={{ fill: "var(--muted)" }}
                  formatter={(value) => [`${formatCompact(Number(value))} tokens`, "Usage"]}
                  labelStyle={{ color: "var(--foreground)" }}
                />
                <Bar dataKey="tokens" fill="var(--chart-2)" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </PageContainer>
    </>
  );
}
