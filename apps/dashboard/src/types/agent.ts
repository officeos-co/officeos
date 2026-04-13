export type Agent = {
  id: string;
  name: string;
  provider: string;
  model: string | null;
  status: string;
  podName: string | null;
  serviceUrl: string | null;
  createdAt: string;
};

export type CreateAgentInput = {
  name: string;
  provider: string;
  model?: string;
};

export type AgentSkillAssignment = {
  id: string;
  agentId: string;
  skillName: string;
  enabledAt: string;
};
