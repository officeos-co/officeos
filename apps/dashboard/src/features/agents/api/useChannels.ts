"use client"

import { gql, useMutation, useQuery, useSubscription } from "@apollo/client"
import type { Channel } from "../data/channels"
import { sanitizeSvg } from "@/lib/sanitize-svg"

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

const WHATSAPP_STATUS_SUBSCRIPTION = gql`
  subscription WhatsAppConnectionStatus($connectionId: UUID!) {
    whatsAppConnectionStatus(connectionId: $connectionId) {
      connectionId
      status
      qrData
    }
  }
`

export function useWhatsAppConnectionStatus(connectionId: string | null) {
  const { data, loading, error } = useSubscription(WHATSAPP_STATUS_SUBSCRIPTION, {
    variables: { connectionId },
    skip: !connectionId,
  })

  return {
    status: (data?.whatsAppConnectionStatus?.status as string) ?? "connecting",
    qrData: (data?.whatsAppConnectionStatus?.qrData as string) ?? null,
    loading,
    error: error ?? undefined,
  }
}

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

export function useBindChannelToAgent() {
  const [fn, state] = useMutation(BIND_CHANNEL)
  return {
    bindChannelToAgent: async (connectionId: string, agentId: string) => {
      const { data } = await fn({
        variables: { agentId, channelConnectionId: connectionId },
        optimisticResponse: {
          bindChannelToAgent: {
            __typename: "ChannelBinding",
            id: `bind_optimistic_${Date.now().toString(36)}`,
            agentId,
            channelConnectionId: connectionId,
          },
        },
      })
      return Boolean(data?.bindChannelToAgent)
    },
    ...state,
  }
}
