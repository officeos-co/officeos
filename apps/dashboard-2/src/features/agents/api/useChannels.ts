"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"
import { channels as mockChannels, type Channel } from "../data/channels"

const CHANNELS_QUERY = gql`
  query ChannelsAndTypes {
    channelTypes {
      type
      displayName
      description
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
  const { data, loading, error } = useQuery(CHANNELS_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { channels: mockChannels, loading: false }

  const types: Array<{
    type: string
    displayName: string
    description: string | null
  }> = data?.channelTypes ?? []
  const connections: Array<{ channelType: string }> = data?.channelConnections ?? []
  const connectedSlugs = new Set(connections.map((c) => c.channelType))

  // Start from the mock catalogue — it carries UI-only metadata (capabilities,
  // onboarding, defaultPermissions, skillMd) the backend does not store.
  // Override `added` from live connection state and, if the backend has a
  // matching type, override `name`/`description` with live values.
  const channels: Channel[] = mockChannels.map((m) => {
    const live = types.find((t) => t.type === m.slug)
    return {
      ...m,
      name: live?.displayName ?? m.name,
      description: live?.description ?? m.description,
      added: connectedSlugs.has(m.slug),
    }
  })

  // Surface any channel types the backend knows about but that aren't in the
  // mock catalogue yet — these get stub UI-only metadata defaulted to empty.
  for (const t of types) {
    if (!mockChannels.find((m) => m.slug === t.type)) {
      channels.push({
        name: t.displayName,
        slug: t.type,
        logo: "",
        description: t.description ?? "",
        protocol: "",
        likes: 0,
        updatedAgo: "",
        capabilities: [],
        defaultPermissions: { receive: "ask", send: "ask", initiate: "ask" },
        added: connectedSlugs.has(t.type),
        onboarding: [],
        skillMd: "",
      })
    }
  }

  return { channels, loading, error: error ?? undefined }
}

export function useCreateChannelConnection() {
  const [fn, state] = useMutation(CREATE_CONNECTION)
  return {
    createChannelConnection: async (input: {
      channelType: string
      credentials: Record<string, string>
    }) => {
      if (USE_MOCKS) return { id: `conn_mock_${Date.now().toString(36)}`, channelType: input.channelType }
      const { data } = await fn({ variables: { input } })
      return data?.createChannelConnection as { id: string; channelType: string }
    },
    ...state,
  }
}

export function useDeleteChannelConnection() {
  const [fn, state] = useMutation(DELETE_CONNECTION)
  return {
    deleteChannelConnection: async (id: string) => {
      if (USE_MOCKS) return true
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
      if (USE_MOCKS) return true
      const { data } = await fn({ variables: { agentId, channelConnectionId: connectionId } })
      return Boolean(data?.bindChannelToAgent)
    },
    ...state,
  }
}
