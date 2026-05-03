"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { toast } from "sonner"
import { PageHeader } from "@/components/page-header"
import { PageContainer } from "@/components/page-container"
import { Button } from "@/components/ui/button"
import { HelpTooltip, WithTooltip } from "@/components/ui/help-tooltip"
import { Switch } from "@/components/ui/switch"
import { Separator } from "@/components/ui/separator"
import {
  SparklesIcon,
  CreditCardIcon,
  CalendarIcon,
  ExternalLinkIcon,
} from "lucide-react"
import { Skeleton } from "@/components/ui/skeleton"

import { useBilling, useSetExtraUsageEnabled } from "@/features/manage"

export default function BillingPage() {
  const router = useRouter()
  const { billing, loading, error } = useBilling()
  const { setExtraUsageEnabled } = useSetExtraUsageEnabled()

  useEffect(() => {
    if (error) toast.error("Failed to load billing", { description: error.message })
  }, [error])

  if (loading && !billing) {
    return (
      <>
        <PageHeader
          page="Billing"
          subtitle="Manage plan, usage, and payment settings."
          width="narrow"
        />
        <PageContainer width="narrow" className="flex flex-1 flex-col gap-6 pb-4">
          <section>
            <div className="flex items-center gap-4">
              <Skeleton className="size-12 rounded-xl" />
              <div className="space-y-2">
                <Skeleton className="h-5 w-24" />
                <Skeleton className="h-4 w-48" />
              </div>
            </div>
            <Skeleton className="h-3 w-32 mt-3" />
          </section>
          <Skeleton className="h-px w-full" />
          <section>
            <Skeleton className="h-4 w-24 mb-3" />
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-3 w-56 mt-1" />
          </section>
          <Skeleton className="h-px w-full" />
          <section>
            <Skeleton className="h-4 w-28 mb-3" />
            <Skeleton className="h-8 w-full rounded-md" />
          </section>
        </PageContainer>
      </>
    )
  }

  if (!billing) {
    return (
      <>
        <PageHeader
          page="Billing"
          subtitle="Manage plan, usage, and payment settings."
          width="narrow"
        />
        <div className="flex flex-col items-center justify-center py-20 gap-3">
          <SparklesIcon className="size-8 text-muted-foreground" />
          <h2 className="text-lg font-semibold">Billing is coming soon</h2>
          <p className="text-sm text-muted-foreground text-center max-w-md">
            You&apos;re on the free plan. Paid plans with higher limits, team features, and priority support are launching soon.
          </p>
        </div>
      </>
    )
  }

  return (
    <>
      <PageHeader
        page="Billing"
        subtitle="Manage plan, usage, and payment settings."
        width="narrow"
      />
      <PageContainer width="narrow" className="flex flex-1 flex-col gap-6 pb-4">
        {/* Current plan */}
        <section>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div className="flex size-12 items-center justify-center rounded-xl border border-border">
                <SparklesIcon className="size-6 text-primary" />
              </div>
              <div>
                <h2 className="text-base font-semibold capitalize">{billing.plan} plan</h2>
                <p className="text-sm text-muted-foreground">{billing.planDescription}</p>
              </div>
            </div>
            <WithTooltip tooltip="Review available plans and limits before changing subscription settings.">
              <Button variant="outline" size="sm" onClick={() => router.push("/pricing")}>
                Adjust plan
              </Button>
            </WithTooltip>
          </div>

          {billing.canceledAt ? (
            <div className="mt-4 flex items-center justify-between rounded-xl border border-border px-4 py-3">
              <div className="flex items-center gap-3">
                <CalendarIcon className="size-4 text-muted-foreground" />
                <span className="text-sm">Your subscription will be canceled on {billing.canceledAt}.</span>
              </div>
              <Button variant="outline" size="sm">Resubscribe</Button>
            </div>
          ) : (
            <p className="mt-3 text-xs text-muted-foreground">
              Renews on {billing.renewsAt}
            </p>
          )}
        </section>

        <Separator />

        {/* Current usage */}
        <section>
          <h3 className="mb-3 flex items-center gap-2 text-sm font-semibold">
            Current usage
            <HelpTooltip>
              Credits are usage accounting units derived from provider token
              usage and the model cost weight.
            </HelpTooltip>
          </h3>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-lg font-semibold">
                {billing.creditsUsedThisMonth.toLocaleString()} / {billing.creditBudgetPerMonth.toLocaleString()}
              </p>
              <p className="text-xs text-muted-foreground">
                credits this period · {billing.creditsRemaining.toLocaleString()} remaining
              </p>
            </div>
            {billing.overBudget && (
              <span className="rounded bg-destructive/10 px-2 py-1 text-xs font-medium text-destructive">
                Over budget
              </span>
            )}
          </div>
        </section>

        <Separator />

        {/* Payment method */}
        <section>
          <h3 className="mb-3 flex items-center gap-2 text-sm font-semibold">
            Payment
            <HelpTooltip>
              Payment methods are handled by Stripe. The dashboard only shows
              summary metadata.
            </HelpTooltip>
          </h3>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <CreditCardIcon className="size-5 text-muted-foreground" />
              <span className="text-sm">{billing.payment.brand} •••• {billing.payment.last4}</span>
            </div>
            <WithTooltip tooltip="Update payment details through the billing provider.">
              <Button variant="outline" size="sm">Update</Button>
            </WithTooltip>
          </div>
        </section>

        <Separator />

        {/* Extra usage — simple on/off toggle (replaces old auto-reload card) */}
        <section>
          <h3 className="mb-1 flex items-center gap-2 text-sm font-semibold">
            Extra usage
            <HelpTooltip>
              When disabled, agents stop once the monthly budget is exhausted
              instead of creating metered overage.
            </HelpTooltip>
          </h3>
          <p className="text-xs text-muted-foreground mb-4">
            When enabled, usage above your monthly credit budget is billed at your plan&apos;s metered rate so your agents keep running.
          </p>

          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium">Extra usage</p>
              <p className="text-xs text-muted-foreground">
                {billing.extraUsageEnabled ? "Enabled — overage billed via Stripe." : "Disabled — agents stop when budget is exhausted."}
              </p>
            </div>
            <WithTooltip tooltip="Toggle whether this workspace may continue running agents after the monthly credit budget is exhausted.">
              <Switch
                checked={billing.extraUsageEnabled}
                onCheckedChange={(v) => setExtraUsageEnabled(v)}
              />
            </WithTooltip>
          </div>
        </section>

        <Separator />

        {/* Invoices */}
        <section>
          <h3 className="text-sm font-semibold mb-3">Invoices</h3>
          {billing.invoices.length === 0 ? (
            <p className="text-sm text-muted-foreground">No invoices yet.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left">
                  <th className="px-0 py-2.5 font-medium">Date</th>
                  <th className="px-0 py-2.5 font-medium">Total</th>
                  <th className="px-0 py-2.5 font-medium">Status</th>
                  <th className="px-0 py-2.5 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {billing.invoices.map((inv) => (
                  <tr key={inv.id} className="border-b last:border-0">
                    <td className="px-0 py-2.5">{inv.date}</td>
                    <td className="px-0 py-2.5">{inv.total}</td>
                    <td className="px-0 py-2.5 text-muted-foreground">{inv.status}</td>
                    <td className="px-0 py-2.5">
                      {inv.hostedUrl ? (
                        <a
                          href={inv.hostedUrl}
                          target="_blank"
                          rel="noreferrer"
                          className="text-sm hover:underline flex items-center gap-1"
                        >
                          View <ExternalLinkIcon className="size-3" />
                        </a>
                      ) : (
                        <span className="text-sm text-muted-foreground">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      </PageContainer>
    </>
  )
}
