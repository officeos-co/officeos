"use client"

import { gql, useMutation } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"
import { type AgentLog } from "@/data/agent-mock"

const SEND_MESSAGE = gql`
  mutation SendAgentMessage($agentId: String!, $content: String!) {
    sendAgentMessage(agentId: $agentId, content: $content) {
      id
      content
    }
  }
`

/** Module-local sink for mock mode so the dev UX mirrors real behaviour without
 *  needing Apollo's cache. Callers that need to render the message immediately
 *  can read this, but the normal path is the log subscription. */
export const mockSentMessages: AgentLog[] = []

export function useSendAgentMessage() {
  const [fn, state] = useMutation(SEND_MESSAGE)
  return {
    sendAgentMessage: async (agentId: string, content: string) => {
      if (USE_MOCKS) {
        const log: AgentLog = {
          id: `msg_mock_${Date.now().toString(36)}`,
          time: Date.now(),
          type: "message_in",
          content,
        }
        mockSentMessages.push(log)
        return log
      }
      const { data } = await fn({ variables: { agentId, content } })
      return data?.sendAgentMessage as { id: string; content: string }
    },
    ...state,
  }
}
