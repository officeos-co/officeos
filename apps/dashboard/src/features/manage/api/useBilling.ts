"use client"

import { gql, useMutation, useQuery } from "@apollo/client"

/* ── Types ──────────────────────────────────────────────── */

export type BillingInvoice = {
  id: string
  date: string
  total: string
  currency: string
  status: string
  hostedUrl: string | null
  pdfUrl: string | null
}

export type BillingPayload = {
  plan: string
  planDescription: string
  status: "active" | "canceled"
  billingCycle: string
  renewsAt: string
  canceledAt: string | null
  periodStart: string
  periodEnd: string
  creditBudgetPerMonth: number
  creditsUsedThisMonth: number
  creditsRemaining: number
  overBudget: boolean
  extraUsageEnabled: boolean
  payment: { brand: string; last4: string }
  invoices: BillingInvoice[]
  /** @deprecated use `extraUsageEnabled`. Kept so the legacy card render keeps compiling. */
  extraUsage: { balance: number; autoReload: boolean }
}

/* ── Queries / mutations ─────────────────────────────────── */

const BILLING_QUERY = gql`
  query Billing {
    billing {
      plan
      planDescription
      status
      billingCycle
      periodStart
      periodEnd
      creditBudgetPerMonth
      creditsUsedThisMonth
      creditsRemaining
      overBudget
      extraUsageEnabled
      paymentBrand
      paymentLast4
      invoices {
        id
        date
        total
        currency
        status
        hostedUrl
        pdfUrl
      }
    }
  }
`

const SET_EXTRA_USAGE_ENABLED = gql`
  mutation SetExtraUsageEnabled($enabled: Boolean!) {
    setExtraUsageEnabled(enabled: $enabled)
  }
`

/* ── Helpers ─────────────────────────────────────────────── */

function fmtDate(iso: string | null | undefined): string {
  if (!iso) return ""
  const t = Date.parse(iso)
  if (Number.isNaN(t)) return iso ?? ""
  return new Date(t).toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" })
}

type BillingRaw = {
  plan: string
  planDescription: string
  status: string
  billingCycle: string
  periodStart: string
  periodEnd: string
  creditBudgetPerMonth: number
  creditsUsedThisMonth: number
  creditsRemaining: number
  overBudget: boolean
  extraUsageEnabled: boolean
  paymentBrand: string | null
  paymentLast4: string | null
  invoices: Array<{
    id: string
    date: string
    total: string
    currency: string
    status: string
    hostedUrl: string | null
    pdfUrl: string | null
  }>
}

/* ── Hooks ───────────────────────────────────────────────── */

export function useBilling(): {
  billing: BillingPayload | null
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(BILLING_QUERY)
  const raw = data?.billing as BillingRaw | null | undefined
  if (!raw) return { billing: null, loading, error: error ?? undefined }
  const billing: BillingPayload = {
    plan: raw.plan,
    planDescription: raw.planDescription,
    status: (raw.status as BillingPayload["status"]) ?? "active",
    billingCycle: raw.billingCycle,
    renewsAt: fmtDate(raw.periodEnd),
    canceledAt: null,
    periodStart: raw.periodStart,
    periodEnd: raw.periodEnd,
    creditBudgetPerMonth: raw.creditBudgetPerMonth,
    creditsUsedThisMonth: raw.creditsUsedThisMonth,
    creditsRemaining: raw.creditsRemaining,
    overBudget: raw.overBudget,
    extraUsageEnabled: raw.extraUsageEnabled,
    payment: {
      brand: raw.paymentBrand ?? "Visa",
      last4: raw.paymentLast4 ?? "••••",
    },
    invoices: (raw.invoices ?? []).map((i) => ({
      id: i.id,
      date: fmtDate(i.date),
      total: `${i.currency === "EUR" ? "€" : i.currency + " "}${i.total}`,
      currency: i.currency,
      status: i.status.charAt(0).toUpperCase() + i.status.slice(1),
      hostedUrl: i.hostedUrl,
      pdfUrl: i.pdfUrl,
    })),
    extraUsage: { balance: 0, autoReload: raw.extraUsageEnabled },
  }
  return { billing, loading, error: error ?? undefined }
}

export function useSetExtraUsageEnabled() {
  const [fn, state] = useMutation(SET_EXTRA_USAGE_ENABLED)
  return {
    setExtraUsageEnabled: async (enabled: boolean): Promise<boolean> => {
      const { data } = await fn({
        variables: { enabled },
        optimisticResponse: { setExtraUsageEnabled: enabled },
        update(cache, result) {
          const existing = cache.readQuery<{ billing: BillingRaw }>({ query: BILLING_QUERY })
          if (existing?.billing) {
            const extraUsageEnabled = result.data?.setExtraUsageEnabled ?? enabled
            cache.writeQuery({
              query: BILLING_QUERY,
              data: { billing: { ...existing.billing, extraUsageEnabled } },
            })
          }
        },
      })
      return Boolean(data?.setExtraUsageEnabled)
    },
    ...state,
  }
}
