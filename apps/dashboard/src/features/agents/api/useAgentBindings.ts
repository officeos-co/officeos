"use client";

import { gql, useQuery } from "@apollo/client";
import { USE_MOCKS } from "@/lib/graphql/mock-mode";

const AGENT_SKILLS_QUERY = gql`
  query AgentSkills($agentId: UUID!) {
    agentSkills(agentId: $agentId) {
      skillName
    }
  }
`;

const AGENT_CHANNEL_BINDINGS_QUERY = gql`
  query AgentChannelBindings($agentId: UUID!) {
    agentChannelBindings(agentId: $agentId) {
      id
      channelConnectionId
    }
  }
`;

const CHANNEL_CONNECTIONS_QUERY = gql`
  query ChannelConnectionsForBindings {
    channelConnections {
      id
      channelType
    }
  }
`;

/**
 * Returns the skill slugs and channel slugs bound to this agent.
 * Resolves channelConnectionId → channelType slug via the connections list.
 */
export function useAgentBindings(agentId: string): {
  skillSlugs: string[];
  channelSlugs: string[];
  loading: boolean;
} {
  const { data: skillsData, loading: skillsLoading } = useQuery(
    AGENT_SKILLS_QUERY,
    { variables: { agentId }, skip: USE_MOCKS || !agentId },
  );
  const { data: bindingsData, loading: bindingsLoading } = useQuery(
    AGENT_CHANNEL_BINDINGS_QUERY,
    { variables: { agentId }, skip: USE_MOCKS || !agentId },
  );
  const { data: connectionsData, loading: connectionsLoading } = useQuery(
    CHANNEL_CONNECTIONS_QUERY,
    { skip: USE_MOCKS },
  );

  if (USE_MOCKS) {
    return {
      skillSlugs: ["github", "linear"],
      channelSlugs: ["slack"],
      loading: false,
    };
  }

  const skillSlugs: string[] = (skillsData?.agentSkills ?? []).map(
    (s: { skillName: string }) => s.skillName,
  );

  // Resolve connection IDs to channel type slugs
  const connections: Array<{ id: string; channelType: string }> =
    connectionsData?.channelConnections ?? [];
  const connMap = new Map(connections.map((c) => [c.id, c.channelType]));
  const bindings: Array<{ channelConnectionId: string }> =
    bindingsData?.agentChannelBindings ?? [];
  const channelSlugs = [
    ...new Set(
      bindings
        .map((b) => connMap.get(b.channelConnectionId))
        .filter((s): s is string => !!s),
    ),
  ];

  return {
    skillSlugs,
    channelSlugs,
    loading: skillsLoading || bindingsLoading || connectionsLoading,
  };
}
