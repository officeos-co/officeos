"use client";

import { gql, useMutation } from "@apollo/client";

export type QuickstartFile = {
  path: string;
  content: string;
};

const QUICKSTART_AGENT_CHAT = gql`
  mutation QuickstartAgentChat($input: QuickstartAgentChatInput!) {
    quickstartAgentChat(input: $input) {
      message
      configYaml
      configJson
      provider
      model
      files {
        path
        content
      }
    }
  }
`;

const APPLY_QUICKSTART_BLUEPRINT = gql`
  mutation ApplyQuickstartBlueprint($input: QuickstartBlueprintApplyInput!) {
    applyQuickstartBlueprint(input: $input) {
      agents {
        id
        name
        filePath
      }
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
    files: QuickstartFile[];
  };
};

type QuickstartAgentChatInput = {
  message: string;
  currentYaml?: string | null;
  currentFiles: QuickstartFile[];
  messages: Array<{
    role: string;
    content: string;
  }>;
  provider?: string | null;
  model?: string | null;
};

type ApplyQuickstartBlueprintData = {
  applyQuickstartBlueprint: {
    agents: Array<{
      id: string;
      name: string;
      filePath: string;
    }>;
  };
};

type ApplyQuickstartBlueprintInput = {
  files: QuickstartFile[];
  provider?: string | null;
  model?: string | null;
};

export function useQuickstartAgentChat() {
  const [chatMutation, chatState] = useMutation<QuickstartAgentChatData>(
    QUICKSTART_AGENT_CHAT,
  );
  const [applyMutation, applyState] = useMutation<ApplyQuickstartBlueprintData>(
    APPLY_QUICKSTART_BLUEPRINT,
  );

  return {
    applyQuickstartBlueprint: async (input: ApplyQuickstartBlueprintInput) => {
      const { data } = await applyMutation({
        variables: { input },
      });
      return data?.applyQuickstartBlueprint ?? null;
    },
    quickstartAgentChat: async (input: QuickstartAgentChatInput) => {
      const { data } = await chatMutation({
        variables: { input },
      });
      return data?.quickstartAgentChat ?? null;
    },
    applying: applyState.loading,
    ...chatState,
  };
}
