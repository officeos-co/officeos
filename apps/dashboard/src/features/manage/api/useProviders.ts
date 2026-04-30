"use client"

import { gql, useQuery } from "@apollo/client"

export type Provider = {
  id: string
  name: string
  displayName: string
  configured: boolean
  configuredAt: string | null
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
      models
    }
  }
`

export function useProviders() {
  const { data, loading, error, refetch } = useQuery(PROVIDERS_QUERY)

  const providers: Provider[] = data?.providers ?? []

  return { providers, loading, error, refetch }
}
