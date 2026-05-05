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

export type UsageAnalyticsPoint = {
  date: string;
  tokens: number;
  credits: number;
};

export type UsageAnalytics = {
  from: string;
  to: string;
  totalTokens: number;
  totalCredits: number;
  cost: {
    totalCents: number;
    includedCents: number;
    onDemandCents: number;
    currency: string;
    estimated: boolean;
  };
  points: UsageAnalyticsPoint[];
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

const USAGE_ANALYTICS_QUERY = gql`
  query UsageAnalytics($from: DateTime!, $to: DateTime!) {
    usageAnalytics(input: { from: $from, to: $to }) {
      from
      to
      totalTokens
      totalCredits
      cost {
        totalCents
        includedCents
        onDemandCents
        currency
        estimated
      }
      points {
        date
        tokens
        credits
      }
    }
  }
`;

export function useUsageAnalytics(from: string, to: string): {
  usage: UsageAnalytics | null;
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery(USAGE_ANALYTICS_QUERY, {
    variables: {
      from: `${from}T00:00:00.000Z`,
      to: `${to}T00:00:00.000Z`,
    },
    pollInterval: 5000,
    fetchPolicy: "network-only",
  });
  const raw = data?.usageAnalytics as UsageAnalytics | null;
  return { usage: raw ?? null, loading, error: error ?? undefined };
}

/* ── Raw log entries (analytics) ─────────────────────────── */

export type LogEntry = {
  id: string;
  time: number;
  agentName: string;
  inputTokens: number;
  outputTokens: number;
  durationMs: number;
};

export function useUsageLogs(): {
  logs: LogEntry[];
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery(gql`
    query UsageLogs {
      globalLogs(filters: { skip: 0, limit: 200 }) {
        items {
          id
          time
          content
          inputTokens
          outputTokens
          durationMs
          agentName
        }
      }
    }
  `, {
    pollInterval: 5000,
    fetchPolicy: "network-only",
  });

  const raw: Array<{
    id: string;
    time: string | number;
    content: string;
    inputTokens?: number | null;
    outputTokens?: number | null;
    durationMs?: number | null;
    agentName?: string | null;
  }> = data?.globalLogs?.items ?? [];

  const logs = raw
    .filter((r) => r.content.startsWith("LLM call complete"))
    .map((r) => ({
      id: r.id,
      time:
        typeof r.time === "number" ? r.time : Date.parse(r.time) || 0,
      agentName: r.agentName ?? "(unbound)",
      inputTokens: r.inputTokens ?? 0,
      outputTokens: r.outputTokens ?? 0,
      durationMs: r.durationMs ?? 0,
    }))
    .filter((r) => r.inputTokens + r.outputTokens > 0);

  return { logs, loading, error: error ?? undefined };
}
