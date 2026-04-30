"use client";

import { gql, useMutation, useQuery } from "@apollo/client";
import type { McpServer, CredentialField } from "../data/integrations";
import { sanitizeSvg } from "@/lib/sanitize-svg";

const MCP_SERVERS_QUERY = gql`
  query McpServers {
    mcpServers {
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
      isBuiltin
      createdAt
    }
  }
`;

const MCP_SERVER_QUERY = gql`
  query McpServer($name: String!) {
    mcpServer(name: $name) {
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
      isBuiltin
      createdAt
    }
  }
`;

export const AGENT_MCP_SERVERS_QUERY = gql`
  query AgentMcpServers($agentId: UUID!) {
    agentMcpServers(agentId: $agentId) {
      id
      name
      title
      description
      transportType
      logo
      category
      credentialFieldsJson
      isBuiltin
    }
  }
`;

const SAVE_MCP_CREDENTIAL = gql`
  mutation SaveMcpCredential($serverName: String!, $fields: [CredentialFieldInput!]!) {
    saveMcpCredential(serverName: $serverName, fields: $fields)
  }
`;

type RawMcpServer = {
  id: string;
  name: string;
  title: string | null;
  description: string | null;
  transportType: string | null;
  command: string | null;
  args: string[] | null;
  url: string | null;
  logo: string | null;
  category: string | null;
  credentialFieldsJson: string | null;
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

function mapMcpServer(s: RawMcpServer): McpServer {
  const credentialFields = parseCredentialFields(s.credentialFieldsJson);
  return {
    id: s.id,
    name: s.name,
    title: s.title ?? s.name,
    description: s.description ?? "",
    transportType: s.transportType ?? "stdio",
    logo: sanitizeSvg(s.logo ?? ""),
    category: s.category ?? "",
    credentialFields,
    configured: credentialFields.length === 0,
    isBuiltin: s.isBuiltin,
  };
}

export function useMcpServers(): {
  servers: McpServer[];
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery(MCP_SERVERS_QUERY, {
    fetchPolicy: "cache-and-network",
  });
  const raw: RawMcpServer[] = data?.mcpServers ?? [];
  const servers = raw.map(mapMcpServer);
  return { servers, loading, error: error ?? undefined };
}

export function useMcpServer(name: string): {
  server: McpServer | null;
  loading: boolean;
  error?: Error;
  refetch: () => void;
} {
  const { data, loading, error, refetch } = useQuery(MCP_SERVER_QUERY, {
    variables: { name },
    skip: !name,
  });
  const raw: RawMcpServer | null = data?.mcpServer ?? null;
  const server = raw ? mapMcpServer(raw) : null;
  return { server, loading, error: error ?? undefined, refetch };
}

export function useSaveMcpCredential() {
  const [fn] = useMutation(SAVE_MCP_CREDENTIAL);
  return async (serverName: string, credentials: Record<string, string>) => {
    const fields = Object.entries(credentials).map(([key, value]) => ({
      key,
      value,
    }));
    await fn({
      variables: { serverName, fields },
    });
  };
}

/** Sort MCP servers: configured first, then alphabetical by title. */
export function sortMcpServers(list: McpServer[]): McpServer[] {
  return [...list].sort((a, b) => {
    const scoreA = a.configured ? 1 : 0;
    const scoreB = b.configured ? 1 : 0;
    if (scoreB !== scoreA) return scoreB - scoreA;
    return a.title.localeCompare(b.title);
  });
}

// Backward-compatible aliases
/** @deprecated Use useMcpServers instead */
export function useIntegrations() {
  const { servers, loading, error } = useMcpServers();
  return { integrations: servers, loading, error };
}

/** @deprecated Use useMcpServer instead */
export function useIntegration(slug: string) {
  const { server, loading, error, refetch } = useMcpServer(slug);
  return { integration: server, loading, error, refetch };
}

/** @deprecated Use sortMcpServers instead */
export const sortIntegrations = sortMcpServers;

/** @deprecated Use useSaveMcpCredential instead */
export function useSetSkillCredentials() {
  const save = useSaveMcpCredential();
  return save;
}
