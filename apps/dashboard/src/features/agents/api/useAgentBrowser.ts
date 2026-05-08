"use client";

import { gql, useMutation, useQuery } from "@apollo/client";

const BROWSER_QUERY = gql`
  query AgentBrowser($agentId: UUID!) {
    agentBrowser(agentId: $agentId) {
      runtimeSessionId
      status
      name
      currentUrl
      title
      takeoverUrl
      createdAt
      lastAccessedAt
    }
  }
`;

const START_BROWSER = gql`
  mutation StartAgentBrowser($agentId: UUID!) {
    startAgentBrowser(agentId: $agentId) {
      runtimeSessionId
      status
      takeoverUrl
    }
  }
`;

const RESTART_BROWSER = gql`
  mutation RestartAgentBrowser($agentId: UUID!) {
    restartAgentBrowser(agentId: $agentId) {
      runtimeSessionId
      status
      takeoverUrl
    }
  }
`;

const STOP_BROWSER = gql`
  mutation StopAgentBrowser($agentId: UUID!) {
    stopAgentBrowser(agentId: $agentId)
  }
`;

export type AgentBrowserState = {
  runtimeSessionId: string | null;
  status: string;
  name: string | null;
  currentUrl: string | null;
  title: string | null;
  takeoverUrl: string | null;
  createdAt: string | null;
  lastAccessedAt: string | null;
};

export function useAgentBrowser(agentId: string) {
  const state = useQuery(BROWSER_QUERY, {
    variables: { agentId },
    skip: !agentId,
    pollInterval: 5000,
  });
  const [startMutation, startState] = useMutation(START_BROWSER);
  const [restartMutation, restartState] = useMutation(RESTART_BROWSER);
  const [stopMutation, stopState] = useMutation(STOP_BROWSER);

  async function refresh() {
    await state.refetch();
  }

  const browser = (state.data?.agentBrowser ?? null) as AgentBrowserState | null;

  return {
    browser,
    viewUrl: browser?.takeoverUrl ?? null,
    loading: state.loading,
    error: state.error,
    busy: startState.loading || restartState.loading || stopState.loading,
    start: async () => {
      await startMutation({ variables: { agentId } });
      await refresh();
    },
    restart: async () => {
      await restartMutation({ variables: { agentId } });
      await refresh();
    },
    stop: async () => {
      await stopMutation({ variables: { agentId } });
      await refresh();
    },
    refresh,
  };
}
