"use client"

import { gql, useQuery } from "@apollo/client"

export type ModelInfo = {
  id: string
  displayName: string
  isDefault: boolean
}

const SUPPORTED_MODELS_QUERY = gql`
  query SupportedModels {
    supportedModels {
      id
      displayName
      isDefault
    }
  }
`

export function useModels(): {
  models: ModelInfo[]
  defaultModelId: string
  loading: boolean
} {
  const { data, loading } = useQuery(SUPPORTED_MODELS_QUERY)
  const models: ModelInfo[] = data?.supportedModels ?? []
  const defaultModelId = models.find((m) => m.isDefault)?.id ?? "auto"
  return { models, defaultModelId, loading }
}
