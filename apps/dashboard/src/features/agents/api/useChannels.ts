"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import type { Channel } from "../data/channels"
import { sanitizeSvg } from "@/lib/sanitize-svg"
import type { AgentLog } from "@/types/logs"

const CHANNELS_QUERY = gql`
  query ChannelsAndTypes {
    channelTypes {
      type
      displayName
      description
      logo
      onboardingSteps {
        type
        title
        description
        value
        inputKey
        inputLabel
        inputPlaceholder
        inputHelp
        inputKind
        inputRequired
      }
    }
    channelConnections {
      id
      channelType
      displayName
      enabled
      createdAt
    }
  }
`

const CHANNEL_CONNECTION_QUERY = gql`
  query ChannelConnection($id: UUID!) {
    channelConnection(id: $id) {
      id
      channelType
      displayName
      enabled
      createdAt
    }
    channelTypes {
      type
      displayName
      description
      logo
      onboardingSteps {
        type
        title
        description
        value
        inputKey
        inputLabel
        inputPlaceholder
        inputHelp
        inputKind
        inputRequired
      }
    }
  }
`

const CHANNEL_LOGS_QUERY = gql`
  query ChannelLogs($channelConnectionId: UUID!, $last: Int!) {
    channelLogs(channelConnectionId: $channelConnectionId, last: $last) {
      nodes {
        id
        agentId
        agentName
        time
        type
        tool
        integration
        channel
        channelConnectionId
        content
        durationMs
        inputTokens
        outputTokens
        correlationId
      }
    }
  }
`

export type ChannelConnection = {
  id: string
  channelType: string
  displayName: string
  enabled: boolean
  createdAt: string
  typeDisplayName: string
  description: string
  logo: string
}

const CREATE_CONNECTION = gql`
  mutation CreateChannelConnection($input: CreateChannelConnectionInput!) {
    createChannelConnection(input: $input) {
      id
      channelType
      displayName
    }
  }
`

const DELETE_CONNECTION = gql`
  mutation DeleteChannelConnection($id: UUID!) {
    deleteChannelConnection(id: $id)
  }
`

const BIND_CHANNEL = gql`
  mutation BindChannelToAgent($agentId: UUID!, $channelConnectionId: UUID!) {
    bindChannelToAgent(agentId: $agentId, channelConnectionId: $channelConnectionId) {
      id
      agentId
      channelConnectionId
    }
  }
`


