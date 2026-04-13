export type ChannelConfigField = {
  key: string;
  label: string;
  kind: "password" | "text" | "textarea";
  required: boolean;
  placeholder: string | null;
  help: string | null;
};

export type ChannelType = {
  type: string;
  displayName: string;
  description: string;
  configFields: ChannelConfigField[];
};

export type ChannelConnection = {
  id: string;
  channelType: string;
  displayName: string;
  enabled: boolean;
  configured: boolean;
  createdAt: string;
  createdById: string | null;
};

export type AgentChannelConfig = {
  dmPolicy: string;
  groupPolicy: string;
  allowedUsers?: string[];
  allowedGroups?: string[];
  requireMention: boolean;
  mentionPatterns?: string[];
  historyLimit: number;
  streamingMode: string;
};

export type AgentChannelBinding = {
  id: string;
  agentId: string;
  channelConnectionId: string;
  channelType: string;
  channelDisplayName: string;
  enabled: boolean;
  config: AgentChannelConfig | null;
  createdAt: string;
};
