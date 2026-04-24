"use client";

import { gql, useQuery } from "@apollo/client";
import { USE_MOCKS } from "@/lib/graphql/mock-mode";
import { useAgent } from "./useAgents";

const CHANNEL_CONNECTIONS_QUERY = gql`
  query ChannelConnectionsForBindings {
    channelConnections {
      id
      channelType
    }
  }
`;

/**
 * Reads skill and channel bindings from the agent aggregate.
 * Only needs one extra query for channel connections (to resolve IDs → slugs).
 */
export function useAgentBindings(agentId: string): {
  skillSlugs: string[];
  channelSlugs: string[];
  loading: boolean;
} {
  const { agent, loading: agentLoading } = useAgent(agentId);
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
