import { gql, useMutation, useQuery } from "@apollo/client";
import { USE_MOCKS } from "@/lib/graphql/mock-mode";

export interface AgentSession {
  id: string;
  agentId: string;
  status: "active" | "ended";
  messageCount: number;
  lastActivityAt: string;
  createdAt: string;
  endedAt: string | null;
}

const ACTIVE_SESSION_QUERY = gql`
  query ActiveSession($agentId: UUID!) {
    activeSession(agentId: $agentId) {
      id
      agentId
      status
      messageCount
      lastActivityAt
      createdAt
      endedAt
    }
  }
`;

const AGENT_SESSIONS_QUERY = gql`
  query AgentSessions($agentId: UUID!, $limit: Int) {
    agentSessions(agentId: $agentId, limit: $limit) {
      id
      agentId
      status
      messageCount
      lastActivityAt
      createdAt
      endedAt
    }
  }
`;

const CREATE_SESSION_MUTATION = gql`
  mutation CreateSession($agentId: UUID!) {
    createSession(agentId: $agentId) {
      id
      agentId
      status
      messageCount
      lastActivityAt
      createdAt
      endedAt
    }
  }
`;

const END_SESSION_MUTATION = gql`
  mutation EndSession($agentId: UUID!) {
    endSession(agentId: $agentId) {
      id
      status
      endedAt
    }
  }
`;

export function useActiveSession(agentId: string) {
  if (USE_MOCKS) {
    return { session: null as AgentSession | null, loading: false };
  }
  const { data, loading } = useQuery(ACTIVE_SESSION_QUERY, {
    variables: { agentId },
    skip: !agentId,
  });
  return { session: data?.activeSession as AgentSession | null, loading };
}

export function useAgentSessions(agentId: string, limit = 20) {
  if (USE_MOCKS) {
    return { sessions: [] as AgentSession[], loading: false };
  }
  const { data, loading } = useQuery(AGENT_SESSIONS_QUERY, {
    variables: { agentId, limit },
    skip: !agentId,
  });
  return {
    sessions: (data?.agentSessions ?? []) as AgentSession[],
    loading,
  };
}

export function useCreateSession() {
  const [mutate] = useMutation(CREATE_SESSION_MUTATION);

  return async (agentId: string) => {
    const { data } = await mutate({
      variables: { agentId },
      refetchQueries: ["ActiveSession", "AgentSessions"],
    });
    return data?.createSession as AgentSession;
  };
}

export function useEndSession() {
  const [mutate] = useMutation(END_SESSION_MUTATION);

  return async (agentId: string) => {
    const { data } = await mutate({
      variables: { agentId },
      refetchQueries: ["ActiveSession", "AgentSessions"],
    });
    return data?.endSession as AgentSession | null;
  };
}
