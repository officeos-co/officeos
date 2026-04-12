"use client";

import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Loader2, CheckCircle2, Sparkles, Leaf } from "lucide-react";
import { cn } from "@/lib/utils";

interface UserSubscription {
  plan: string;
  billingCycle: string;
  concurrentAgentLimit: number;
  creditBudgetPerMonth: number;
  creditsUsedThisMonth: number;
  creditsRemaining: number;
  overageEnabled: boolean;
  overBudget: boolean;
  periodStart: string;
  periodEnd: string;
  isActive: boolean;
  billingEnabled: boolean;
}

function CreditUsageBar({
  used,
  budget,
}: {
  used: number;
  budget: number;
}) {
  const percent = budget > 0 ? Math.min((used / budget) * 100, 100) : 0;
  const isHigh = percent >= 80;

  return (
    <div className="space-y-1.5">
      <div className="flex justify-between text-xs text-muted-foreground">
        <span>{(used / 1_000_000).toFixed(2)}M credits used</span>
        <span>{(budget / 1_000_000).toFixed(1)}M / month</span>
      </div>
      <div className="h-2 rounded-full bg-muted overflow-hidden">
        <div
          className={cn(
            "h-full rounded-full transition-all",
            isHigh ? "bg-amber-500" : "bg-primary",
          )}
          style={{ width: `${percent}%` }}
        />
      </div>
    </div>
  );
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

export default function BillingPage() {
  const searchParams = useSearchParams();
  const checkoutSuccess = searchParams.get("checkout") === "success";

  const [sub, setSub] = useState<UserSubscription | null>(null);
  const [loading, setLoading] = useState(true);
  const [checkoutLoading, setCheckoutLoading] = useState(false);
  const [portalLoading, setPortalLoading] = useState(false);
  const [overageLoading, setOverageLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch("/api/billing/user/subscription", { credentials: "include" })
      .then((r) => r.json())
      .then((data) => setSub(data))
      .catch(() => setError("Failed to load subscription info."))
      .finally(() => setLoading(false));
  }, []);

  async function handleUpgrade() {
    if (!sub) return;
    setCheckoutLoading(true);
    try {
      const res = await fetch("/api/billing/user/subscribe", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ plan: "pro", billingCycle: "monthly" }),
      });
      if (!res.ok) {
        setError("Could not start checkout. Please try again.");
        return;
      }
      const { checkoutUrl } = await res.json();
      window.location.href = checkoutUrl;
    } catch {
      setError("Could not start checkout. Please try again.");
    } finally {
      setCheckoutLoading(false);
    }
  }

  async function handlePortal() {
    setPortalLoading(true);
    try {
      const res = await fetch("/api/billing/user/portal", {
        credentials: "include",
      });
      if (!res.ok) {
        setError("Could not open billing portal. Please try again.");
        return;
      }
      const { portalUrl } = await res.json();
      window.location.href = portalUrl;
    } catch {
      setError("Could not open billing portal. Please try again.");
    } finally {
      setPortalLoading(false);
    }
  }

  async function handleOverageToggle() {
    if (!sub) return;
    setOverageLoading(true);
    try {
      const res = await fetch("/api/billing/user/overage", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ enabled: !sub.overageEnabled }),
      });
      if (!res.ok) {
        setError("Could not update overage setting. Please try again.");
        return;
      }
      setSub((prev) =>
        prev ? { ...prev, overageEnabled: !prev.overageEnabled } : prev,
      );
    } catch {
      setError("Could not update overage setting. Please try again.");
    } finally {
      setOverageLoading(false);
    }
  }

  const isPro = sub?.plan === "pro";
  const PlanIcon = isPro ? Sparkles : Leaf;

  return (
    <div className="max-w-2xl mx-auto px-6 py-10 space-y-6">
      <h1 className="text-2xl font-bold text-foreground">Billing</h1>

      {/* Checkout success banner */}
      {checkoutSuccess && (
        <div className="flex items-center gap-3 rounded-xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-600 dark:text-emerald-400">
          <CheckCircle2 className="h-4 w-4 shrink-0" />
          <span>You&apos;re now on Pro — thanks for subscribing!</span>
        </div>
      )}

      {/* Error banner */}
      {error && (
        <div className="rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {/* Loading state */}
      {loading && (
        <div className="flex items-center gap-2 text-sm text-muted-foreground py-8 justify-center">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading subscription…
        </div>
      )}

      {sub && (
        <>
          {/* Current plan card */}
          <div className="rounded-2xl border border-border bg-card p-6 space-y-4">
            <div className="flex items-center gap-3">
              <PlanIcon className="h-6 w-6 text-primary" />
              <div>
                <p className="font-semibold text-foreground capitalize">
                  {sub.plan} Plan
                  {sub.billingCycle && sub.plan !== "free" && (
                    <span className="ml-2 text-xs text-muted-foreground font-normal capitalize">
                      · {sub.billingCycle}
                    </span>
                  )}
                </p>
                <p className="text-xs text-muted-foreground">
                  {sub.concurrentAgentLimit} concurrent agent{sub.concurrentAgentLimit !== 1 ? "s" : ""}
                </p>
              </div>
              {sub.isActive && (
                <span className="ml-auto text-xs rounded-full bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 px-2.5 py-0.5 font-medium">
                  Active
                </span>
              )}
            </div>

            {/* Credit usage */}
            <div className="space-y-1">
              <p className="text-sm font-medium text-foreground">Credit usage this period</p>
              <CreditUsageBar
                used={sub.creditsUsedThisMonth}
                budget={sub.creditBudgetPerMonth}
              />
              {sub.overBudget && !sub.overageEnabled && (
                <p className="text-xs text-amber-600">
                  Over budget — enable pay-as-you-go below to continue past your limit.
                </p>
              )}
            </div>

            {/* Period dates */}
            <p className="text-xs text-muted-foreground">
              Period: {formatDate(sub.periodStart)} — {formatDate(sub.periodEnd)}
            </p>
          </div>

          {/* Overage toggle */}
          {sub.billingEnabled && (
            <div className="rounded-2xl border border-border bg-card p-5 flex items-start justify-between gap-4">
              <div>
                <p className="text-sm font-medium text-foreground">Pay-as-you-go overage</p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  Continue using agents after your monthly credit budget is exhausted.
                  Billed in arrears at{" "}
                  {isPro ? "$3.00" : "$5.00"} per 1M credits.
                </p>
              </div>
              <button
                type="button"
                onClick={handleOverageToggle}
                disabled={overageLoading}
                aria-checked={sub.overageEnabled}
                role="switch"
                className={cn(
                  "relative shrink-0 mt-0.5 h-6 w-11 rounded-full border transition-colors focus:outline-none disabled:opacity-60",
                  sub.overageEnabled
                    ? "bg-primary border-primary"
                    : "bg-muted border-border",
                )}
              >
                <span
                  className={cn(
                    "absolute top-0.5 h-5 w-5 rounded-full bg-white shadow transition-transform",
                    sub.overageEnabled ? "translate-x-5" : "translate-x-0.5",
                  )}
                />
              </button>
            </div>
          )}

          {/* Actions */}
          <div className="flex flex-col gap-3 sm:flex-row">
            {!isPro && sub.billingEnabled && (
              <button
                type="button"
                onClick={handleUpgrade}
                disabled={checkoutLoading}
                className="flex items-center justify-center gap-2 rounded-xl bg-primary text-primary-foreground px-5 py-2.5 text-sm font-medium hover:bg-primary/90 transition-colors disabled:opacity-70 disabled:cursor-not-allowed"
              >
                {checkoutLoading && <Loader2 className="h-4 w-4 animate-spin" />}
                {checkoutLoading ? "Redirecting…" : "Upgrade to Pro"}
              </button>
            )}

            {sub.billingEnabled && (
              <button
                type="button"
                onClick={handlePortal}
                disabled={portalLoading}
                className="flex items-center justify-center gap-2 rounded-xl border border-border bg-background text-foreground px-5 py-2.5 text-sm font-medium hover:bg-muted transition-colors disabled:opacity-70 disabled:cursor-not-allowed"
              >
                {portalLoading && <Loader2 className="h-4 w-4 animate-spin" />}
                {portalLoading ? "Opening portal…" : "Manage subscription"}
              </button>
            )}

            {!sub.billingEnabled && (
              <p className="text-sm text-muted-foreground py-2">
                Billing is not configured yet. Contact your administrator.
              </p>
            )}
          </div>
        </>
      )}
    </div>
  );
}
