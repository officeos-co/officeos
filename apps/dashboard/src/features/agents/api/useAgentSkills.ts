"use client";

import { gql, useMutation } from "@apollo/client";
import { AGENT_MCP_SERVERS_QUERY } from "./useIntegrations";

const ASSIGN_MCP_SERVER = gql`
  mutation AssignIntegrationToAgent($agentId: UUID!, $integrationName: String!) {
    assignIntegrationToAgent(agentId: $agentId, integrationName: $integrationName)
  }
`;

const UNASSIGN_MCP_SERVER = gql`
  mutation UnassignIntegrationFromAgent($agentId: UUID!, $integrationName: String!) {
    unassignIntegrationFromAgent(agentId: $agentId, integrationName: $integrationName)
  }
`;

export function useAssignMcpServerToAgent() {
  const [fn] = useMutation(ASSIGN_MCP_SERVER);
  return async (agentId: string, integrationName: string) => {
    await fn({
      variables: { agentId, integrationName },
      refetchQueries: [
        { query: AGENT_MCP_SERVERS_QUERY, variables: { agentId } },
      ],
    });
  };
}

export function useUnassignMcpServerFromAgent() {
  const [fn] = useMutation(UNASSIGN_MCP_SERVER);
  return async (agentId: string, integrationName: string) => {
    await fn({
      variables: { agentId, integrationName },
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
