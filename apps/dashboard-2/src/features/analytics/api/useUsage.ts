"use client"

import { gql, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"
import {
  mockDailyUsage,
  mockDailyCost,
  mockLogs,
  type DailyUsage,
  type DailyCost,
  type LogEntry,
} from "../data/analytics-mock"

/* ── Token usage (usage page) ────────────────────────────── */

const TOKEN_USAGE_QUERY = gql`
  query TokenUsage($range: String) {
    tokenUsage(range: $range) {
      creditsUsedThisMonth
      creditBudgetPerMonth
      creditsRemaining
      periodStart
      periodEnd
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
  const raw = data?.tokenUsage as {
    creditsUsedThisMonth: number
    creditBudgetPerMonth: number
    creditsRemaining: number
    periodStart: string
    periodEnd: string
  } | null
  // Backend returns a summary, not daily breakdown — map to a single-entry array for the chart
  const dailyUsage: DailyUsage[] = raw
    ? [{
        date: raw.periodStart ?? "",
        inputTokens: raw.creditsUsedThisMonth,
        outputTokens: 0,
        requests: 0,
        rateLimited: 0,
        webSearches: 0,
      }]
    : []
  return { dailyUsage, loading, error: error ?? undefined }
}

/* ── Cost (cost page) ────────────────────────────────────── */

const COST_QUERY = gql`
  query Cost {
    modelCostWeights {
      model
      weight
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
  // Backend returns model cost weights (model + weight), not daily breakdown
  const raw: Array<{ model: string; weight: number }> = data?.modelCostWeights ?? []
  const dailyCost: DailyCost[] = raw.map((d) => ({
    date: d.model,
    tokenCost: d.weight,
    webSearchCost: 0,
    codeExecCost: 0,
    runtimeCost: 0,
  }))
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
