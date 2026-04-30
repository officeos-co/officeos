"use client";

import { gql, useMutation } from "@apollo/client";
import { AGENT_MCP_SERVERS_QUERY } from "./useIntegrations";

const ASSIGN_MCP_SERVER = gql`
  mutation AssignMcpServerToAgent($agentId: UUID!, $serverName: String!) {
    assignMcpServerToAgent(agentId: $agentId, serverName: $serverName)
  }
`;

const UNASSIGN_MCP_SERVER = gql`
  mutation UnassignMcpServerFromAgent($agentId: UUID!, $serverName: String!) {
    unassignMcpServerFromAgent(agentId: $agentId, serverName: $serverName)
  }
`;

export function useAssignMcpServerToAgent() {
  const [fn] = useMutation(ASSIGN_MCP_SERVER);
  return async (agentId: string, serverName: string) => {
    await fn({
      variables: { agentId, serverName },
      refetchQueries: [
        { query: AGENT_MCP_SERVERS_QUERY, variables: { agentId } },
      ],
    });
  };
}

export function useUnassignMcpServerFromAgent() {
  const [fn] = useMutation(UNASSIGN_MCP_SERVER);
  return async (agentId: string, serverName: string) => {
    await fn({
      variables: { agentId, serverName },
      refetchQueries: [
        { query: AGENT_MCP_SERVERS_QUERY, variables: { agentId } },
      ],
    });
  };
}

// Backward-compatible aliases
/** @deprecated Use useAssignMcpServerToAgent instead */
export const useAssignSkillToAgent = useAssignMcpServerToAgent;
/** @deprecated Use useUnassignMcpServerFromAgent instead */
export const useUnassignSkillFromAgent = useUnassignMcpServerFromAgent;
