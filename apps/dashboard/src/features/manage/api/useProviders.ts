"use client"

import { gql, useMutation, useQuery } from "@apollo/client"

export type Provider = {
  id: string
  name: string
  displayName: string
  configured: boolean
  configuredAt: string | null
  models: string[]
}

export type ProviderEnvironment = {
  key: string
  value: string
}

export type ProviderSetupStatus = {
  provider: string
  displayName: string
  configured: boolean
  enabled: boolean
  authKind: string
  configuredAt: string
  pinnedModels: string[]
  environment: ProviderEnvironment[]
}

export type BedrockProviderSetupInput = {
  organizationId: string
  displayName: string
  awsRegion?: string | null
  authKind: string
  awsProfile?: string | null
  awsAccessKeyId?: string | null
  awsSecretAccessKey?: string | null
  awsSessionToken?: string | null
  bedrockApiKey?: string | null
  baseUrl?: string | null
  skipProviderAuth: boolean
  pinnedModels: string[]
  enabled: boolean
}

export type VertexProviderSetupInput = {
  organizationId: string
  displayName: string
  projectId?: string | null
  location?: string | null
  authKind: string
  credentialsPath?: string | null
  baseUrl?: string | null
  skipProviderAuth: boolean
  pinnedModels: string[]
  enabled: boolean
}

export type FoundryProviderSetupInput = {
  organizationId: string
  displayName: string
  resource?: string | null
  baseUrl?: string | null
  authKind: string
  apiKey?: string | null
  skipProviderAuth: boolean
  pinnedModels: string[]
  enabled: boolean
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

const PROVIDER_SETUP_STATUS_QUERY = gql`
  query ProviderSetupStatus($organizationId: UUID!) {
    providerSetupStatus(organizationId: $organizationId) {
      provider
      displayName
      configured
      enabled
      authKind
      configuredAt
      pinnedModels
      environment {
        key
        value
      }
    }
  }
`

const SAVE_BEDROCK_PROVIDER_SETUP = gql`
  mutation SaveBedrockProviderSetup($input: BedrockProviderSetupInput!) {
    saveBedrockProviderSetup(input: $input) {
      id
      provider
      displayName
      allowedModels
      enabled
      configuredAt
    }
  }
`

const SAVE_VERTEX_PROVIDER_SETUP = gql`
  mutation SaveVertexProviderSetup($input: VertexProviderSetupInput!) {
    saveVertexProviderSetup(input: $input) {
      id
      provider
      displayName
      allowedModels
      enabled
      configuredAt
    }
  }
`

const SAVE_FOUNDRY_PROVIDER_SETUP = gql`
  mutation SaveFoundryProviderSetup($input: FoundryProviderSetupInput!) {
    saveFoundryProviderSetup(input: $input) {
      id
      provider
      displayName
      allowedModels
      enabled
      configuredAt
    }
  }
`

export function useProviders() {
  const { data, loading, error, refetch } = useQuery(PROVIDERS_QUERY)

  const providers: Provider[] = data?.providers ?? []

  return { providers, loading, error, refetch }
}

export function useProviderSetupStatus(organizationId?: string | null) {
  const { data, loading, error, refetch } = useQuery(PROVIDER_SETUP_STATUS_QUERY, {
    variables: { organizationId },
    skip: !organizationId,
  })

  const statuses: ProviderSetupStatus[] = data?.providerSetupStatus ?? []

  return { statuses, loading, error, refetch }
}

export function useSaveBedrockProviderSetup() {
  const [fn, state] = useMutation(SAVE_BEDROCK_PROVIDER_SETUP)
  return {
    saveBedrockProviderSetup: async (input: BedrockProviderSetupInput) => {
      const { data } = await fn({ variables: { input } })
      return data?.saveBedrockProviderSetup
    },
    ...state,
  }
}

export function useSaveVertexProviderSetup() {
  const [fn, state] = useMutation(SAVE_VERTEX_PROVIDER_SETUP)
  return {
    saveVertexProviderSetup: async (input: VertexProviderSetupInput) => {
      const { data } = await fn({ variables: { input } })
      return data?.saveVertexProviderSetup
    },
    ...state,
  }
}

export function useSaveFoundryProviderSetup() {
  const [fn, state] = useMutation(SAVE_FOUNDRY_PROVIDER_SETUP)
  return {
    saveFoundryProviderSetup: async (input: FoundryProviderSetupInput) => {
      const { data } = await fn({ variables: { input } })
      return data?.saveFoundryProviderSetup
    },
    ...state,
  }
}
