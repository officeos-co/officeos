"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"
import { mockAgentsList, type AgentListRow } from "@/data/agents-list-mock"
import { mockAgent, type AgentDetail } from "@/data/agent-mock"

/* ── Queries / mutations ─────────────────────────────────── */

const AGENTS_QUERY = gql`
  query Agents {
    agents {
      id
      name
      model
      status
      createdAt
      updatedAt
    }
  }
`

const AGENT_QUERY = gql`
  query Agent($id: String!) {
    agent(id: $id) {
      id
      name
      model
      status
      prompt
      integrations
      channels
      createdAt
    }
  }
`

const CREATE_AGENT = gql`
  mutation CreateAgent($input: CreateAgentInput!) {
    createAgent(input: $input) {
      id
      name
    }
  }
`

const UPDATE_AGENT = gql`
  mutation UpdateAgent($id: String!, $input: UpdateAgentInput!) {
    updateAgent(id: $id, input: $input) {
      id
      name
    }
  }
`

const DELETE_AGENT = gql`
  mutation DeleteAgent($id: String!) {
    deleteAgent(id: $id)
  }
`

/* ── Helpers ─────────────────────────────────────────────── */

function humanAgo(iso: string | number | null | undefined): string {
  if (!iso) return ""
  const then = typeof iso === "number" ? iso : Date.parse(iso)
  if (Number.isNaN(then)) return ""
  const diffMs = Date.now() - then
  const m = Math.floor(diffMs / 60000)
  if (m < 1) return "just now"
  if (m < 60) return `${m} minute${m === 1 ? "" : "s"} ago`
  const h = Math.floor(m / 60)
  if (h < 24) return `${h} hour${h === 1 ? "" : "s"} ago`
  const d = Math.floor(h / 24)
  if (d < 14) return `${d} day${d === 1 ? "" : "s"} ago`
  const w = Math.floor(d / 7)
  return `${w} week${w === 1 ? "" : "s"} ago`
}

/* ── Hooks ───────────────────────────────────────────────── */

export function useAgents(): {
  agents: AgentListRow[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(AGENTS_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { agents: mockAgentsList, loading: false }
  const raw: Array<{
    id: string
    name: string
    model: string
    status: string
    createdAt?: string
    updatedAt?: string
  }> = data?.agents ?? []
  const agents: AgentListRow[] = raw.map((a) => ({
    id: a.id,
    name: a.name,
    model: a.model,
    status: (a.status ?? "stopped").toLowerCase(),
    created: humanAgo(a.createdAt),
    updated: humanAgo(a.updatedAt),
  }))
  return { agents, loading, error: error ?? undefined }
}

export function useAgent(id: string): {
  agent: AgentDetail | null
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(AGENT_QUERY, {
    variables: { id },
    skip: USE_MOCKS || !id,
  })
  if (USE_MOCKS) return { agent: mockAgent, loading: false }
  const a = data?.agent as
    | {
        id: string
        name: string
        model: string
        status: string
        prompt: string
        integrations: string[]
        channels: string[]
        createdAt?: string
      }
    | null
    | undefined
  if (!a) return { agent: null, loading, error: error ?? undefined }
  const agent: AgentDetail = {
    id: a.id,
    name: a.name,
    model: a.model,
    status: (a.status ?? "stopped").toLowerCase(),
    prompt: a.prompt ?? "",
    integrations: a.integrations ?? [],
    channels: a.channels ?? [],
    createdAt: a.createdAt ? Date.parse(a.createdAt) : Date.now(),
  }
  return { agent, loading, error: error ?? undefined }
}

export function useCreateAgent() {
  const [fn, state] = useMutation(CREATE_AGENT)
  return {
    createAgent: async (input: {
      name: string
      model: string
      prompt: string
      integrations: string[]
      channels: string[]
    }) => {
      if (USE_MOCKS) return { id: `agt_mock_${Date.now().toString(36)}`, name: input.name }
      const { data } = await fn({ variables: { input } })
      return data?.createAgent as { id: string; name: string }
    },
    ...state,
  }
}

export function useUpdateAgent() {
  const [fn, state] = useMutation(UPDATE_AGENT)
  return {
    updateAgent: async (
      id: string,
      input: Partial<{
        name: string
        model: string
        prompt: string
        integrations: string[]
        channels: string[]
      }>,
    ) => {
      if (USE_MOCKS) return { id, name: input.name ?? mockAgent.name }
      const { data } = await fn({ variables: { id, input } })
      return data?.updateAgent as { id: string; name: string }
    },
    ...state,
  }
}

export function useDeleteAgent() {
  const [fn, state] = useMutation(DELETE_AGENT)
  return {
    deleteAgent: async (id: string) => {
      if (USE_MOCKS) return true
      const { data } = await fn({ variables: { id } })
      return Boolean(data?.deleteAgent)
    },
    ...state,
  }
}