export function useChannels(): {
  channels: Channel[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(CHANNELS_QUERY)

  const types: Array<{
    type: string
    displayName: string
    description: string | null
    logo: string | null
    onboardingSteps: Array<{
      type: string
      title: string
      description: string
      value?: string | null
      inputKey?: string | null
      inputLabel?: string | null
      inputPlaceholder?: string | null
      inputHelp?: string | null
      inputKind?: string | null
      inputRequired?: boolean | null
    }>
  }> = data?.channelTypes ?? []
  const connections: Array<{ id: string; channelType: string }> = data?.channelConnections ?? []
  const connectedSlugs = new Set(connections.map((c) => c.channelType))
  const connectionByType = new Map(connections.map((c) => [c.channelType, c.id]))

  const channels: Channel[] = types.map((t) => ({
    name: t.displayName,
    slug: t.type,
    logo: sanitizeSvg(t.logo ?? ""),
    description: t.description ?? "",
    defaultPermissions: { receive: "ask" as const, send: "ask" as const, initiate: "ask" as const },
    added: connectedSlugs.has(t.type),
    connectionId: connectionByType.get(t.type) ?? null,
    onboarding: (t.onboardingSteps ?? []).map((s) => ({
      type: s.type as "url" | "qr" | "input" | "copy",
      title: s.title,
      description: s.description,
      value: s.value ?? undefined,
      inputKey: s.inputKey ?? undefined,
      inputLabel: s.inputLabel ?? undefined,
      inputPlaceholder: s.inputPlaceholder ?? undefined,
      inputHelp: s.inputHelp ?? undefined,
      inputKind: (s.inputKind ?? "text") as "text" | "password" | "textarea",
      inputRequired: s.inputRequired ?? true,
    })),
  }))

  return { channels, loading, error: error ?? undefined }
}

function toChannelTypes(data: unknown): Channel[] {
  const d = (data ?? {}) as {
    channelTypes?: Array<{
      type: string
      displayName: string
      description: string | null
      logo: string | null
      onboardingSteps: Array<{
        type: string
        title: string
        description: string
        value?: string | null
        inputKey?: string | null
        inputLabel?: string | null
        inputPlaceholder?: string | null
        inputHelp?: string | null
        inputKind?: string | null
        inputRequired?: boolean | null
      }>
    }>
    channelConnections?: Array<{ id: string; channelType: string }>
  }
  const types = d.channelTypes ?? []
  const connections = d.channelConnections ?? []
  const connectedSlugs = new Set(connections.map((c) => c.channelType))
  const connectionByType = new Map(connections.map((c) => [c.channelType, c.id]))
  return types.map((t) => ({
    name: t.displayName,
    slug: t.type,
    logo: sanitizeSvg(t.logo ?? ""),
    description: t.description ?? "",
    defaultPermissions: { receive: "ask" as const, send: "ask" as const, initiate: "ask" as const },
    added: connectedSlugs.has(t.type),
    connectionId: connectionByType.get(t.type) ?? null,
    onboarding: (t.onboardingSteps ?? []).map((s) => ({
      type: s.type as "url" | "qr" | "input" | "copy",
      title: s.title,
      description: s.description,
      value: s.value ?? undefined,
      inputKey: s.inputKey ?? undefined,
      inputLabel: s.inputLabel ?? undefined,
      inputPlaceholder: s.inputPlaceholder ?? undefined,
      inputHelp: s.inputHelp ?? undefined,
      inputKind: (s.inputKind ?? "text") as "text" | "password" | "textarea",
      inputRequired: s.inputRequired ?? true,
    })),
  }))
}

export function useChannelConnections(): {
  connections: ChannelConnection[]
  channelTypes: Channel[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(CHANNELS_QUERY)
  const channelTypes = toChannelTypes(data)
  const typeBySlug = new Map(channelTypes.map((type) => [type.slug, type]))
  const rawConnections: Array<{
    id: string
    channelType: string
    displayName: string
    enabled: boolean
    createdAt: string
  }> = data?.channelConnections ?? []

  return {
    channelTypes,
    connections: rawConnections.map((connection) => {
      const type = typeBySlug.get(connection.channelType)
      return {
        ...connection,
        typeDisplayName: type?.name ?? connection.channelType,
        description: type?.description ?? "",
        logo: type?.logo ?? "",
      }
    }),
    loading,
    error: error ?? undefined,
  }
}

export function useChannelConnection(id: string): {
  connection: ChannelConnection | null
  channelTypes: Channel[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(CHANNEL_CONNECTION_QUERY, {
    variables: { id },
    skip: !id,
    fetchPolicy: "cache-and-network",
  })
  const channelTypes = toChannelTypes(data)
  const raw = data?.channelConnection
  const type = raw ? channelTypes.find((item) => item.slug === raw.channelType) : null
  return {
    channelTypes,
    connection: raw
      ? {
          id: raw.id,
          channelType: raw.channelType,
          displayName: raw.displayName,
          enabled: raw.enabled,
          createdAt: raw.createdAt,
          typeDisplayName: type?.name ?? raw.channelType,
          description: type?.description ?? "",
          logo: type?.logo ?? "",
        }
      : null,
    loading,
    error: error ?? undefined,
  }
}

type RawChannelLog = {
  id: string
  agentId?: string | null
  agentName?: string | null
  time: string | number
  type: string
  tool?: string | null
  integration?: string | null
  channel?: string | null
  channelConnectionId?: string | null
  content: string
  durationMs?: number | null
  inputTokens?: number | null
  outputTokens?: number | null
  correlationId?: string | null
}

function normaliseLogType(raw: string | null | undefined): AgentLog["type"] {
  if (!raw) return "system"
  const value = raw.toString()
  if (value.includes("_")) return value as AgentLog["type"]
  const map: Record<string, AgentLog["type"]> = {
    ToolCall: "tool_call",
    ToolResult: "tool_result",
    MessageIn: "message_in",
    MessageOut: "message_out",
    ChannelIn: "channel_in",
    ChannelOut: "channel_out",
    System: "system",
    AgentStartup: "agent_startup",
    AgentShutdown: "agent_shutdown",
    Error: "error",
  }
  return map[value] ?? "system"
}

function toMillis(time: string | number) {
  if (typeof time === "number") return time
  const parsed = Date.parse(time)
  return Number.isFinite(parsed) ? parsed : 0
}

export function useChannelLogs(channelConnectionId: string, limit = 200): {
  logs: (AgentLog & { agentName?: string })[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(CHANNEL_LOGS_QUERY, {
    variables: { channelConnectionId, last: limit },
    skip: !channelConnectionId,
    fetchPolicy: "network-only",
  })
  const raw: RawChannelLog[] = data?.channelLogs?.nodes ?? []
  return {
    logs: raw.map((log) => ({
      id: log.id,
      time: toMillis(log.time),
      type: normaliseLogType(log.type),
      tool: log.tool ?? undefined,
      integration: log.integration ?? undefined,
      channel: log.channel ?? undefined,
      channelConnectionId: log.channelConnectionId ?? undefined,
      content: log.content,
      durationMs: log.durationMs ?? undefined,
      tokens:
        log.inputTokens != null || log.outputTokens != null
          ? { input: log.inputTokens ?? 0, output: log.outputTokens ?? 0 }
          : undefined,
      correlationId: log.correlationId ?? undefined,
      agentName: log.agentName ?? undefined,
    })),
    loading,
    error: error ?? undefined,
  }
}

export function useCreateChannelConnection() {
  const [fn, state] = useMutation(CREATE_CONNECTION)
  return {
    createChannelConnection: async (input: {
      channelType: string
      displayName: string
      config: Record<string, string>
    }) => {
      const optimisticId = `conn_optimistic_${Date.now().toString(36)}`
      const { data } = await fn({
        variables: {
          input: {
            channelType: input.channelType,
            displayName: input.displayName,
            configJson: JSON.stringify(input.config),
          },
        },
        optimisticResponse: {
          createChannelConnection: {
            __typename: "ChannelConnection",
            id: optimisticId,
            channelType: input.channelType,
            displayName: input.displayName,
          },
        },
        update(cache, { data: result }) {
          if (!result?.createChannelConnection) return
          const existing = cache.readQuery<{
            channelTypes: unknown[]
            channelConnections: Array<{ id: string; channelType: string; displayName: string; enabled: boolean; createdAt: string }>
          }>({ query: CHANNELS_QUERY })
          if (existing) {
            cache.writeQuery({
              query: CHANNELS_QUERY,
              data: {
                channelTypes: existing.channelTypes,
                channelConnections: [
                  ...existing.channelConnections,
                  {
                    __typename: "ChannelConnection",
                    id: result.createChannelConnection.id,
                    channelType: result.createChannelConnection.channelType,
                    displayName: result.createChannelConnection.displayName,
                    enabled: true,
                    createdAt: new Date().toISOString(),
                  },
                ],
              },
            })
          }
        },
      })
      return data?.createChannelConnection as { id: string; channelType: string; displayName: string }
    },
    ...state,
  }
}

export function useDeleteChannelConnection() {
  const [fn, state] = useMutation(DELETE_CONNECTION)
  return {
    deleteChannelConnection: async (id: string) => {
      const { data } = await fn({
        variables: { id },
        optimisticResponse: { deleteChannelConnection: true },
        update(cache) {
          const existing = cache.readQuery<{
            channelTypes: unknown[]
            channelConnections: Array<{ id: string; channelType: string; displayName: string; enabled: boolean; createdAt: string }>
          }>({ query: CHANNELS_QUERY })
          if (existing) {
            cache.writeQuery({
              query: CHANNELS_QUERY,
              data: {
                channelTypes: existing.channelTypes,
                channelConnections: existing.channelConnections.filter((c) => c.id !== id),
              },
            })
          }
          cache.evict({ id: cache.identify({ __typename: "ChannelConnection", id }) })
          cache.gc()
        },
      })
      return Boolean(data?.deleteChannelConnection)
    },
    ...state,
  }
}

const AGENT_FRAGMENT = gql`
  fragment AgentChannelBindings on Agent {
    channelBindings {
      id
      channelConnectionId
    }
  }
`

export function useBindChannelToAgent() {
  const [fn, state] = useMutation(BIND_CHANNEL)
  return {
    bindChannelToAgent: async (connectionId: string, agentId: string) => {
      const { data } = await fn({
        variables: { agentId, channelConnectionId: connectionId },
        optimisticResponse: {
          bindChannelToAgent: {
            __typename: "AgentChannelBindingGqlDto",
            id: `bind_optimistic_${Date.now().toString(36)}`,
            agentId,
            channelConnectionId: connectionId,
          },
        },
        update(cache, { data: mutData }) {
          if (!mutData?.bindChannelToAgent) return
          const binding = mutData.bindChannelToAgent
          const id = cache.identify({ __typename: "Agent", id: agentId })
          if (!id) return
          const existing = cache.readFragment<{
            channelBindings: Array<{ id: string; channelConnectionId: string }>
          }>({ id, fragment: AGENT_FRAGMENT })
          const bindings = existing?.channelBindings ?? []
          if (bindings.some((b) => b.channelConnectionId === connectionId)) return
          cache.writeFragment({
            id,
            fragment: AGENT_FRAGMENT,
            data: { channelBindings: [...bindings, binding] },
          })
        },
      })
      return Boolean(data?.bindChannelToAgent)
    },
    ...state,
  }
}

const UNBIND_CHANNEL = gql`
  mutation UnbindChannelFromAgent($agentId: UUID!, $channelConnectionId: UUID!) {
    unbindChannelFromAgent(agentId: $agentId, channelConnectionId: $channelConnectionId)
  }
`

export function useUnbindChannelFromAgent() {
  const [fn, state] = useMutation(UNBIND_CHANNEL)
  return {
    unbindChannelFromAgent: async (connectionId: string, agentId: string) => {
      const { data } = await fn({
        variables: { agentId, channelConnectionId: connectionId },
        optimisticResponse: { unbindChannelFromAgent: true },
        update(cache) {
          const id = cache.identify({ __typename: "Agent", id: agentId })
          if (!id) return
          const existing = cache.readFragment<{
            channelBindings: Array<{ id: string; channelConnectionId: string }>
          }>({ id, fragment: AGENT_FRAGMENT })
          if (!existing) return
          cache.writeFragment({
            id,
            fragment: AGENT_FRAGMENT,
            data: {
              channelBindings: existing.channelBindings.filter(
                (b) => b.channelConnectionId !== connectionId,
              ),
            },
          })
        },
      })
      return Boolean(data?.unbindChannelFromAgent)
    },
    ...state,
  }
}
