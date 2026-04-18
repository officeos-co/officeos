"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"

export type Provider = {
  id: string
  name: string
  displayName: string
  hasKey: boolean
  models: string[]
}

const mockProviders: Provider[] = [
  { id: "anthropic", name: "anthropic", displayName: "Anthropic", hasKey: true, models: [] },
  { id: "openai", name: "openai", displayName: "OpenAI", hasKey: true, models: [] },
  { id: "google", name: "google", displayName: "Google", hasKey: false, models: [] },
]

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

const SET_PROVIDER_KEY = gql`
  mutation SetProviderKey($providerName: String!, $apiKey: String!) {
    setProviderKey(providerName: $providerName, apiKey: $apiKey) {
      id
      name
      configured
    }
  }
`

const CLEAR_PROVIDER_KEY = gql`
  mutation ClearProviderKey($providerName: String!) {
    clearProviderKey(providerName: $providerName) {
      id
      name
      configured
    }
  }
`

export function useProviders(): {
  providers: Provider[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(PROVIDERS_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { providers: mockProviders, loading: false }
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

export function useSetProviderKey() {
  const [fn, state] = useMutation(SET_PROVIDER_KEY)
  return {
    setProviderKey: async (providerId: string, apiKey: string) => {
      if (USE_MOCKS) return true
      const { data } = await fn({ variables: { providerName: providerId, apiKey } })
      return Boolean(data?.setProviderKey)
    },
    ...state,
  }
}

export function useClearProviderKey() {
  const [fn, state] = useMutation(CLEAR_PROVIDER_KEY)
  return {
    clearProviderKey: async (providerId: string) => {
      if (USE_MOCKS) return true
      const { data } = await fn({ variables: { providerName: providerId } })
      return Boolean(data?.clearProviderKey)
    },
    ...state,
  }
}
