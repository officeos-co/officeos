"use client"

import { gql, useMutation, useQuery } from "@apollo/client"

/* ── Types ──────────────────────────────────────────────── */

export type PlanLimit = {
  plan: string
  concurrentAgents: number
  creditsPerMonth: number
}

export type PlanLimitsPayload = {
  individualFree: PlanLimit
  individualPro: PlanLimit
  orgFree: PlanLimit
  orgTeam: PlanLimit
}

export type PlanPrice = {
  plan: string
  monthlyAmountCents: number
  yearlyAmountCents: number
  currency: string
}

/* ── Queries / mutations ─────────────────────────────────── */

const PLAN_LIMITS_QUERY = gql`
  query PlanLimits {
    planLimits {
      individualFree { plan concurrentAgents creditsPerMonth }
      individualPro { plan concurrentAgents creditsPerMonth }
      orgFree { plan concurrentAgents creditsPerMonth }
      orgTeam { plan concurrentAgents creditsPerMonth }
    }
  }
`

const PLAN_PRICES_QUERY = gql`
  query PlanPrices {
    planPrices {
      plan
      monthlyAmountCents
      yearlyAmountCents
      currency
    }
  }
`

const SUBSCRIBE_MUTATION = gql`
  mutation SubscribeUser($plan: String!, $billingCycle: String!) {
    subscribeUser(plan: $plan, billingCycle: $billingCycle) {
      checkoutUrl
    }
  }
`

/* ── Hooks ───────────────────────────────────────────────── */

export function usePlanLimits(): {
  planLimits: PlanLimitsPayload | null
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(PLAN_LIMITS_QUERY)
  const raw = data?.planLimits as PlanLimitsPayload | null | undefined
  if (!raw) return { planLimits: null, loading, error: error ?? undefined }
  return { planLimits: raw, loading, error: error ?? undefined }
}

export function usePlanPrices(): {
  prices: Record<string, { monthly: number; yearly: number }> | null
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(PLAN_PRICES_QUERY)
  const raw = data?.planPrices as PlanPrice[] | null | undefined
  if (!raw) return { prices: null, loading, error: error ?? undefined }

  const prices: Record<string, { monthly: number; yearly: number }> = {}
  for (const p of raw) {
    prices[p.plan] = { monthly: p.monthlyAmountCents, yearly: p.yearlyAmountCents }
  }
  return { prices, loading, error: error ?? undefined }
}

export function useSubscribe(): {
  subscribe: (plan: string, billingCycle: string) => Promise<string | null>
  loading: boolean
} {
  const [fn, state] = useMutation(SUBSCRIBE_MUTATION)
  return {
    subscribe: async (plan: string, billingCycle: string) => {
      const { data } = await fn({ variables: { plan, billingCycle } })
      return (data?.subscribeUser?.checkoutUrl as string) ?? null
    },
    loading: state.loading,
  }
}
