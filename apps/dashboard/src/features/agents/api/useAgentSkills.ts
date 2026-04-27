"use client";

import { gql, useMutation, useQuery } from "@apollo/client";
import type { ToolPermission } from "@/components/permission-cards";

const AGENT_SKILLS_QUERY = gql`
  query AgentSkills($agentId: UUID!) {
    agentSkills(agentId: $agentId) {
      skillName
      permissions {
        skillName
        toolName
        permission
      }
    }
  }
`;

const ASSIGN_SKILL = gql`
  mutation AssignSkillToAgent($agentId: UUID!, $skillName: String!) {
    assignSkillToAgent(agentId: $agentId, skillName: $skillName)
  }
`;

const UNASSIGN_SKILL = gql`
  mutation UnassignSkillFromAgent($agentId: UUID!, $skillName: String!) {
    unassignSkillFromAgent(agentId: $agentId, skillName: $skillName)
  }
`;

const SET_TOOL_PERMISSION = gql`
  mutation SetAgentToolPermission(
    $agentId: UUID!
    $skillName: String!
    $toolName: String!
    $permission: ToolPermission!
  ) {
    setAgentToolPermission(
      agentId: $agentId
      skillName: $skillName
      toolName: $toolName
      permission: $permission
    ) {
      skillName
      toolName
      permission
    }
  }
`;

export function useAssignSkillToAgent() {
  const [fn] = useMutation(ASSIGN_SKILL);
  return async (agentId: string, skillName: string) => {
    await fn({ variables: { agentId, skillName } });
  };
}

export function useUnassignSkillFromAgent() {
  const [fn] = useMutation(UNASSIGN_SKILL);
  return async (agentId: string, skillName: string) => {
    await fn({ variables: { agentId, skillName } });
  };
}

export type AgentSkillPermissions = {
  toolPermissions: Record<string, ToolPermission>;
  groupPermissions: Record<string, ToolPermission>;
};

export function useAgentToolPermissions(agentId: string): {
  data: AgentSkillPermissions;
  loading: boolean;
} {
  const { data, loading } = useQuery(AGENT_SKILLS_QUERY, {
    variables: { agentId },
    skip: !agentId,
    fetchPolicy: "cache-and-network",
  });

  const toolPerms: Record<string, ToolPermission> = {};
  const groupPerms: Record<string, ToolPermission> = {};

  const skills: Array<{
    skillName: string;
    permissions: Array<{
      skillName: string;
      toolName: string;
      permission: "ALLOW" | "DENY";
    }>;
  }> = data?.agentSkills ?? [];

  for (const skill of skills) {
    for (const p of skill.permissions) {
      const mode: ToolPermission = p.permission === "ALLOW" ? "allow" : "deny";
      if (p.toolName === "") {
        groupPerms[p.skillName] = mode;
      } else {
        toolPerms[`${p.skillName}:${p.toolName}`] = mode;
      }
    }
  }

  return { data: { toolPermissions: toolPerms, groupPermissions: groupPerms }, loading };
}

export function useSetAgentToolPermission() {
  const [fn] = useMutation(SET_TOOL_PERMISSION);
  return async (
    agentId: string,
    skillName: string,
    toolName: string,
    permission: "ALLOW" | "DENY",
  ) => {
    await fn({ variables: { agentId, skillName, toolName, permission } });
  };
}
