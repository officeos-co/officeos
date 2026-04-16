"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { Switch } from "@/components/ui/switch"
import { Separator } from "@/components/ui/separator"
import {
  SparklesIcon,
  CreditCardIcon,
  CalendarIcon,
  ExternalLinkIcon,
} from "lucide-react"

import { useBilling } from "@/hooks/useBilling"

export default function BillingPage() {
  const router = useRouter()
  const { billing: mockBilling } = useBilling()
  const [autoReload, setAutoReload] = useState(mockBilling.extraUsage.autoReload)

  return (
    <>
      <PageHeader group="Manage" page="Billing" />
      <div className="flex flex-1 flex-col gap-6 p-4 pt-0 max-w-3xl mx-auto w-full">
        {/* Current plan */}
        <section>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div className="flex size-12 items-center justify-center rounded-xl border border-border">
                <SparklesIcon className="size-6 text-primary" />
              </div>
              <div>
                <h2 className="text-base font-semibold">{mockBilling.plan} plan</h2>
                <p className="text-sm text-muted-foreground">{mockBilling.planDescription}</p>
              </div>
            </div>
            <Button variant="outline" size="sm" onClick={() => router.push("/pricing")}>
              Adjust plan
            </Button>
          </div>

          {/* Cancellation / renewal notice */}
          {mockBilling.canceledAt ? (
            <div className="mt-4 flex items-center justify-between rounded-xl border border-border px-4 py-3">
              <div className="flex items-center gap-3">
                <CalendarIcon className="size-4 text-muted-foreground" />
                <span className="text-sm">Your subscription will be canceled on {mockBilling.canceledAt}.</span>
              </div>
              <Button variant="outline" size="sm">Resubscribe</Button>
            </div>
          ) : (
            <p className="mt-3 text-xs text-muted-foreground">
              Renews on {mockBilling.renewsAt}
            </p>
          )}
        </section>

        <Separator />

        {/* Payment method */}
        <section>
          <h3 className="text-sm font-semibold mb-3">Payment</h3>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <CreditCardIcon className="size-5 text-muted-foreground" />
              <span className="text-sm">{mockBilling.payment.brand} •••• {mockBilling.payment.last4}</span>
            </div>
            <Button variant="outline" size="sm">Update</Button>
          </div>
        </section>

        <Separator />

        {/* Extra usage */}
        <section>
          <h3 className="text-sm font-semibold mb-1">Extra usage</h3>
          <p className="text-xs text-muted-foreground mb-4">
            Buy extra credits so your agents can keep running when you hit a limit.
          </p>

          <div className="flex items-center justify-between mb-4">
            <div>
              <p className="text-lg font-semibold">€{mockBilling.extraUsage.balance.toFixed(2)}</p>
              <p className="text-xs text-muted-foreground">Current balance</p>
            </div>
            <Button variant="outline" size="sm">
              Buy more
            </Button>
          </div>

          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium">Auto-reload</p>
              <p className="text-xs text-muted-foreground">Automatically buy more credits when your balance is low.</p>
            </div>
            <Switch checked={autoReload} onCheckedChange={setAutoReload} />
          </div>
        </section>

        <Separator />

        {/* Invoices */}
        <section>
          <h3 className="text-sm font-semibold mb-3">Invoices</h3>
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
              {mockBilling.invoices.map((inv) => (
                <tr key={inv.date} className="border-b last:border-0">
                  <td className="px-0 py-2.5">{inv.date}</td>
                  <td className="px-0 py-2.5">{inv.total}</td>
                  <td className="px-0 py-2.5 text-muted-foreground">{inv.status}</td>
                  <td className="px-0 py-2.5">
                    <button type="button" className="text-sm hover:underline flex items-center gap-1">
                      View <ExternalLinkIcon className="size-3" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      </div>
    </>
  )
}
