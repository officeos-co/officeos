"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"
import { mockBilling, type BillingPayload as LegacyBillingShape } from "@/data/billing-mock"
import {
  mockDailyUsage,
  mockDailyCost,
  mockLogs,
  type DailyUsage,
  type DailyCost,
  type LogEntry,
} from "@/data/analytics-mock"

/* ── Unified billing (billing page) ──────────────────────── */

export type BillingInvoice = {
  id: string
  date: string
  total: string
  currency: string
  status: string
  hostedUrl: string | null
  pdfUrl: string | null
}

/**
 * Billing view-model consumed by `/billing`. Backend returns plan, current
 * usage, extra-usage on/off (replaces old auto-reload), invoices synced
 * from Stripe. Preserves `.extraUsage.balance` / `.extraUsage.autoReload`
 * aliases so any remaining UI that reads the legacy shape keeps working —
 * but the canonical field is `.extraUsageEnabled`.
 */
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

function fmtDate(iso: string | null | undefined): string {
  if (!iso) return ""
  const t = Date.parse(iso)
  if (Number.isNaN(t)) return iso ?? ""
  return new Date(t).toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" })
}

function legacyToUnified(legacy: LegacyBillingShape): BillingPayload {
  return {
    plan: legacy.plan,
    planDescription: legacy.planDescription,
    status: legacy.status,
    billingCycle: "monthly",
    renewsAt: legacy.renewsAt,
    canceledAt: legacy.canceledAt,
    periodStart: "",
    periodEnd: "",
    creditBudgetPerMonth: 10_000_000,
    creditsUsedThisMonth: 0,
    creditsRemaining: 10_000_000,
    overBudget: false,
    extraUsageEnabled: legacy.extraUsage.autoReload,
    payment: legacy.payment,
    invoices: legacy.invoices.map((inv, i) => ({
      id: `inv_mock_${i}`,
      date: inv.date,
      total: inv.total,
      currency: "EUR",
      status: inv.status,
      hostedUrl: null,
      pdfUrl: null,
    })),
    extraUsage: legacy.extraUsage,
  }
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

export function useBilling(): {
  billing: BillingPayload | null
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(BILLING_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { billing: legacyToUnified(mockBilling), loading: false }
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
  const [fn, state] = useMutation(SET_EXTRA_USAGE_ENABLED, {
    refetchQueries: [{ query: BILLING_QUERY }],
  })
  return {
    setExtraUsageEnabled: async (enabled: boolean): Promise<boolean> => {
      if (USE_MOCKS) return enabled
      const { data } = await fn({ variables: { enabled } })
      return Boolean(data?.setExtraUsageEnabled)
    },
    ...state,
  }
}

/* ── Token usage (usage page) ────────────────────────────── */

const TOKEN_USAGE_QUERY = gql`
  query TokenUsage($range: Int) {
    tokenUsage(range: $range) {
      date
      inputTokens
      outputTokens
      requests
      rateLimited
      webSearches
    }
  }
`

export function useUsage(): {
  dailyUsage: DailyUsage[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(TOKEN_USAGE_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { dailyUsage: mockDailyUsage, loading: false }
  const dailyUsage: DailyUsage[] = (data?.tokenUsage ?? []).map(
    (d: Partial<DailyUsage>) => ({
      date: d.date ?? "",
      inputTokens: d.inputTokens ?? 0,
      outputTokens: d.outputTokens ?? 0,
      requests: d.requests ?? 0,
      rateLimited: d.rateLimited ?? 0,
      webSearches: d.webSearches ?? 0,
    }),
  )
  return { dailyUsage, loading, error: error ?? undefined }
}

/* ── Cost (cost page) ────────────────────────────────────── */

const COST_QUERY = gql`
  query Cost($range: Int) {
    modelCostWeights(range: $range) {
      date
      tokenCost
      webSearchCost
      codeExecCost
      runtimeCost
    }
  }
`

export function useCost(): {
  dailyCost: DailyCost[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(COST_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { dailyCost: mockDailyCost, loading: false }
  const dailyCost: DailyCost[] = (data?.modelCostWeights ?? []).map(
    (d: Partial<DailyCost>) => ({
      date: d.date ?? "",
      tokenCost: d.tokenCost ?? 0,
      webSearchCost: d.webSearchCost ?? 0,
      codeExecCost: d.codeExecCost ?? 0,
      runtimeCost: d.runtimeCost ?? 0,
    }),
  )
  return { dailyCost, loading, error: error ?? undefined }
}

/* ── Raw log entries (analytics) ─────────────────────────── */

export function useUsageLogs(): {
  logs: LogEntry[]
  loading: boolean
} {
  if (USE_MOCKS) return { logs: mockLogs, loading: false }
  return { logs: [], loading: false }
}
