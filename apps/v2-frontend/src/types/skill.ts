export type CredentialField = {
  key: string;
  label: string;
  kind: "password" | "text" | "textarea";
  required: boolean;
  placeholder: string | null;
  help: string | null;
};

export type LlmTool = {
  name: string;
  description: string;
  parameters: Record<string, unknown>;
};

export type Skill = {
  name: string;
  title: string;
  description: string;
  emoji: string;
  installed: boolean;
  configured: boolean;
  runTarget: "cloud" | "runner";
  credentialFields: CredentialField[];
  llmTools: LlmTool[];
  // Registry metadata (populated from skill registry join)
  registryVersion?: string;
  registryStatus?: "active" | "disabled";
};

export type CustomSkill = {
  id: string;
  name: string;
  source: "upload" | "github";
  gitHubRepoUrl: string | null;
  buildStatus: string;
  buildError: string | null;
  createdAt: string;
  lastSyncAt: string | null;
};

export type RegistrySkill = {
  id: string;
  name: string;
  version: string;
  npmPackage: string | null;
  bundleUrl: string | null;
  status: "active" | "disabled";
  publishedById: string | null;
  createdAt: string;
  updatedAt: string;
};
