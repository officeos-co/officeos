"use client";

import { gql, useQuery } from "@apollo/client";
import { useAgent } from "./useAgents";
import { AGENT_MCP_SERVERS_QUERY } from "./useIntegrations";

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
  const { data: mcpData, loading: mcpLoading } = useQuery(
    AGENT_MCP_SERVERS_QUERY,
    { variables: { agentId }, skip: !agentId },
  );

  const { agent, loading: agentLoading } = useAgent(agentId);

  const { data: connectionsData, loading: connectionsLoading } = useQuery(
    CHANNEL_CONNECTIONS_QUERY,
  );

  const skillSlugs = (mcpData?.agentMcpServers ?? []).map(
    (s: { name: string }) => s.name,
  );

  const connections: Array<{ id: string; channelType: string }> =
    connectionsData?.channelConnections ?? [];
  const connMap = new Map(connections.map((c) => [c.id, c.channelType]));
  const channelSlugs = [
    ...new Set(
      (agent?.channelBindings ?? [])
        .map((b: { channelConnectionId: string }) =>
          connMap.get(b.channelConnectionId),
        )
        .filter((s: string | undefined): s is string => !!s),
    ),
  ];

  return {
    skillSlugs,
    channelSlugs,
    loading: mcpLoading || agentLoading || connectionsLoading,
  };
}
