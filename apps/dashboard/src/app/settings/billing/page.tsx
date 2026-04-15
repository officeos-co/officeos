"use client";

import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Loader2 } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { TopBar } from "@/components/shared/TopBar";
import { cn } from "@/lib/utils";
import { isMockEnabled, MOCK_BILLING } from "@/lib/mock-data";

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
    if (isMockEnabled()) {
      setSub(MOCK_BILLING as UserSubscription);
      setLoading(false);
      return;
    }
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
      if (!res.ok) { setError("Could not start checkout."); return; }
      const { checkoutUrl } = await res.json();
      window.location.href = checkoutUrl;
    } catch {
      setError("Could not start checkout.");
    } finally {
      setCheckoutLoading(false);
    }
  }

  async function handlePortal() {
    setPortalLoading(true);
    try {
      const res = await fetch("/api/billing/user/portal", { credentials: "include" });
      if (!res.ok) { setError("Could not open billing portal."); return; }
      const { portalUrl } = await res.json();
      window.location.href = portalUrl;
    } catch {
      setError("Could not open billing portal.");
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
      if (!res.ok) { setError("Could not update overage setting."); return; }
      setSub((prev) => prev ? { ...prev, overageEnabled: !prev.overageEnabled } : prev);
    } catch {
      setError("Could not update overage setting.");
    } finally {
      setOverageLoading(false);
    }
  }

  const isPro = sub?.plan === "pro";

  return (
    <>
      <TopBar title="Billing" />
      <div className="max-w-lg px-6 py-6 space-y-6">
        {checkoutSuccess && (
          <div className="rounded-md border border-emerald-500/20 bg-emerald-500/5 px-4 py-2.5 text-[13px] text-emerald-600">
            You&apos;re now on Pro.
          </div>
        )}

        {error && (
          <div className="rounded-md border border-destructive/20 bg-destructive/5 px-4 py-2.5 text-[13px] text-destructive">
            {error}
          </div>
        )}

        {loading && (
          <div className="space-y-4">
            <Skeleton className="h-5 w-32" />
            <Skeleton className="h-2 w-full" />
            <Skeleton className="h-4 w-48" />
          </div>
        )}

        {sub && (
          <>
            {/* Plan info */}
            <section className="space-y-3">
              <div className="flex items-center gap-2">
                <h2 className="text-sm font-medium capitalize">{sub.plan} Plan</h2>
                {sub.isActive && (
                  <span className="text-[11px] text-emerald-600 font-medium">Active</span>
                )}
              </div>
              <p className="text-[12px] text-muted-foreground">
                {sub.concurrentAgentLimit} concurrent agent{sub.concurrentAgentLimit !== 1 ? "s" : ""}
                {sub.billingCycle && sub.plan !== "free" && ` · ${sub.billingCycle}`}
              </p>
            </section>

            {/* Credit usage */}
            <section className="space-y-2">
              <div className="flex justify-between text-[12px] text-muted-foreground">
                <span>{(sub.creditsUsedThisMonth / 1_000_000).toFixed(2)}M used</span>
                <span>{(sub.creditBudgetPerMonth / 1_000_000).toFixed(1)}M / month</span>
              </div>
              <div className="h-1.5 rounded-full bg-muted overflow-hidden">
                <div
                  className={cn(
                    "h-full rounded-full transition-all",
                    sub.creditBudgetPerMonth > 0 && (sub.creditsUsedThisMonth / sub.creditBudgetPerMonth) >= 0.8
                      ? "bg-amber-500"
                      : "bg-foreground",
                  )}
                  style={{ width: `${sub.creditBudgetPerMonth > 0 ? Math.min((sub.creditsUsedThisMonth / sub.creditBudgetPerMonth) * 100, 100) : 0}%` }}
                />
              </div>
              <p className="text-[11px] text-muted-foreground">
                {formatDate(sub.periodStart)} — {formatDate(sub.periodEnd)}
              </p>
            </section>

            {/* Overage */}
            {sub.billingEnabled && (
              <>
                <div className="border-t border-border" />
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-[13px] font-medium">Pay-as-you-go overage</p>
                    <p className="text-[12px] text-muted-foreground">
                      {isPro ? "$3.00" : "$5.00"} per 1M credits over budget.
                    </p>
                  </div>
                  <Switch
                    checked={sub.overageEnabled}
                    onCheckedChange={() => handleOverageToggle()}
                    disabled={overageLoading}
                  />
                </div>
              </>
            )}

            <div className="border-t border-border" />

            {/* Actions */}
            <div className="flex items-center gap-2">
              {!isPro && sub.billingEnabled && (
                <Button size="sm" onClick={handleUpgrade} disabled={checkoutLoading} className="h-7">
                  {checkoutLoading && <Loader2 className="h-3 w-3 animate-spin" />}
                  Upgrade to Pro
                </Button>
              )}
              {sub.billingEnabled && (
                <Button variant="outline" size="sm" onClick={handlePortal} disabled={portalLoading} className="h-7 text-[12px]">
                  {portalLoading && <Loader2 className="h-3 w-3 animate-spin" />}
                  Manage subscription
                </Button>
              )}
              {!sub.billingEnabled && (
                <p className="text-[13px] text-muted-foreground">
                  Billing is not configured. Contact your administrator.
                </p>
              )}
            </div>
          </>
        )}
      </div>
    </>
  );
}
