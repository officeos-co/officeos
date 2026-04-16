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
  query AuditLog($agentId: String, $skip: Int, $limit: Int) {
    auditLog(agentId: $agentId, skip: $skip, limit: $limit) {
      id
      time
      actor
      action
      target
      detail
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
  const entries: AuditEntry[] = (data?.auditLog ?? []).map(
    (e: {
      id: string
      time: string | number
      actor: string
      action: string
      target: string
      detail: string
    }) => ({
      id: e.id,
      time: typeof e.time === "number" ? e.time : Date.parse(e.time) || Date.now(),
      actor: e.actor,
      action: e.action,
      target: e.target,
      detail: e.detail,
    }),
  )
  return { entries, loading, error: error ?? undefined }
}
