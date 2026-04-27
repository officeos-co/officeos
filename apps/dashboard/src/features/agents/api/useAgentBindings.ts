"use client";

import { gql, useQuery } from "@apollo/client";
import { useAgent } from "./useAgents";

const CHANNEL_CONNECTIONS_QUERY = gql`
  query ChannelConnectionsForBindings {
    channelConnections {
      id
      channelType
    }
  }
`;

export function useAgentBindings(agentId: string): {
  skillSlugs: string[];
  channelSlugs: string[];
  loading: boolean;
} {
  const { agent, loading: agentLoading } = useAgent(agentId);
  const { data: connectionsData, loading: connectionsLoading } = useQuery(
    CHANNEL_CONNECTIONS_QUERY,
  );

  const skillSlugs = (agent?.installedSkills ?? []).map((s) => s.skillName);

  const connections: Array<{ id: string; channelType: string }> =
    connectionsData?.channelConnections ?? [];
  const connMap = new Map(connections.map((c) => [c.id, c.channelType]));
  const channelSlugs = [
    ...new Set(
      (agent?.channelBindings ?? [])
        .map((b) => connMap.get(b.channelConnectionId))
        .filter((s): s is string => !!s),
    ),
  ];

  return {
    skillSlugs,
    channelSlugs,
    loading: agentLoading || connectionsLoading,
  };
}
