"use client"

import { gql, useMutation, useApolloClient } from "@apollo/client"

const SEND_MESSAGE = gql`
  mutation SendAgentMessage($agentId: UUID!, $content: String!) {
    sendAgentMessage(agentId: $agentId, content: $content) {
      id
      content
    }
  }
`

const AGENT_LOGS_QUERY = gql`
  query AgentLogs($agentId: UUID!, $limit: Int!) {
    agentLogs(agentId: $agentId, limit: $limit) {
      id
      time
      type
      tool
      integration
      channel
      content
      durationMs
      inputTokens
      outputTokens
    }
  }
`

export function useSendAgentMessage() {
  const [fn, state] = useMutation(SEND_MESSAGE)
  const client = useApolloClient()

  return {
    sendAgentMessage: async (agentId: string, content: string) => {
      const optimisticId = `msg_optimistic_${Date.now().toString(36)}`
      const now = new Date().toISOString()

      const optimisticLog = {
        __typename: "AgentLog",
        id: optimisticId,
        time: now,
        type: "MessageIn",
        tool: null,
        integration: null,
        channel: null,
        content,
        durationMs: null,
        inputTokens: null,
        outputTokens: null,
      }

      try {
        client.cache.updateQuery(
          { query: AGENT_LOGS_QUERY, variables: { agentId, limit: 200 } },
          (old: { agentLogs?: unknown[] } | null) => ({
            agentLogs: [...((old?.agentLogs as unknown[]) ?? []), optimisticLog],
          }),
        )
      } catch {
        // Cache may not have this query yet
      }

      const { data } = await fn({
        variables: { agentId, content },
        optimisticResponse: {
          sendAgentMessage: {
            __typename: "AgentMessage",
            id: optimisticId,
            content,
          },
        },
      })
      return data?.sendAgentMessage as { id: string; content: string }
    },
    ...state,
  }
}
