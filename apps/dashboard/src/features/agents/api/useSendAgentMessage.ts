"use client"

import { gql, useMutation, useApolloClient } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"
import { type AgentLog } from "../data/agent-mock"

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

/** Module-local sink for mock mode so the dev UX mirrors real behaviour without
 *  needing Apollo's cache. Callers that need to render the message immediately
 *  can read this, but the normal path is the log subscription. */
export const mockSentMessages: AgentLog[] = []

export function useSendAgentMessage() {
  const [fn, state] = useMutation(SEND_MESSAGE)
  const client = useApolloClient()

  return {
    sendAgentMessage: async (agentId: string, content: string) => {
      const optimisticId = `msg_optimistic_${Date.now().toString(36)}`
      const now = new Date().toISOString()

      // Immediately inject a "message_in" log entry so the user sees their
      // message in the logs tab without waiting for the server/subscription.
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

      if (USE_MOCKS) {
        const log: AgentLog = {
          id: optimisticId,
          time: Date.now(),
          type: "message_in",
          content,
        }
        mockSentMessages.push(log)
        return log
      }

      // Write the optimistic log entry to the agent logs cache
      try {
        client.cache.updateQuery(
          { query: AGENT_LOGS_QUERY, variables: { agentId, limit: 200 } },
          (old: { agentLogs?: unknown[] } | null) => ({
            agentLogs: [...((old?.agentLogs as unknown[]) ?? []), optimisticLog],
          }),
        )
      } catch {
        // Cache may not have this query yet — that's fine
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
