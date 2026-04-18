"use client"

import { gql, useMutation, useLazyQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"

// ── Types ────────────────────────────────────────────────────────────────────

export type GdprExport = {
  user: {
    id: string
    email: string
    name: string | null
    createdAt: string
    lastLoginAt: string
  }
  agents: Array<{
    id: string
    name: string
    provider: string
    model: string | null
    status: string
    createdAt: string
  }>
  conversations: Array<{
    id: string
    agentId: string
    role: string
    content: string
    sessionId: string | null
    createdAt: string
  }>
  auditEntries: Array<{
    id: string
    agentId: string
    skillName: string
    action: string
    paramsJson: string
    resultSummary: string | null
    durationMs: number
    timestamp: string
  }>
  skillCredentials: Array<{
    id: string
    skillName: string
    enabled: boolean
    configuredAt: string | null
  }>
}

// ── GraphQL ──────────────────────────────────────────────────────────────────

const EXPORT_MY_DATA = gql`
  query ExportMyData {
    exportMyData {
      user { id email name createdAt lastLoginAt }
      agents { id name provider model status createdAt }
      conversations { id agentId role content sessionId createdAt }
      auditEntries { id agentId skillName action paramsJson resultSummary durationMs timestamp }
      skillCredentials { id skillName enabled configuredAt }
    }
  }
`

const PURGE_MY_DATA = gql`
  mutation PurgeMyData {
    purgeMyData
  }
`

// ── Hooks ────────────────────────────────────────────────────────────────────

export function useExportMyData() {
  if (USE_MOCKS) {
    return {
      exportData: async () => {
        console.debug("[mock] exportMyData")
        return null
      },
      loading: false,
      error: null,
      data: null as GdprExport | null,
    }
  }

  const [execute, { loading, error, data }] = useLazyQuery<{
    exportMyData: GdprExport
  }>(EXPORT_MY_DATA, { fetchPolicy: "no-cache" })

  return {
    exportData: async () => {
      const result = await execute()
      if (result.data?.exportMyData) {
        const json = JSON.stringify(result.data.exportMyData, null, 2)
        const blob = new Blob([json], { type: "application/json" })
        const url = URL.createObjectURL(blob)
        const a = document.createElement("a")
        a.href = url
        a.download = "eaos-export.json"
        a.click()
        URL.revokeObjectURL(url)
      }
      return result.data?.exportMyData ?? null
    },
    loading,
    error: error ?? null,
    data: data?.exportMyData ?? null,
  }
}

export function usePurgeMyData() {
  if (USE_MOCKS) {
    return {
      purgeData: async () => {
        console.debug("[mock] purgeMyData")
        return true
      },
      loading: false,
      error: null,
    }
  }

  const [execute, { loading, error }] = useMutation<{ purgeMyData: boolean }>(
    PURGE_MY_DATA
  )

  return {
    purgeData: async () => {
      const result = await execute()
      return result.data?.purgeMyData ?? false
    },
    loading,
    error: error ?? null,
  }
}
