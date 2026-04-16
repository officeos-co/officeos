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
  { id: "anthropic", name: "anthropic", displayName: "Anthropic", hasKey: true, models: ["claude-sonnet-4-6", "claude-opus-4-6", "claude-haiku-4-5"] },
  { id: "openai", name: "openai", displayName: "OpenAI", hasKey: true, models: ["gpt-4o", "gpt-4o-mini"] },
  { id: "google", name: "google", displayName: "Google", hasKey: false, models: ["gemini-2.5-pro"] },
]

const PROVIDERS_QUERY = gql`
  query Providers {
    providers {
      id
      name
      displayName
      hasKey
      models
    }
  }
`

const SET_PROVIDER_KEY = gql`
  mutation SetProviderKey($providerId: String!, $apiKey: String!) {
    setProviderKey(providerId: $providerId, apiKey: $apiKey)
  }
`

const CLEAR_PROVIDER_KEY = gql`
  mutation ClearProviderKey($providerId: String!) {
    clearProviderKey(providerId: $providerId)
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
      hasKey: boolean
      models: string[] | null
    }) => ({
      id: p.id,
      name: p.name,
      displayName: p.displayName ?? p.name,
      hasKey: p.hasKey,
      models: p.models ?? [],
    }),
  )
  return { providers, loading, error: error ?? undefined }
}

export function useSetProviderKey() {
  const [fn, state] = useMutation(SET_PROVIDER_KEY)
  return {
    setProviderKey: async (providerId: string, apiKey: string) => {
      if (USE_MOCKS) return true
      const { data } = await fn({ variables: { providerId, apiKey } })
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
      const { data } = await fn({ variables: { providerId } })
      return Boolean(data?.clearProviderKey)
    },
    ...state,
  }
}
