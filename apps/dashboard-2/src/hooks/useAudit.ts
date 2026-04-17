"use client"

import { gql, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"

export type AuditEntry = {
  id: string
  time: number
  actor: string
  action: string
  target: string
  detail: string
}

const AUDIT_QUERY = gql`
  query AuditLog($agentId: UUID!, $skip: Int!, $limit: Int!) {
    auditLog(agentId: $agentId, skip: $skip, limit: $limit) {
      items {
        id
        agentId
        userId
        skillName
        action
        paramsJson
        resultSummary
        durationMs
        timestamp
      }
      total
    }
  }
`

const mockAudit: AuditEntry[] = []

export function useAudit(
  agentId?: string,
  skip = 0,
  limit = 100,
): { entries: AuditEntry[]; loading: boolean; error?: Error } {
  const { data, loading, error } = useQuery(AUDIT_QUERY, {
    variables: { agentId, skip, limit },
    skip: USE_MOCKS,
  })
  if (USE_MOCKS) return { entries: mockAudit, loading: false }
  const raw: Array<{
    id: string
    agentId: string
    userId: string | null
    skillName: string
    action: string
    paramsJson: string
    resultSummary: string | null
    durationMs: number
    timestamp: string | number
  }> = data?.auditLog?.items ?? []
  const entries: AuditEntry[] = raw.map((e) => ({
    id: e.id,
    time: typeof e.timestamp === "number" ? e.timestamp : Date.parse(e.timestamp) || Date.now(),
    actor: e.skillName,
    action: e.action,
    target: e.agentId,
    detail: e.resultSummary ?? e.paramsJson,
  }))
  return { entries, loading, error: error ?? undefined }
}
