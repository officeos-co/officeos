"use client"

import { gql, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"
import { mockBilling, type BillingPayload } from "@/data/billing-mock"
import {
  mockDailyUsage,
  mockDailyCost,
  mockLogs,
  type DailyUsage,
  type DailyCost,
  type LogEntry,
} from "@/data/analytics-mock"

/* ── Subscription + plan (billing page) ──────────────────── */

const BILLING_QUERY = gql`
  query Billing {
    userSubscription {
      plan
      planDescription
      status
      renewsAt
      canceledAt
      extraBalance
      autoReload
      paymentBrand
      paymentLast4
      invoices {
        date
        total
        status
      }
    }
    planLimits {
      plan
      concurrentAgents
      monthlyCredits
    }
  }
`

export function useBilling(): {
  billing: BillingPayload
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(BILLING_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { billing: mockBilling, loading: false }
  const sub = data?.userSubscription as
    | {
        plan: string | null
        planDescription: string | null
        status: string | null
        renewsAt: string | null
        canceledAt: string | null
        extraBalance: number | null
        autoReload: boolean | null
        paymentBrand: string | null
        paymentLast4: string | null
        invoices?: Array<{ date: string; total: string; status: string }>
      }
    | null
    | undefined
  const billing: BillingPayload = {
    plan: sub?.plan ?? mockBilling.plan,
    planDescription: sub?.planDescription ?? mockBilling.planDescription,
    status: (sub?.status ?? mockBilling.status) as "active" | "canceled",
    renewsAt: sub?.renewsAt ?? mockBilling.renewsAt,
    canceledAt: sub?.canceledAt ?? null,
    payment: {
      brand: sub?.paymentBrand ?? mockBilling.payment.brand,
      last4: sub?.paymentLast4 ?? mockBilling.payment.last4,
    },
    extraUsage: {
      balance: sub?.extraBalance ?? mockBilling.extraUsage.balance,
      autoReload: sub?.autoReload ?? mockBilling.extraUsage.autoReload,
    },
    invoices: sub?.invoices ?? mockBilling.invoices,
  }
  return { billing, loading, error: error ?? undefined }
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
  // Not wired yet — backend exposes per-request usage via auditLog / tokenUsage.
  return { logs: [], loading: false }
}
