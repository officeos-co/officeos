"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"
import { mockTemplates, type Template } from "@/data/agent-templates"

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
  mutation CreateAgentFromTemplate($input: CreateAgentFromTemplateInput!) {
    createAgentFromTemplate(input: $input) {
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
  const { data, loading, error } = useQuery(TEMPLATES_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { templates: mockTemplates, loading: false }
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
      if (USE_MOCKS) {
        // eslint-disable-next-line no-console
        console.info("[useCreateAgentFromTemplate mock]", { templateId, name, provider, model })
        return { id: `agt_mock_${Date.now().toString(36)}`, name }
      }
      const { data } = await fn({
        variables: { input: { templateId, name, provider, model } },
      })
      return data?.createAgentFromTemplate as { id: string; name: string }
    },
    ...state,
  }
}
