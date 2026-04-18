"use client"

import { gql, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"

export type ModelInfo = {
  id: string
  displayName: string
  isDefault: boolean
}

const mockModels: ModelInfo[] = [
  { id: "auto", displayName: "Auto (smart routing)", isDefault: true },
  { id: "claude-haiku-4-5", displayName: "Claude Haiku 4.5", isDefault: false },
  { id: "claude-sonnet-4-6", displayName: "Claude Sonnet 4.6", isDefault: false },
  { id: "claude-opus-4-6", displayName: "Claude Opus 4.6", isDefault: false },
  { id: "gpt-4o", displayName: "GPT-4o", isDefault: false },
  { id: "gpt-4o-mini", displayName: "GPT-4o Mini", isDefault: false },
  { id: "gemini-2.5-pro", displayName: "Gemini 2.5 Pro", isDefault: false },
  { id: "gemini-2.5-flash", displayName: "Gemini 2.5 Flash", isDefault: false },
  { id: "grok-4", displayName: "Grok 4", isDefault: false },
]

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
  const { data, loading } = useQuery(SUPPORTED_MODELS_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { models: mockModels, defaultModelId: "auto", loading: false }
  const models: ModelInfo[] = data?.supportedModels ?? []
  const defaultModelId = models.find((m) => m.isDefault)?.id ?? "auto"
  return { models, defaultModelId, loading }
}
