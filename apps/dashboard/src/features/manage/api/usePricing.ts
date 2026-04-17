"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"

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

const MOCK_PLAN_LIMITS: PlanLimitsPayload = {
  individualFree: { plan: "free", concurrentAgents: 1, creditsPerMonth: 500_000 },
  individualPro: { plan: "pro", concurrentAgents: 3, creditsPerMonth: 10_000_000 },
  orgFree: { plan: "free", concurrentAgents: 1, creditsPerMonth: 500_000 },
  orgTeam: { plan: "team", concurrentAgents: 10, creditsPerMonth: 25_000_000 },
}

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

const SUBSCRIBE_MUTATION = gql`
  mutation SubscribeUser($plan: String!, $billingCycle: String!) {
    subscribeUser(plan: $plan, billingCycle: $billingCycle) {
      checkoutUrl
    }
  }
`

export function usePlanLimits(): {
  planLimits: PlanLimitsPayload | null
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(PLAN_LIMITS_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { planLimits: MOCK_PLAN_LIMITS, loading: false }
  const raw = data?.planLimits as PlanLimitsPayload | null | undefined
  if (!raw) return { planLimits: null, loading, error: error ?? undefined }
  return { planLimits: raw, loading, error: error ?? undefined }
}

export function useSubscribe(): {
  subscribe: (plan: string, billingCycle: string) => Promise<string | null>
  loading: boolean
} {
  const [fn, state] = useMutation(SUBSCRIBE_MUTATION)
  return {
    subscribe: async (plan: string, billingCycle: string) => {
      if (USE_MOCKS) return null
      const { data } = await fn({ variables: { plan, billingCycle } })
      return (data?.subscribeUser?.checkoutUrl as string) ?? null
    },
    loading: state.loading,
  }
}
