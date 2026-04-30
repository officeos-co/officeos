"use client"

import { gql, useQuery } from "@apollo/client"

export type Provider = {
  id: string
  name: string
  displayName: string
  hasKey: boolean
  models: string[]
}

const PROVIDERS_QUERY = gql`
  query Providers {
    providers {
      id
      name
      displayName
      configured
      configuredAt
    }
  }
`

export function useProviders(): {
  providers: Provider[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(PROVIDERS_QUERY)
  const providers: Provider[] = (data?.providers ?? []).map(
    (p: {
      id: string
      name: string
      displayName: string | null
      configured: boolean
    }) => ({
      id: p.id,
      name: p.name,
      displayName: p.displayName ?? p.name,
      hasKey: p.configured,
      models: [],
    }),
  )
  return { providers, loading, error: error ?? undefined }
}
