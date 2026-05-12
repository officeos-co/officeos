"use client";

import { gql, useMutation } from "@apollo/client";

const QUICKSTART_AGENT_CHAT = gql`
  mutation QuickstartAgentChat($input: QuickstartAgentChatInput!) {
    quickstartAgentChat(input: $input) {
      message
      configYaml
      configJson
      provider
      model
    }
  }
`;

type QuickstartAgentChatData = {
  quickstartAgentChat: {
    message: string;
    configYaml: string;
    configJson: string;
    provider: string;
    model: string;
  };
};

type QuickstartAgentChatInput = {
  message: string;
  currentYaml: string;
  messages: Array<{
    role: string;
    content: string;
  }>;
  provider?: string | null;
  model?: string | null;
};

export function useQuickstartAgentChat() {
  const [mutate, state] = useMutation<QuickstartAgentChatData>(
    QUICKSTART_AGENT_CHAT,
  );

  return {
    quickstartAgentChat: async (input: QuickstartAgentChatInput) => {
      const { data } = await mutate({
        variables: { input },
      });
      return data?.quickstartAgentChat ?? null;
    },
    ...state,
  };
}
