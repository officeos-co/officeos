"use client";

import { gql, useMutation, useQuery } from "@apollo/client";
import type {
  McpServer,
  CredentialField,
  IntegrationCapability,
  Tool,
} from "../data/integrations";
import { sanitizeSvg } from "@/lib/sanitize-svg";

const MCP_SERVERS_QUERY = gql`
  query Integrations {
    integrations {
      id
      name
      provider
      title
      description
      transportType
      command
      args
      url
      logo
      category
      credentialFieldsJson
      oauthProvider
      oauthScopesJson
      oauthConfigured
      credentialConfigured
      subtitle
      authorName
      authorUrl
      documentationUrl
      repositoryUrl
      tools {
        name
        description
      }
      capabilitiesJson
      isBuiltin
      createdAt
    }
  }
`;

const INTEGRATION_CATALOG_QUERY = gql`
  query IntegrationCatalog {
    integrationCatalog {
      id
      name
      provider
      title
      description
      transportType
      command
      args
      url
      logo
      category
      credentialFieldsJson
      oauthProvider
      oauthScopesJson
      oauthConfigured
      credentialConfigured
      subtitle
      authorName
      authorUrl
      documentationUrl
      repositoryUrl
      tools {
        name
        description
      }
      capabilitiesJson
      isBuiltin
      createdAt
    }
  }
`;

const MCP_SERVER_QUERY = gql`
  query Integration($name: String!) {
    integration(name: $name) {
      id
      name
      provider
      title
      description
      transportType
      command
      args
      url
      logo
      category
      credentialFieldsJson
      oauthProvider
      oauthScopesJson
      oauthConfigured
      credentialConfigured
      subtitle
      authorName
      authorUrl
      documentationUrl
      repositoryUrl
      tools {
        name
        description
      }
      capabilitiesJson
      isBuiltin
      createdAt
    }
  }
`;

export const AGENT_MCP_SERVERS_QUERY = gql`
  query AgentIntegrations($agentId: UUID!) {
    agentIntegrations(agentId: $agentId) {
      id
      name
      provider
      title
      description
      transportType
      logo
      category
      credentialFieldsJson
      oauthProvider
      oauthScopesJson
      oauthConfigured
      credentialConfigured
      capabilitiesJson
      isBuiltin
    }
  }
`;

const REGISTER_MCP_SERVER = gql`
  mutation RegisterIntegration($input: RegisterIntegrationInput!) {
    registerIntegration(input: $input) {
      id
      name
      title
      description
      transportType
      command
      args
      url
      logo
      category
      credentialFieldsJson
      oauthProvider
      oauthScopesJson
      oauthConfigured
      credentialConfigured
      subtitle
      authorName
      authorUrl
      documentationUrl
      repositoryUrl
      tools {
        name
        description
      }
      isBuiltin
      createdAt
    }
  }
`;

const DELETE_MCP_SERVER = gql`
  mutation DeleteIntegration($name: String!) {
    deleteIntegration(name: $name)
  }
`;

const SAVE_MCP_CREDENTIAL = gql`
  mutation SaveIntegrationCredential($integrationName: String!, $fields: [CredentialFieldInput!]!) {
    saveIntegrationCredential(integrationName: $integrationName, fields: $fields)
  }
`;

const DISCONNECT_INTEGRATION = gql`
  mutation DisconnectIntegration($integrationName: String!) {
    disconnectIntegration(integrationName: $integrationName)
  }
`;

type RawMcpServer = {
  id: string;
  name: string;
  provider: string | null;
  title: string | null;
  description: string | null;
  transportType: string | null;
  command: string | null;
  args: string | null;
  url: string | null;
  logo: string | null;
  category: string | null;
  credentialFieldsJson: string | null;
  oauthProvider: string | null;
  oauthScopesJson: string | null;
  oauthConfigured: boolean | null;
  credentialConfigured: boolean | null;
  subtitle: string | null;
  authorName: string | null;
  authorUrl: string | null;
  documentationUrl: string | null;
  repositoryUrl: string | null;
  tools: Tool[] | null;
  capabilitiesJson: string | null;
  isBuiltin: boolean;
  createdAt: string | null;
};

function parseCredentialFields(json: string | null): CredentialField[] {
  if (!json) return [];
  try {
    const parsed = JSON.parse(json);
    if (!Array.isArray(parsed)) return [];
    return parsed.map((f: Record<string, unknown>) => ({
      name: String(f.name ?? ""),
      label: String(f.label ?? f.name ?? ""),
      type: String(f.type ?? "text"),
      required: Boolean(f.required),
    }));
  } catch {
    return [];
  }
}

function parseTools(tools: Tool[] | null): Tool[] {
  if (!Array.isArray(tools)) return [];
  return tools.map((tool) => ({
    name: String(tool.name ?? ""),
    description: String(tool.description ?? ""),
  }));
}

function parseCapabilities(json: string | null): IntegrationCapability[] {
  if (!json) return [];
  try {
    const parsed = JSON.parse(json);
    if (!Array.isArray(parsed)) return [];
    return parsed.map((capability: Record<string, unknown>) => ({
      type: String(capability.type ?? ""),
      name: String(capability.name ?? ""),
      description: String(capability.description ?? ""),
    }));
  } catch {
    return [];
  }
}

function parseOAuthScopes(json: string | null): string[] {
  if (!json) return [];
  try {
    const parsed = JSON.parse(json);
    return Array.isArray(parsed) ? parsed.map(String) : [];
  } catch {
    return [];
  }
}

function parseArgs(json: string | null): string[] {
  if (!json) return [];
  try {
    const parsed = JSON.parse(json);
    return Array.isArray(parsed) ? parsed.map(String) : [];
  } catch {
    return [];
  }
}

