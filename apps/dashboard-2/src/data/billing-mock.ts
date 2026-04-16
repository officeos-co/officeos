/**
 * Billing mock. Used to live inline inside `billing/page.tsx`. `useBilling`
 * falls back to this payload under USE_MOCKS. The backend exposes subscription,
 * plan limits, and model cost weights as separate GraphQL fields; the hook
 * projects them onto this shape so layout stays identical.
 */

export type BillingInvoice = {
  date: string
  total: string
  status: string
}

export type BillingPayload = {
  plan: string
  planDescription: string
  status: "active" | "canceled"
  renewsAt: string
  canceledAt: string | null
  payment: { brand: string; last4: string }
  extraUsage: { balance: number; autoReload: boolean }
  invoices: BillingInvoice[]
}

export const mockBilling: BillingPayload = {
  plan: "Pro",
  planDescription: "3 concurrent agents, 10M credits/month",
  status: "active",
  renewsAt: "May 16, 2026",
  canceledAt: null,
  payment: { brand: "Visa", last4: "5594" },
  extraUsage: { balance: 84.99, autoReload: false },
  invoices: [
    { date: "Apr 1, 2026", total: "€107,10", status: "Paid" },
    { date: "Mar 1, 2026", total: "€49,00", status: "Paid" },
    { date: "Feb 1, 2026", total: "€49,00", status: "Paid" },
    { date: "Jan 1, 2026", total: "€49,00", status: "Paid" },
  ],
}
