"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import type { Channel } from "../data/channels"

const CHANNELS_QUERY = gql`
  query ChannelsAndTypes {
    channelTypes {
      type
      displayName
      description
      logo
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
  }> = data?.channelTypes ?? []
  const connections: Array<{ channelType: string }> = data?.channelConnections ?? []
  const connectedSlugs = new Set(connections.map((c) => c.channelType))

  const channels: Channel[] = types.map((t) => ({
    name: t.displayName,
    slug: t.type,
    logo: t.logo ?? "",
    description: t.description ?? "",
    protocol: "",
    capabilities: [],
    defaultPermissions: { receive: "ask" as const, send: "ask" as const, initiate: "ask" as const },
    added: connectedSlugs.has(t.type),
    onboarding: [],
  }))

  return { channels, loading, error: error ?? undefined }
}

export function useCreateChannelConnection() {
  const [fn, state] = useMutation(CREATE_CONNECTION, { refetchQueries: ["ChannelsAndTypes"] })
  return {
    createChannelConnection: async (input: {
      channelType: string
      credentials: Record<string, string>
    }) => {
      const { data } = await fn({ variables: { input } })
      return data?.createChannelConnection as { id: string; channelType: string }
    },
    ...state,
  }
}

export function useDeleteChannelConnection() {
  const [fn, state] = useMutation(DELETE_CONNECTION, { refetchQueries: ["ChannelsAndTypes"] })
  return {
    deleteChannelConnection: async (id: string) => {
      const { data } = await fn({ variables: { id } })
      return Boolean(data?.deleteChannelConnection)
    },
    ...state,
  }
}

export function useBindChannelToAgent() {
  const [fn, state] = useMutation(BIND_CHANNEL)
  return {
    bindChannelToAgent: async (connectionId: string, agentId: string) => {
      const { data } = await fn({ variables: { agentId, channelConnectionId: connectionId } })
      return Boolean(data?.bindChannelToAgent)
    },
    ...state,
  }
}