function mapIntegration(s: RawMcpServer): McpServer {
  const credentialFields = parseCredentialFields(s.credentialFieldsJson);
  const oauthProvider = s.oauthProvider ?? null;
  const oauthConfigured = Boolean(s.oauthConfigured);
  return {
    id: s.id,
    name: s.name,
    provider: s.provider ?? s.name,
    title: s.title ?? s.name,
    subtitle: s.subtitle ?? "",
    description: s.description ?? "",
    transportType: s.transportType ?? "stdio",
    command: s.command ?? "",
    args: parseArgs(s.args),
    url: s.url ?? "",
    logo: sanitizeSvg(s.logo ?? ""),
    category: s.category ?? "",
    credentialFields,
    oauthProvider,
    oauthScopes: parseOAuthScopes(s.oauthScopesJson),
    oauthConfigured,
    configured: oauthProvider
      ? oauthConfigured
      : credentialFields.length === 0 || Boolean(s.credentialConfigured),
    isBuiltin: s.isBuiltin,
    authorName: s.authorName ?? "",
    authorUrl: s.authorUrl ?? "",
    documentationUrl: s.documentationUrl ?? "",
    repositoryUrl: s.repositoryUrl ?? "",
    tools: parseTools(s.tools),
    capabilities: parseCapabilities(s.capabilitiesJson),
  };
}

export function useIntegrations(): {
  integrations: McpServer[];
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery(MCP_SERVERS_QUERY, {
    fetchPolicy: "cache-and-network",
  });
  const raw: RawMcpServer[] = data?.integrations ?? [];
  const integrations = raw.map(mapIntegration);
  return { integrations, loading, error: error ?? undefined };
}

export function useIntegrationCatalog(): {
  integrations: McpServer[];
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery(INTEGRATION_CATALOG_QUERY, {
    fetchPolicy: "cache-and-network",
  });
  const raw: RawMcpServer[] = data?.integrationCatalog ?? [];
  const integrations = raw.map(mapIntegration);
  return { integrations, loading, error: error ?? undefined };
}

export function useIntegration(name: string): {
  integration: McpServer | null;
  loading: boolean;
  error?: Error;
  refetch: () => void;
} {
  const { data, loading, error, refetch } = useQuery(MCP_SERVER_QUERY, {
    variables: { name },
    skip: !name,
  });
  const raw: RawMcpServer | null = data?.integration ?? null;
  const integration = raw ? mapIntegration(raw) : null;
  return { integration, loading, error: error ?? undefined, refetch };
}

export function useSaveIntegrationCredential() {
  const [fn] = useMutation(SAVE_MCP_CREDENTIAL);
  return async (integrationName: string, credentials: Record<string, string>) => {
    const fields = Object.entries(credentials).map(([key, value]) => ({
      key,
      value,
    }));
    await fn({
      variables: { integrationName, fields },
      refetchQueries: [{ query: MCP_SERVERS_QUERY }, { query: INTEGRATION_CATALOG_QUERY }],
      awaitRefetchQueries: true,
    });
  };
}

export function useDisconnectIntegration() {
  const [fn] = useMutation(DISCONNECT_INTEGRATION);
  return async (integrationName: string) => {
    await fn({
      variables: { integrationName },
      refetchQueries: [{ query: MCP_SERVERS_QUERY }, { query: INTEGRATION_CATALOG_QUERY }],
      awaitRefetchQueries: true,
    });
  };
}

export type RegisterIntegrationInput = {
  name: string;
  title: string;
  description: string;
  subtitle: string;
  authorName: string;
  authorUrl: string;
  documentationUrl: string;
  repositoryUrl: string;
  tools?: Tool[] | null;
  transportType: string;
  command?: string | null;
  args?: string | null;
  url?: string | null;
  category: string;
  credentialFieldsJson?: string | null;
  logo?: string | null;
};

export function useRegisterIntegration() {
  const [fn] = useMutation(REGISTER_MCP_SERVER);
  return async (input: RegisterIntegrationInput) => {
    await fn({
      variables: { input },
      refetchQueries: [{ query: MCP_SERVERS_QUERY }, { query: INTEGRATION_CATALOG_QUERY }],
      awaitRefetchQueries: true,
    });
  };
}

export function useDeleteIntegration() {
  const [fn] = useMutation(DELETE_MCP_SERVER);
  return async (name: string) => {
    await fn({
      variables: { name },
      refetchQueries: [{ query: MCP_SERVERS_QUERY }, { query: INTEGRATION_CATALOG_QUERY }],
      awaitRefetchQueries: true,
    });
  };
}

/** Sort integrations: configured first, then alphabetical by title. */
export function sortIntegrations(list: McpServer[]): McpServer[] {
  return [...list].sort((a, b) => {
    const scoreA = a.configured ? 1 : 0;
    const scoreB = b.configured ? 1 : 0;
    if (scoreB !== scoreA) return scoreB - scoreA;
    return a.title.localeCompare(b.title);
  });
}

/** @deprecated Use useSaveIntegrationCredential instead */
export function useSetSkillCredentials() {
  const save = useSaveIntegrationCredential();
  return save;
}

/** @deprecated Use useIntegrations instead */
export function useMcpServers() {
  const { integrations, loading, error } = useIntegrations();
  return { servers: integrations, loading, error };
}

/** @deprecated Use useIntegration instead */
export function useMcpServer(slug: string) {
  const { integration, loading, error, refetch } = useIntegration(slug);
  return { server: integration, loading, error, refetch };
}

/** @deprecated Use useRegisterIntegration instead */
export const useRegisterMcpServer = useRegisterIntegration;

/** @deprecated Use useDeleteIntegration instead */
export const useDeleteMcpServer = useDeleteIntegration;
