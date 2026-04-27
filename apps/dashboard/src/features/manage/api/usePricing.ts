"use client"

import { gql, useMutation, useQuery } from "@apollo/client"

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
  const { data, loading, error } = useQuery(PLAN_LIMITS_QUERY)
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
      const { data } = await fn({ variables: { plan, billingCycle } })
      return (data?.subscribeUser?.checkoutUrl as string) ?? null
    },
    loading: state.loading,
  }
}
