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

const VIEW_URL_QUERY = gql`
  query AgentBrowserViewUrl($agentId: UUID!) {
    agentBrowserViewUrl(agentId: $agentId)
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
  const view = useQuery(VIEW_URL_QUERY, {
    variables: { agentId },
    skip: !agentId,
    fetchPolicy: "network-only",
  });
  const [startMutation, startState] = useMutation(START_BROWSER);
  const [restartMutation, restartState] = useMutation(RESTART_BROWSER);
  const [stopMutation, stopState] = useMutation(STOP_BROWSER);

  async function refresh() {
    await Promise.all([state.refetch(), view.refetch()]);
  }

  return {
    browser: (state.data?.agentBrowser ?? null) as AgentBrowserState | null,
    viewUrl: (view.data?.agentBrowserViewUrl ?? null) as string | null,
    loading: state.loading || view.loading,
    error: state.error ?? view.error,
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
