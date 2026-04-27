import { gql, useMutation, useQuery } from "@apollo/client";

export interface AgentSession {
  id: string;
  agentId: string;
  status: "active" | "ended";
  messageCount: number;
  lastActivityAt: string;
  createdAt: string;
  endedAt: string | null;
}

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

export function useAgentSessions(agentId: string, limit = 20) {
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
      refetchQueries: ["AgentSessions", "Agent"],
    });
    return data?.createSession as AgentSession;
  };
}

export function useEndSession() {
  const [mutate] = useMutation(END_SESSION_MUTATION);

  return async (agentId: string) => {
    const { data } = await mutate({
      variables: { agentId },
      refetchQueries: ["AgentSessions", "Agent"],
    });
    return data?.endSession as AgentSession | null;
  };
}
