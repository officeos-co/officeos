"use client"

import { gql, useQuery } from "@apollo/client"

/* ── Types ──────────────────────────────────────────────── */

export type DailyUsage = {
  date: string
  inputTokens: number
  outputTokens: number
  requests: number
  rateLimited: number
  webSearches: number
}

export type DailyCost = {
  date: string
  tokenCost: number
  webSearchCost: number
  codeExecCost: number
  runtimeCost: number
}

export type LogEntry = {
  id: string
  time: number
  model: string
  inputTokens: number
  outputTokens: number
  type: string
  serviceTier: string
  request: string
}

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
  const { data, loading, error } = useQuery(TOKEN_USAGE_QUERY)
  const raw = data?.tokenUsage as {
    creditsUsedThisMonth: number
    creditBudgetPerMonth: number
    creditsRemaining: number
    periodStart: string
    periodEnd: string
  } | null
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
  const { data, loading, error } = useQuery(COST_QUERY)
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
  return { logs: [], loading: false }
}
