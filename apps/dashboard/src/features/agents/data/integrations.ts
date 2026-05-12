export type Tool = {
  name: string;
  description: string;
};

export type IntegrationCapability = {
  type: string;
  name: string;
  description: string;
};

export type CredentialField = {
  name: string;
  label: string;
  type: string; // "password" | "text"
  required: boolean;
};

export type McpServer = {
  id: string;
  name: string; // slug
  provider: string;
  title: string;
  subtitle: string;
  description: string;
  transportType: string;
  command: string;
  args: string[];
  url: string;
  logo: string;
  category: string;
  credentialFields: CredentialField[];
  oauthProvider: string | null;
  oauthScopes: string[];
  oauthConfigured: boolean;
  configured: boolean; // derived: has credentials saved
  isBuiltin: boolean;
  authorName: string;
  authorUrl: string;
  documentationUrl: string;
  repositoryUrl: string;
  tools: Tool[];
  capabilities: IntegrationCapability[];
};

/** @deprecated Use McpServer instead */
export type Integration = McpServer;
