"use client";

import { gql, useQuery } from "@apollo/client";

/* ── Types ──────────────────────────────────────────────── */

export type CreditUsage = {
  creditsUsedThisMonth: number;
  creditBudgetPerMonth: number;
  creditsRemaining: number;
  overBudget: boolean;
  periodStart: string;
  periodEnd: string;
  plan: string;
  billingCycle: string;
};

export type ModelCostWeight = {
  model: string;
  weight: number;
};

/* ── Credit usage (usage page) ───────────────────────────── */

const TOKEN_USAGE_QUERY = gql`
  query TokenUsage($range: String) {
    tokenUsage(range: $range) {
      creditsUsedThisMonth
      creditBudgetPerMonth
      creditsRemaining
      overBudget
      overageEnabled
      periodStart
      periodEnd
      plan
      billingCycle
    }
  }
`;

export function useUsage(): {
  usage: CreditUsage | null;
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery(TOKEN_USAGE_QUERY);
  const raw = data?.tokenUsage as CreditUsage | null;
  return { usage: raw ?? null, loading, error: error ?? undefined };
}

/* ── Model cost weights (cost page) ──────────────────────── */

const MODEL_COST_WEIGHTS_QUERY = gql`
  query ModelCostWeights {
    modelCostWeights {
      model
      weight
    }
  }
`;

export function useCost(): {
  weights: ModelCostWeight[];
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery(MODEL_COST_WEIGHTS_QUERY);
  const weights: ModelCostWeight[] = data?.modelCostWeights ?? [];
  return { weights, loading, error: error ?? undefined };
}

/* ── Raw log entries (analytics) ─────────────────────────── */

export type LogEntry = {
  id: string;
  time: number;
  model: string;
  inputTokens: number;
  outputTokens: number;
  type: string;
  serviceTier: string;
  request: string;
};

export function useUsageLogs(): {
  logs: LogEntry[];
  loading: boolean;
} {
  return { logs: [], loading: false };
}
