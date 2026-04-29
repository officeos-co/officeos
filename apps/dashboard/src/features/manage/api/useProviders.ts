"use client"

import { gql, useQuery, useMutation } from "@apollo/client"

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

const SET_PROVIDER_KEY = gql`
  mutation SetProviderKey($providerName: String!, $apiKey: String!) {
    setProviderKey(providerName: $providerName, apiKey: $apiKey) {
      id
      name
      displayName
      configured
      configuredAt
      models
    }
  }
`

const CLEAR_PROVIDER_KEY = gql`
  mutation ClearProviderKey($providerName: String!) {
    clearProviderKey(providerName: $providerName) {
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
  const [setKey] = useMutation(SET_PROVIDER_KEY)
  const [clearKey] = useMutation(CLEAR_PROVIDER_KEY)

  const providers: Provider[] = data?.providers ?? []

  async function setProviderKey(providerName: string, apiKey: string) {
    await setKey({
      variables: { providerName, apiKey },
      refetchQueries: ["Providers", "SupportedModels"],
    })
  }

  async function clearProviderKey(providerName: string) {
    await clearKey({
      variables: { providerName },
      refetchQueries: ["Providers", "SupportedModels"],
    })
  }

  return { providers, loading, error, refetch, setProviderKey, clearProviderKey }
}
