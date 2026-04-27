"use client"

import { gql, useMutation, useQuery } from "@apollo/client"

export type Template = {
  name: string
  description: string
  integrations: string[]
  channels: string[]
  prompt: string
}

const TEMPLATES_QUERY = gql`
  query AgentTemplates {
    agentTemplates {
      id
      name
      description
      prompt
      integrations
      channels
      isBuiltin
    }
  }
`

const CREATE_FROM_TEMPLATE = gql`
  mutation CreateAgentFromTemplate($templateId: UUID!, $name: String!, $provider: String!, $model: String) {
    createAgentFromTemplate(templateId: $templateId, name: $name, provider: $provider, model: $model) {
      id
      name
    }
  }
`

export function useAgentTemplates(): {
  templates: Template[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(TEMPLATES_QUERY)
  const raw: Array<{
    id: string
    name: string
    description: string | null
    prompt: string | null
    integrations: string[] | null
    channels: string[] | null
  }> = data?.agentTemplates ?? []
  const templates: Template[] = raw.map((t) => ({
    name: t.name,
    description: t.description ?? "",
    prompt: t.prompt ?? "",
    integrations: t.integrations ?? [],
    channels: t.channels ?? [],
  }))
  return { templates, loading, error: error ?? undefined }
}

export function useCreateAgentFromTemplate() {
  const [fn, state] = useMutation(CREATE_FROM_TEMPLATE)
  return {
    createAgentFromTemplate: async (
      templateId: string,
      name: string,
      provider: string,
      model?: string,
    ) => {
      const { data } = await fn({
        variables: { templateId, name, provider, model },
      })
      return data?.createAgentFromTemplate as { id: string; name: string }
    },
    ...state,
  }
}
